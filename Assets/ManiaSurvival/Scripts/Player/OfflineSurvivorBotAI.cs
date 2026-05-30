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

    [Header("Flee")]
    public float fleeRadius = 11f;
    public float fleeMoveSpeed = 5f;
    public float lowHealthFleeThreshold = 0.4f;
    public float woundedFleeSpeedMultiplier = 1.25f;
    public float hellfireAvoidRadius = 4f;

    [Header("Combat")]
    public float minAbilityInterval = 4f;
    public float maxAbilityInterval = 7f;
    public float medicHealTargetPercent = 0.7f;

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

                Transform predator = FindPredatorTransform();
                Vector3 moveDirection = GetMoveDirection(destination, predator);
                float moveSpeed = GetMoveSpeed(predator);

                if (moveDirection.sqrMagnitude <= 0.001f)
                {
                    TrySimpleMove(Vector3.zero);
                    stuckTimer = 0f;
                    yield return null;
                    continue;
                }

                Vector3 toDestination = destination - transform.position;
                toDestination.y = 0f;
                if (toDestination.sqrMagnitude <= stopDistance * stopDistance && predator == null)
                {
                    TrySimpleMove(Vector3.zero);
                    stuckTimer = 0f;
                    break;
                }

                Vector3 beforeMove = transform.position;
                TrySimpleMove(moveDirection.normalized * moveSpeed);
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

    private Vector3 GetMoveDirection(Vector3 wanderDestination, Transform predator)
    {
        Vector3 direction = Vector3.zero;

        if (predator != null)
        {
            Vector3 toPredator = predator.position - transform.position;
            toPredator.y = 0f;
            float predatorDistance = toPredator.magnitude;
            bool lowHealth = IsLowHealth();
            float effectiveFleeRadius = lowHealth ? fleeRadius * 1.15f : fleeRadius;

            if (predatorDistance <= effectiveFleeRadius && predatorDistance > 0.05f)
            {
                direction = -toPredator.normalized;
            }
        }

        if (direction.sqrMagnitude <= 0.001f)
        {
            Vector3 toDestination = wanderDestination - transform.position;
            toDestination.y = 0f;
            if (toDestination.sqrMagnitude > 0.05f)
            {
                direction = toDestination.normalized;
            }
        }

        direction = ApplyHellfireAvoidance(direction);
        return direction;
    }

    private float GetMoveSpeed(Transform predator)
    {
        if (predator == null)
        {
            return Mathf.Max(0.1f, wanderMoveSpeed);
        }

        Vector3 toPredator = predator.position - transform.position;
        toPredator.y = 0f;
        if (toPredator.magnitude > fleeRadius)
        {
            return Mathf.Max(0.1f, wanderMoveSpeed);
        }

        float speed = Mathf.Max(0.1f, fleeMoveSpeed);
        if (IsLowHealth())
        {
            speed *= woundedFleeSpeedMultiplier;
        }

        return speed;
    }

    private Vector3 ApplyHellfireAvoidance(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            return direction;
        }

        Vector3 hellfirePush = GetHellfireAvoidanceVector();
        if (hellfirePush.sqrMagnitude <= 0.001f)
        {
            return direction;
        }

        Vector3 blended = (direction.normalized + hellfirePush).normalized;
        return blended.sqrMagnitude > 0.001f ? blended : direction;
    }

    private Vector3 GetHellfireAvoidanceVector()
    {
        HellfirePitDamageZone[] pits = FindObjectsByType<HellfirePitDamageZone>(FindObjectsSortMode.None);
        Vector3 push = Vector3.zero;

        for (int i = 0; i < pits.Length; i++)
        {
            HellfirePitDamageZone pit = pits[i];
            if (pit == null)
            {
                continue;
            }

            Vector3 pitCenter = pit.transform.TransformPoint(pit.localDamageCenter);
            Vector3 away = transform.position - pitCenter;
            away.y = 0f;
            float distance = away.magnitude;
            if (distance <= hellfireAvoidRadius && distance > 0.05f)
            {
                push += away.normalized * (hellfireAvoidRadius - distance);
            }
        }

        return push;
    }

    private bool IsLowHealth()
    {
        return unitHealth != null
            && unitHealth.maxHealth > 0
            && (float)unitHealth.currentHealth / unitHealth.maxHealth <= lowHealthFleeThreshold;
    }

    private Transform FindPredatorTransform()
    {
        LocalRoleController roleController = FindFirstObjectByType<LocalRoleController>();
        if (roleController != null && roleController.controlMode == PlayerControlMode.MonsterControlled)
        {
            if (roleController.monsterMovement != null && roleController.monsterMovement.enabled)
            {
                return roleController.monsterMovement.transform;
            }
        }

        MonsterPlayerMovement monster = FindFirstObjectByType<MonsterPlayerMovement>();
        if (monster != null && monster.enabled)
        {
            return monster.transform;
        }

        GameObject taggedMonster = GameObject.FindGameObjectWithTag("Monster");
        if (taggedMonster != null)
        {
            return taggedMonster.transform;
        }

        MonsterAI monsterAi = FindFirstObjectByType<MonsterAI>();
        return monsterAi != null && monsterAi.enabled ? monsterAi.transform : null;
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

            if (TryMedicSupport())
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

    private bool TryMedicSupport()
    {
        if (survivorClassManager == null || survivorClassManager.activeClass != SurvivorClass.Medic)
        {
            return false;
        }

        if (abilityController == null)
        {
            return false;
        }

        if (abilityController.GetCooldownRemaining(2) <= 0f)
        {
            int alliesNeedingHeal = CountAlliesBelowHealthPercent(survivorClassManager.healPulseRadius, medicHealTargetPercent);
            if (alliesNeedingHeal > 0)
            {
                abilityController.UseAbilitySlot(2);
                Debug.Log("[SurvivorAI] Medic used Heal Pulse on " + alliesNeedingHeal + " allies");
                return true;
            }
        }

        if (abilityController.GetCooldownRemaining(1) <= 0f)
        {
            UnitHealth woundedAlly = FindNearestWoundedAlly(survivorClassManager.bioticDartRange, medicHealTargetPercent);
            if (woundedAlly != null)
            {
                abilityController.UseAbilitySlot(1);
                Debug.Log("[SurvivorAI] Medic used Heal Dart on " + woundedAlly.name);
                return true;
            }
        }

        return false;
    }

    private int CountAlliesBelowHealthPercent(float radius, float healthPercent)
    {
        int count = 0;
        UnitHealth[] allies = FindAllies(radius);
        for (int i = 0; i < allies.Length; i++)
        {
            UnitHealth ally = allies[i];
            if (ally == null || ally.IsDead || ally.maxHealth <= 0)
            {
                continue;
            }

            if ((float)ally.currentHealth / ally.maxHealth < healthPercent)
            {
                count++;
            }
        }

        return count;
    }

    private UnitHealth FindNearestWoundedAlly(float radius, float healthPercent)
    {
        UnitHealth[] allies = FindAllies(radius);
        UnitHealth nearest = null;
        float nearestSqr = float.MaxValue;

        for (int i = 0; i < allies.Length; i++)
        {
            UnitHealth ally = allies[i];
            if (ally == null || ally.IsDead || ally.maxHealth <= 0)
            {
                continue;
            }

            if ((float)ally.currentHealth / ally.maxHealth >= healthPercent)
            {
                continue;
            }

            float sqr = (ally.transform.position - transform.position).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = ally;
            }
        }

        return nearest;
    }

    private UnitHealth[] FindAllies(float radius)
    {
        UnitHealth[] all = FindObjectsByType<UnitHealth>(FindObjectsSortMode.None);
        System.Collections.Generic.List<UnitHealth> allies = new System.Collections.Generic.List<UnitHealth>();

        for (int i = 0; i < all.Length; i++)
        {
            UnitHealth candidate = all[i];
            if (candidate == null || candidate.IsDead || !candidate.CompareTag("Survivor"))
            {
                continue;
            }

            float sqr = (candidate.transform.position - transform.position).sqrMagnitude;
            if (sqr <= radius * radius)
            {
                allies.Add(candidate);
            }
        }

        return allies.ToArray();
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
