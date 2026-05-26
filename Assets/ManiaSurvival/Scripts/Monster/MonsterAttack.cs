using UnityEngine;

public class MonsterAttack : MonoBehaviour
{
    [Header("Attack")]
    public int attackDamage = 10;
    public float attackRange = 1.75f;
    public float attackCooldown = 0.75f;
    public bool autoAttack = true;
    public float attackRadius = 1.75f;
    public LayerMask survivorLayers = ~0;
    public Transform attackPoint;
    public string targetTag = "Survivor";

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
            TryAttackInternal(false);
        }
    }

    public void TryAttack()
    {
        TryAttackInternal(true);
    }

    public void TryAttackNearestSurvivor()
    {
        TryAttackInternal(false);
    }

    public void TryAttackTarget(UnitHealth target)
    {
        if (!CanAttackTarget(target) || cooldownTimer > 0f)
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

    private void TryAttackInternal(bool logMessages)
    {
        if (cooldownTimer > 0f)
        {
            if (logMessages)
            {
                Debug.Log("[MonsterAttack] Attack on cooldown");
            }

            return;
        }

        if (logMessages)
        {
            Debug.Log("[MonsterAttack] Attack started");
        }

        UnitHealth target = FindNearestSurvivorInRange();

        if (target == null)
        {
            if (logMessages)
            {
                Debug.Log("[MonsterAttack] No valid target");
            }

            return;
        }

        Attack(target);

        if (logMessages)
        {
            Debug.Log("[MonsterAttack] Target hit: " + target.name);
        }
    }

    private UnitHealth FindNearestSurvivorInRange()
    {
        Vector3 origin = GetAttackOrigin();
        float searchRadius = attackRadius > 0f ? attackRadius : attackRange;
        Collider[] hits = Physics.OverlapSphere(origin, searchRadius, survivorLayers, QueryTriggerInteraction.Ignore);
        UnitHealth nearest = null;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth survivor = hits[i].GetComponentInParent<UnitHealth>();

            if (!CanAttackTarget(survivor))
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

    private void Attack(UnitHealth target)
    {
        cooldownTimer = attackCooldown;
        target.TakeDamage(attackDamage, gameObject);

        if (target.IsDead && gameManager != null)
        {
            gameManager.ReportSurvivorDeath(target, gameObject);
        }
    }

    private Vector3 GetAttackOrigin()
    {
        return attackPoint != null ? attackPoint.position : transform.position;
    }

    private bool CanAttackTarget(UnitHealth target)
    {
        if (target == null || target.IsDead)
        {
            return false;
        }

        if (target.gameObject == gameObject)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(targetTag) && !target.CompareTag(targetTag))
        {
            return false;
        }

        SurvivorVisibilityStatus visibilityStatus = target.GetComponent<SurvivorVisibilityStatus>();
        if (visibilityStatus != null && visibilityStatus.IsHiddenFromMonster)
        {
            return false;
        }

        return true;
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
