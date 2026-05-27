using UnityEngine;

[RequireComponent(typeof(UnitHealth))]
public class MonsterStompAbility : MonoBehaviour
{
    [Header("Stomp")]
    public int damage = 20;
    public float radius = 3f;
    public float cooldown = 8f;
    public string targetTag = "Survivor";
    public LayerMask survivorLayers = ~0;

    [Header("UI")]
    public AbilityCooldownButton cooldownButton;

    [Header("Game Manager")]
    public ManiaGameManager gameManager;

    [Header("Debug")]
    public bool drawStompRange = true;

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

    public void CastStomp()
    {
        if (cooldownButton != null && cooldownButton.IsCoolingDown)
        {
            Debug.Log("Stomp on cooldown");
            return;
        }

        if (Time.time < nextCastTime)
        {
            Debug.Log("Stomp on cooldown");
            return;
        }

        if (unitHealth == null)
        {
            unitHealth = GetComponent<UnitHealth>();
        }

        if (unitHealth == null || unitHealth.IsDead)
        {
            Debug.Log("Stomp blocked");
            return;
        }

        if (gameManager != null && gameManager.State != ManiaGameState.Playing)
        {
            Debug.Log("Stomp blocked: game not in play");
            return;
        }

        int hitCount = ApplyStompDamage();

        nextCastTime = Time.time + cooldown;
        if (cooldownButton != null)
        {
            cooldownButton.StartCooldown(cooldown);
        }

        Debug.Log("Stomp cast, survivors hit: " + hitCount);
    }

    private int ApplyStompDamage()
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

    private void OnDrawGizmosSelected()
    {
        if (!drawStompRange)
        {
            return;
        }

        Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, radius));
    }
}
