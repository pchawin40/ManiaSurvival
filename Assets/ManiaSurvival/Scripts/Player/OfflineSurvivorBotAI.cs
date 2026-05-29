using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(SurvivorClassManager))]
public class OfflineSurvivorBotAI : MonoBehaviour
{
    [Header("AI Toggle")]
    [SerializeField] private bool isAiControlled;
    [SerializeField] private LocalRoleController localRoleController;

    [Header("Wander")]
    public float wanderRadius = 10f;
    public float wanderRetargetInterval = 3f;
    public float wanderMoveSpeed = 3.5f;
    public float waypointReachDistance = 0.8f;

    [Header("Combat")]
    public float minAbilityInterval = 4f;
    public float maxAbilityInterval = 7f;

    private CharacterController characterController;
    private SurvivorClassManager survivorClassManager;
    private Coroutine wanderRoutine;
    private Coroutine combatRoutine;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        survivorClassManager = GetComponent<SurvivorClassManager>();

        if (localRoleController == null)
        {
            localRoleController = GetComponent<LocalRoleController>();
        }
    }

    private void OnEnable()
    {
        RefreshAiControlState();
        UpdateAiRoutines();
    }

    private void OnDisable()
    {
        StopAiRoutines();
    }

    private void Update()
    {
        RefreshAiControlState();
        UpdateAiRoutines();
    }

    private void RefreshAiControlState()
    {
        if (localRoleController == null)
        {
            localRoleController = FindFirstObjectByType<LocalRoleController>();
        }

        isAiControlled = localRoleController != null
            && localRoleController.controlMode == PlayerControlMode.MonsterControlled;
    }

    private void UpdateAiRoutines()
    {
        bool hasWander = wanderRoutine != null;
        bool hasCombat = combatRoutine != null;

        if (!isAiControlled)
        {
            if (hasWander || hasCombat)
            {
                StopAiRoutines();
            }
            return;
        }

        if (!hasWander)
        {
            wanderRoutine = StartCoroutine(WanderRoutine());
        }

        if (!hasCombat)
        {
            combatRoutine = StartCoroutine(CombatRoutine());
        }
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

        if (characterController != null && characterController.enabled)
        {
            characterController.SimpleMove(Vector3.zero);
        }
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
                    if (characterController != null && characterController.enabled)
                    {
                        characterController.SimpleMove(Vector3.zero);
                    }

                    yield return null;
                    continue;
                }

                Vector3 toDestination = destination - transform.position;
                toDestination.y = 0f;

                if (toDestination.sqrMagnitude <= stopDistance * stopDistance)
                {
                    characterController.SimpleMove(Vector3.zero);
                    break;
                }

                characterController.SimpleMove(toDestination.normalized * moveSpeed);
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

            if (!CanMoveAsAi() || survivorClassManager == null)
            {
                continue;
            }

            int roll = Random.Range(0, 4);
            switch (roll)
            {
                case 0:
                    survivorClassManager.ExecutePrimary();
                    break;
                case 1:
                    survivorClassManager.ExecuteAbility2();
                    break;
                case 2:
                    survivorClassManager.ExecuteAbility3();
                    break;
                default:
                    survivorClassManager.ExecuteUltimate();
                    break;
            }
        }
    }

    private Vector3 GetRandomWanderDestination()
    {
        Vector2 offset2D = Random.insideUnitCircle * Mathf.Max(0.1f, wanderRadius);
        Vector3 destination = transform.position + new Vector3(offset2D.x, 0f, offset2D.y);
        destination.y = transform.position.y;
        return destination;
    }

    private bool CanMoveAsAi()
    {
        return isAiControlled
            && characterController != null
            && characterController.enabled;
    }
}
