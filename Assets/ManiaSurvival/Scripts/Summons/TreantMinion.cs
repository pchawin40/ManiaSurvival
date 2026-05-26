using UnityEngine;

[RequireComponent(typeof(UnitHealth))]
public class TreantMinion : MonoBehaviour
{
    [Header("Treant")]
    public float moveSpeed = 2.5f;
    public float attackRange = 1.5f;
    public int attackDamage = 4;
    public float attackCooldown = 1.2f;
    public float lifetime = 18f;

    [Header("Target")]
    public string targetTag = "Monster";

    private UnitHealth unitHealth;
    private Transform owner;
    private UnitHealth target;
    private float nextAttackTime;
    private float lifeTimer;
    private bool expired;

    public bool IsExpired => expired || lifeTimer <= 0f || unitHealth == null || unitHealth.IsDead;

    private void Awake()
    {
        unitHealth = GetComponent<UnitHealth>();
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

        if (target == null || target.IsDead || !target.CompareTag(targetTag))
        {
            target = FindMonsterTarget();
        }

        if (target == null)
        {
            return;
        }

        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude > attackRange * attackRange)
        {
            Vector3 moveDirection = toTarget.normalized;
            transform.position += moveDirection * moveSpeed * Time.deltaTime;

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }

            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        target.TakeDamage(attackDamage, gameObject);
        Debug.Log("Treant hit Monster");
    }

    public void Initialize(Transform summonOwner)
    {
        owner = summonOwner;
    }

    private UnitHealth FindMonsterTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 25f, ~0, QueryTriggerInteraction.Ignore);
        UnitHealth nearest = null;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth health = hits[i].GetComponentInParent<UnitHealth>();
            if (health == null || health.IsDead || !health.CompareTag(targetTag))
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

        return nearest;
    }
}
