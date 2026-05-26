using UnityEngine;

[RequireComponent(typeof(UnitHealth))]
public class TreantMinion : MonoBehaviour
{
    [Header("Treant")]
    public float moveSpeed = 2.5f;
    public float attackRange = 3f;
    public int attackDamage = 4;
    public float attackCooldown = 1.2f;
    public float lifetime = 18f;

    [Header("Target")]
    public float targetSearchRadius = 25f;

    private UnitHealth unitHealth;
    private CharacterController characterController;
    private Transform owner;
    private UnitHealth target;
    private float nextAttackTime;
    private float lifeTimer;
    private bool expired;

    public bool IsExpired => expired || lifeTimer <= 0f || unitHealth == null || unitHealth.IsDead;

    private void Awake()
    {
        unitHealth = GetComponent<UnitHealth>();
        characterController = GetComponent<CharacterController>();
        lifeTimer = lifetime;
    }

    private void Update()
    {
        if (IsExpired)
        {
            expired = true;
            return;
        }

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            expired = true;
            Debug.Log("Treant expired");
            if (unitHealth != null)
            {
                unitHealth.TakeDamage(unitHealth.currentHealth, gameObject);
            }
            return;
        }

        if (!IsValidTarget(target))
        {
            target = FindMonsterTarget();
        }

        if (target == null)
        {
            Debug.Log("no valid target found");
            return;
        }

        float targetDistance = GetTargetDistance(target);
        Debug.Log("current distance to target: " + targetDistance.ToString("0.00") + " attack range: " + attackRange.ToString("0.00"));

        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;

        if (targetDistance > attackRange)
        {
            Vector3 moveDirection = toTarget.normalized;

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
            }
            else
            {
                transform.position += moveDirection * moveSpeed * Time.deltaTime;
            }

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }

            Debug.Log("moving toward target");
            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        int healthBefore = target.currentHealth;
        target.TakeDamage(attackDamage, gameObject);
        int healthAfter = target != null ? target.currentHealth : 0;
        Debug.Log("attacking target: hp " + healthBefore + " -> " + healthAfter);
    }

    public void Initialize(Transform summonOwner)
    {
        owner = summonOwner;
    }

    private UnitHealth FindMonsterTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, targetSearchRadius, ~0, QueryTriggerInteraction.Ignore);
        UnitHealth nearest = null;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth health = hits[i].GetComponentInParent<UnitHealth>();
            if (health == null)
            {
                Debug.Log("target missing UnitHealth");
                continue;
            }

            if (health.IsDead || !IsMonsterTarget(health))
            {
                continue;
            }

            if (owner != null && health.transform == owner)
            {
                continue;
            }

            float distanceSqr = (health.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr < nearestDistanceSqr)
            {
                nearest = health;
                nearestDistanceSqr = distanceSqr;
            }
        }

        if (nearest != null)
        {
            Debug.Log("target found: " + nearest.name);
        }

        return nearest;
    }

    private bool IsValidTarget(UnitHealth health)
    {
        return health != null && !health.IsDead && IsMonsterTarget(health);
    }

    private bool IsMonsterTarget(UnitHealth health)
    {
        return health != null && (health.CompareTag("Monster") || health.CompareTag("Predator"));
    }

    private float GetTargetDistance(UnitHealth health)
    {
        if (health == null)
        {
            return float.MaxValue;
        }

        Collider targetCollider = health.GetComponentInChildren<Collider>();
        if (targetCollider == null)
        {
            targetCollider = health.GetComponent<Collider>();
        }

        Vector3 treantPosition = transform.position;
        treantPosition.y = 0f;

        if (targetCollider != null)
        {
            Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);
            closestPoint.y = 0f;
            return Vector3.Distance(treantPosition, closestPoint);
        }

        Vector3 targetPosition = health.transform.position;
        targetPosition.y = 0f;
        return Vector3.Distance(treantPosition, targetPosition);
    }
}
