using UnityEngine;

[RequireComponent(typeof(UnitHealth))]
public class MonsterRoarAbility : MonoBehaviour
{
    [Header("Roar")]
    public int damage = 10;
    public float radius = 6f;
    public float cooldown = 12f;
    public float knockbackDistance = 3f;
    public string targetTag = "Survivor";
    public LayerMask survivorLayers = ~0;

    [Header("UI")]
    public AbilityCooldownButton cooldownButton;

    [Header("Game Manager")]
    public ManiaGameManager gameManager;

    [Header("Debug")]
    public bool drawRoarRange = true;

    private UnitHealth unitHealth;
    private float nextCastTime;

    private void Awake()
    {
        unitHealth = GetComponent<UnitHealth>();

        if (cooldownButton == null)
        {
            cooldownButton = FindFirstObjectByType<AbilityCooldownButton>();
        }

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

    public void CastRoar()
    {
        if (cooldownButton != null && cooldownButton.IsCoolingDown)
        {
            Debug.Log("Roar on cooldown");
            return;
        }

        if (Time.time < nextCastTime)
        {
            Debug.Log("Roar on cooldown");
            return;
        }

        if (unitHealth == null)
        {
            unitHealth = GetComponent<UnitHealth>();
        }

        if (unitHealth == null || unitHealth.IsDead)
        {
            Debug.Log("Roar blocked");
            return;
        }

        if (gameManager != null && gameManager.State != ManiaGameState.Playing)
        {
            Debug.Log("Roar blocked: game not in play");
            return;
        }

        int hitCount = ApplyRoarDamage();

        nextCastTime = Time.time + cooldown;
        if (cooldownButton != null)
        {
            cooldownButton.StartCooldown(cooldown);
        }

        Debug.Log("Roar cast, survivors hit: " + hitCount);
    }

    private int ApplyRoarDamage()
    {
        Vector3 origin = transform.position;
        float searchRadius = Mathf.Max(0.01f, radius);
        Collider[] hits = Physics.OverlapSphere(origin, searchRadius, survivorLayers, QueryTriggerInteraction.Ignore);

        int hitCount = 0;
        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth survivor = hits[i].GetComponentInParent<UnitHealth>();
            if (!CanHit(survivor))
            {
                continue;
            }

            survivor.TakeDamage(damage, gameObject);
            ApplyKnockback(survivor, origin);

            if (survivor.IsDead && gameManager != null)
            {
                gameManager.ReportSurvivorDeath(survivor, gameObject);
            }

            hitCount++;
        }

        return hitCount;
    }

    private bool CanHit(UnitHealth target)
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

    private void ApplyKnockback(UnitHealth target, Vector3 origin)
    {
        if (knockbackDistance <= 0f)
        {
            return;
        }

        Vector3 awayDirection = target.transform.position - origin;
        awayDirection.y = 0f;

        if (awayDirection.sqrMagnitude <= 0.001f)
        {
            awayDirection = transform.forward;
            awayDirection.y = 0f;
        }

        if (awayDirection.sqrMagnitude <= 0.001f)
        {
            awayDirection = Vector3.forward;
        }

        awayDirection = awayDirection.normalized;

        CharacterController characterController = target.GetComponent<CharacterController>();
        if (characterController != null && characterController.enabled)
        {
            characterController.Move(awayDirection * knockbackDistance);
            return;
        }

        Rigidbody rigidbody = target.GetComponent<Rigidbody>();
        if (rigidbody != null && !rigidbody.isKinematic)
        {
            rigidbody.AddForce(awayDirection * knockbackDistance, ForceMode.Impulse);
            return;
        }

        target.transform.position += awayDirection * knockbackDistance;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawRoarRange)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, radius));
    }
}
