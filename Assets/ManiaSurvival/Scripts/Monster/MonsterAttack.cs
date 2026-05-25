using UnityEngine;

public class MonsterAttack : MonoBehaviour
{
    [Header("Attack")]
    public int damage = 1;
    public float attackRange = 1.6f;
    public float attackCooldown = 0.8f;
    public bool autoAttack = true;
    public LayerMask survivorLayers = ~0;
    public Transform attackPoint;

    [Header("Game Manager")]
    public ManiaGameManager gameManager;

    [Header("Debug")]
    public bool drawAttackRange = true;

    private float cooldownTimer;

    private void Awake()
    {
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
    }

    private void Update()
    {
        cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);

        if (gameManager != null && gameManager.State != ManiaGameState.Playing)
        {
            return;
        }

        if (autoAttack)
        {
            TryAttackNearestSurvivor();
        }
    }

    public void TryAttackNearestSurvivor()
    {
        if (cooldownTimer > 0f)
        {
            return;
        }

        SurvivorHealth target = FindNearestSurvivorInRange();

        if (target != null)
        {
            Attack(target);
        }
    }

    public void TryAttackTarget(SurvivorHealth target)
    {
        if (target == null || !target.IsAlive || cooldownTimer > 0f)
        {
            return;
        }

        Vector3 origin = GetAttackOrigin();
        float distanceSqr = (target.transform.position - origin).sqrMagnitude;

        if (distanceSqr <= attackRange * attackRange)
        {
            Attack(target);
        }
    }

    private SurvivorHealth FindNearestSurvivorInRange()
    {
        Vector3 origin = GetAttackOrigin();
        Collider[] hits = Physics.OverlapSphere(origin, attackRange, survivorLayers, QueryTriggerInteraction.Ignore);
        SurvivorHealth nearest = null;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            SurvivorHealth survivor = hits[i].GetComponentInParent<SurvivorHealth>();

            if (survivor == null || !survivor.IsAlive)
            {
                continue;
            }

            float distanceSqr = (survivor.transform.position - origin).sqrMagnitude;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearest = survivor;
                nearestDistanceSqr = distanceSqr;
            }
        }

        return nearest;
    }

    private void Attack(SurvivorHealth target)
    {
        cooldownTimer = attackCooldown;
        target.TakeDamage(damage, gameObject);
    }

    private Vector3 GetAttackOrigin()
    {
        return attackPoint != null ? attackPoint.position : transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawAttackRange)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(GetAttackOrigin(), attackRange);
    }
}
