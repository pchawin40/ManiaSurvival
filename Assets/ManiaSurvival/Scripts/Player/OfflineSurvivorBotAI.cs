using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(SurvivorClassManager))]
[RequireComponent(typeof(AbilityController))]
public class OfflineSurvivorBotAI : MonoBehaviour
{
    [Header("Wander")]
    public float wanderRadius = 10f;
    public float wanderRetargetInterval = 3f;
    public float wanderMoveSpeed = 3.5f;
    public float waypointReachDistance = 0.8f;

    [Header("Combat")]
    public float minAbilityInterval = 4f;
    public float maxAbilityInterval = 7f;

    [Header("Rotation")]
    [Tooltip("Keep the unit's play-start rotation. Useful for test anchors/dummies that should wander without turning.")]
    public bool lockRotationDuringPlay;

    private CharacterController characterController;
    private UnitHealth unitHealth;
    private SurvivorClassManager survivorClassManager;
    private AbilityController abilityController;
    private Coroutine wanderRoutine;
    private Coroutine combatRoutine;
    private bool loggedInactiveController;
    private bool loggedWanderClamp;
    private float stuckTimer;
    private Quaternion playStartRotation;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        unitHealth = GetComponent<UnitHealth>();
        survivorClassManager = GetComponent<SurvivorClassManager>();
        abilityController = GetComponent<AbilityController>();
        playStartRotation = transform.rotation;
    }

    private void OnEnable()
    {
        loggedInactiveController = false;
        loggedWanderClamp = false;
        stuckTimer = 0f;
        playStartRotation = transform.rotation;
        wanderRoutine = StartCoroutine(WanderRoutine());
        combatRoutine = StartCoroutine(CombatRoutine());
    }

    private void OnDisable()
    {
        StopAiRoutines();
    }

    private void StopAiRoutines()
    {
        if (wanderRoutine != null)
        {
            StopCoroutine(wanderRoutine);
            wanderRoutine = null;
        }

        if (combatRoutine != null)
        {
            StopCoroutine(combatRoutine);
            combatRoutine = null;
        }

        TrySimpleMove(Vector3.zero);
    }

    private IEnumerator WanderRoutine()
    {
        float retargetTime = Mathf.Max(0.1f, wanderRetargetInterval);
        float moveSpeed = Mathf.Max(0.1f, wanderMoveSpeed);
        float stopDistance = Mathf.Max(0.1f, waypointReachDistance);

        while (isActiveAndEnabled)
        {
            Vector3 destination = GetRandomWanderDestination();
            float timer = 0f;

            while (timer < retargetTime)
            {
                timer += Time.deltaTime;

                if (!CanMoveAsAi())
                {
                    TrySimpleMove(Vector3.zero);
                    yield return null;
                    continue;
                }

                Vector3 toDestination = destination - transform.position;
                toDestination.y = 0f;

                if (toDestination.sqrMagnitude <= stopDistance * stopDistance)
                {
                    TrySimpleMove(Vector3.zero);
                    stuckTimer = 0f;
                    break;
                }

                Vector3 beforeMove = transform.position;
                TrySimpleMove(toDestination.normalized * moveSpeed);
                float movedSqr = (transform.position - beforeMove).sqrMagnitude;
                if (movedSqr < 0.0004f)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer >= 0.6f)
                    {
                        stuckTimer = 0f;
                        break;
                    }
                }
                else
                {
                    stuckTimer = 0f;
                }

                yield return null;
            }
        }
    }

    private IEnumerator CombatRoutine()
    {
        while (isActiveAndEnabled)
        {
            float delay = Random.Range(Mathf.Max(0.1f, minAbilityInterval), Mathf.Max(minAbilityInterval + 0.1f, maxAbilityInterval));
            yield return new WaitForSeconds(delay);

            if (!CanMoveAsAi())
            {
                continue;
            }

            int roll = Random.Range(0, 4);
            if (abilityController != null)
            {
                abilityController.UseAbilitySlot(roll + 1);
                continue;
            }

            if (survivorClassManager == null)
            {
                continue;
            }

            switch (roll)
            {
                case 0: survivorClassManager.ExecutePrimary(); break;
                case 1: survivorClassManager.ExecuteAbility2(); break;
                case 2: survivorClassManager.ExecuteAbility3(); break;
                default: survivorClassManager.ExecuteUltimate(); break;
            }
        }
    }

    private Vector3 GetRandomWanderDestination()
    {
        float effectiveRadius = GetEffectiveWanderRadius();
        Vector2 offset2D = Random.insideUnitCircle * effectiveRadius;
        Vector3 raw = transform.position + new Vector3(offset2D.x, 0f, offset2D.y);
        raw.y = transform.position.y;

        if (ArenaBounds.Instance == null)
        {
            return raw;
        }

        Vector3 clamped = ArenaBounds.Instance.ClampPosition(raw);
        clamped.y = transform.position.y;

        if ((clamped - raw).sqrMagnitude > 0.01f && !loggedWanderClamp)
        {
            loggedWanderClamp = true;
            Debug.Log("[AI] Wander target clamped inside arena on '" + gameObject.name + "'.");
        }

        return clamped;
    }

    private float GetEffectiveWanderRadius()
    {
        float radius = Mathf.Max(0.1f, wanderRadius);
        if (ArenaBounds.Instance == null)
        {
            return radius;
        }

        float margin = GetMarginToBoundsEdge(transform.position);
        return Mathf.Min(radius, Mathf.Max(1f, margin - 1f));
    }

    private float GetMarginToBoundsEdge(Vector3 position)
    {
        if (ArenaBounds.Instance == null)
        {
            return wanderRadius;
        }

        float dx = Mathf.Min(position.x - ArenaBounds.Instance.minX, ArenaBounds.Instance.maxX - position.x);
        float dz = Mathf.Min(position.z - ArenaBounds.Instance.minZ, ArenaBounds.Instance.maxZ - position.z);
        return Mathf.Min(dx, dz);
    }

    private bool CanMoveAsAi()
    {
        if (!isActiveAndEnabled || !enabled)
        {
            return false;
        }

        if (unitHealth != null && unitHealth.IsDead)
        {
            return false;
        }

        if (ManiaGameManager.Instance != null && !ManiaGameManager.Instance.IsPlaying)
        {
            return false;
        }

        return characterController != null
            && characterController.enabled
            && characterController.gameObject.activeInHierarchy;
    }

    private bool TrySimpleMove(Vector3 velocity)
    {
        if (characterController == null || !characterController.enabled || !characterController.gameObject.activeInHierarchy)
        {
            if (!loggedInactiveController)
            {
                loggedInactiveController = true;
                Debug.LogWarning("[OfflineSurvivorBotAI] Skipping movement on inactive CharacterController on '" + gameObject.name + "'.");
            }

            return false;
        }

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return false;
        }

        if (unitHealth != null && unitHealth.IsDead)
        {
            return false;
        }

        characterController.SimpleMove(velocity);

        if (ArenaBounds.Instance != null && !ArenaBounds.Instance.IsInside(transform.position))
        {
            ArenaBounds.Instance.ClampUnitTransform(transform, "OfflineSurvivorBotAI");
        }

        if (lockRotationDuringPlay)
        {
            transform.rotation = playStartRotation;
        }

        return true;
    }
}
