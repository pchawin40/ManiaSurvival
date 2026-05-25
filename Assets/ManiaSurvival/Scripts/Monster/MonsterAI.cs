using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MonsterAI : MonoBehaviour
{
    [Header("Chase")]
    public float moveSpeed = 5.75f;
    public float stoppingDistance = 1.25f;
    public float retargetInterval = 0.5f;
    public UnitHealth currentTarget;

    [Header("Movement")]
    public float rotationSpeed = 720f;
    public float gravity = -20f;

    [Header("Game Manager")]
    public ManiaGameManager gameManager;

    private CharacterController characterController;
    private float retargetTimer;
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (gameManager == null)
        {
            gameManager = ManiaGameManager.Instance;
        }
    }

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<ManiaGameManager>();
        }

        FindNearestTarget();
    }

    private void Update()
    {
        if (gameManager != null && gameManager.State != ManiaGameState.Playing)
        {
            return;
        }

        retargetTimer -= Time.deltaTime;

        if (!IsValidVisibleTarget(currentTarget))
        {
            currentTarget = null;
        }

        if (currentTarget == null || retargetTimer <= 0f)
        {
            FindNearestTarget();
        }

        if (!IsValidVisibleTarget(currentTarget))
        {
            currentTarget = null;
            ApplyGravityOnly();
            return;
        }

        ChaseTarget();
    }

    public void SetTarget(UnitHealth target)
    {
        currentTarget = target;
        retargetTimer = retargetInterval;
    }

    private void FindNearestTarget()
    {
        retargetTimer = retargetInterval;

        GameObject[] allSurvivorObjects = GameObject.FindGameObjectsWithTag("Survivor");
        UnitHealth nearest = null;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < allSurvivorObjects.Length; i++)
        {
            UnitHealth survivor = allSurvivorObjects[i].GetComponent<UnitHealth>();

            if (!IsValidVisibleTarget(survivor))
            {
                continue;
            }

            float distanceSqr = (survivor.transform.position - transform.position).sqrMagnitude;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearest = survivor;
                nearestDistanceSqr = distanceSqr;
            }
        }

        currentTarget = nearest;
    }

    private bool IsValidVisibleTarget(UnitHealth target)
    {
        if (target == null || target.IsDead || !target.CompareTag("Survivor"))
        {
            return false;
        }

        SurvivorStealthStatus stealthStatus = target.GetComponent<SurvivorStealthStatus>();
        return stealthStatus == null || !stealthStatus.IsInvisible;
    }

    private void ChaseTarget()
    {
        Vector3 toTarget = currentTarget.transform.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= stoppingDistance)
        {
            ApplyGravityOnly();
            RotateToward(toTarget);
            return;
        }

        Vector3 moveDirection = toTarget.normalized;
        ApplyGravity();
        characterController.Move((moveDirection * moveSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);
        RotateToward(moveDirection);
    }

    private void ApplyGravityOnly()
    {
        ApplyGravity();
        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -1f;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void RotateToward(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
