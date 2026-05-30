using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitHealth))]
public class MonsterAttack : MonoBehaviour
{
    [Header("Attack")]
    public bool autoAttack = false;
    public int attackDamage = 8;
    public float attackRange = 2f;
    public float attackCooldown = 0.8f;
    public LayerMask targetLayers = ~0;
    public string targetTag = "Survivor";

    [Header("References")]
    public ManiaGameManager gameManager;
    public UnitHealth ownerHealth;

    private float nextAttackTime;

    private void Awake()
    {
        if (ownerHealth == null)
        {
            ownerHealth = GetComponent<UnitHealth>();
        }

        if (gameManager == null)
        {
            gameManager = ManiaGameManager.Instance;
        }
    }

    private void Update()
    {
        if (!autoAttack)
        {
            return;
        }

        TryAttack();
    }

    public void TryAttack()
    {
        if (!CanAttack())
        {
            return;
        }

        UnitHealth target = FindNearestSurvivor();
        if (target == null)
        {
            return;
        }

        nextAttackTime = Time.time + Mathf.Max(0.05f, attackCooldown);
        target.TakeDamage(Mathf.Max(1, attackDamage), gameObject);
    }

    private bool CanAttack()
    {
        if (ownerHealth == null)
        {
            ownerHealth = GetComponent<UnitHealth>();
        }

        if (ownerHealth == null || ownerHealth.IsDead)
        {
            return false;
        }

        if (gameManager == null)
        {
            gameManager = ManiaGameManager.Instance;
        }

        if (gameManager != null && gameManager.State != ManiaGameState.Playing)
        {
            return false;
        }

        return Time.time >= nextAttackTime;
    }

    private UnitHealth FindNearestSurvivor()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            Mathf.Max(0.1f, attackRange),
            targetLayers,
            QueryTriggerInteraction.Ignore);

        UnitHealth nearest = null;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth health = hits[i].GetComponentInParent<UnitHealth>();
            if (health == null || health.IsDead || !health.CompareTag(targetTag))
            {
                continue;
            }

            float distSqr = (health.transform.position - transform.position).sqrMagnitude;
            if (distSqr < nearestDistanceSqr)
            {
                nearest = health;
                nearestDistanceSqr = distSqr;
            }
        }

        return nearest;
    }
}
