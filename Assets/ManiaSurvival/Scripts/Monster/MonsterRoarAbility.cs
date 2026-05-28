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

    [Header("Mana")]
    public string abilityDisplayName = "Roar";
    [Min(0)] public int manaCost = 4;

    [Header("Auto Mana Pool (Monster Defaults)")]
    [Tooltip("Used only if no SurvivorMana exists on this monster yet. Attach SurvivorMana manually to override.")]
    [Min(1)] public int autoMaxMana = 100;
    [Tooltip("Used only if no SurvivorMana exists on this monster yet. Attach SurvivorMana manually to override.")]
    [Min(0f)] public float autoPassiveRegenPerSecond = 5f;

    [Header("UI")]
    public AbilityCooldownButton cooldownButton;

    [Header("Game Manager")]
    public ManiaGameManager gameManager;
    public LocalRoleController localRoleController;

    [Header("Debug")]
    public bool drawRoarRange = true;

    private UnitHealth unitHealth;
    private SurvivorMana mana;
    private float nextCastTime;

    private void Awake()
    {
        unitHealth = GetComponent<UnitHealth>();
        mana = SurvivorMana.EnsureOn(gameObject, manaCost, autoMaxMana, autoPassiveRegenPerSecond);

        if (cooldownButton != null)
        {
            cooldownButton.SetAbilityInfo(abilityDisplayName, manaCost);
        }

        if (gameManager == null)
        {
            gameManager = ManiaGameManager.Instance;
        }

        if (localRoleController == null)
        {
            localRoleController = FindFirstObjectByType<LocalRoleController>();
        }
    }

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<ManiaGameManager>();
        }

        if (localRoleController == null)
        {
            localRoleController = FindFirstObjectByType<LocalRoleController>();
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

        if (localRoleController != null && localRoleController.controlMode != PlayerControlMode.MonsterControlled)
        {
            Debug.Log("Roar blocked: not playing as Monster");
            return;
        }

        if (manaCost > 0)
        {
            if (mana == null)
            {
                mana = SurvivorMana.EnsureOn(gameObject, manaCost, autoMaxMana, autoPassiveRegenPerSecond);
            }

            if (mana == null || !mana.SpendMana(manaCost))
            {
                Debug.Log("Roar blocked: not enough mana");
                return;
            }
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
