using UnityEngine;

/// <summary>
/// Short-lived AoE hazard spawned by NPCChaosCaster.
/// Deals modest damage ticks to any unit inside its radius and optionally destroys nearby trees.
/// Uses a cylinder primitive as a placeholder visual — no art assets required.
/// </summary>
[DisallowMultipleComponent]
public class TornadoHazard : MonoBehaviour
{
    [Header("Hazard")]
    [Tooltip("How long the tornado lasts before self-destructing.")]
    public float duration = 4f;

    [Tooltip("Radius of the damage area.")]
    public float damageRadius = 2.5f;

    [Tooltip("HP dealt per damage tick to units inside the radius. Never one-shots.")]
    public int damagePerTick = 10;

    [Tooltip("Seconds between damage ticks.")]
    public float tickInterval = 1f;

    [Tooltip("If true, trees inside the radius are destroyed each tick.")]
    public bool destroyTreesInRadius = false;

    [Header("Target Tags")]
    [Tooltip("Tags that can be damaged. Leave empty to hit everything with a UnitHealth.")]
    public string[] targetTags = { "Survivor", "Monster" };

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private float remainingLifetime;
    private float tickCooldown;

    private void Start()
    {
        remainingLifetime = Mathf.Max(0.1f, duration);
        tickCooldown = 0f;

        BuildPlaceholderVisual();

        if (enableDebugLogs)
        {
            Debug.Log("[Tornado] Spawned at " + transform.position.ToString("F2") +
                      " — lasts " + duration + "s, radius " + damageRadius);
        }
    }

    private void Update()
    {
        remainingLifetime -= Time.deltaTime;

        if (remainingLifetime <= 0f)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[Tornado] Expired.");
            }

            Destroy(gameObject);
            return;
        }

        tickCooldown -= Time.deltaTime;

        if (tickCooldown > 0f)
        {
            return;
        }

        tickCooldown = Mathf.Max(0.1f, tickInterval);
        ApplyDamageTick();

        if (destroyTreesInRadius)
        {
            DestroyNearbyTrees();
        }
    }

    private void ApplyDamageTick()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            Mathf.Max(0.1f, damageRadius),
            ~0,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth health = hits[i].GetComponentInParent<UnitHealth>();

            if (health == null || health.IsDead)
            {
                continue;
            }

            if (!HasTargetTag(health))
            {
                continue;
            }

            // Never one-shot: cap so at least 1 HP remains.
            int dmg = Mathf.Min(Mathf.Max(1, damagePerTick), health.currentHealth - 1);

            if (dmg <= 0)
            {
                continue;
            }

            health.TakeDamage(dmg, gameObject);

            if (enableDebugLogs)
            {
                Debug.Log("[Tornado] Hit " + health.name + " for " + dmg + " HP.");
            }
        }
    }

    private void DestroyNearbyTrees()
    {
        NeutralTree[] trees = FindObjectsByType<NeutralTree>(FindObjectsSortMode.None);

        for (int i = 0; i < trees.Length; i++)
        {
            if (trees[i] == null)
            {
                continue;
            }

            if (Vector3.Distance(transform.position, trees[i].transform.position) > damageRadius)
            {
                continue;
            }

            UnitHealth treeHealth = trees[i].GetComponent<UnitHealth>();

            if (treeHealth != null)
            {
                treeHealth.TakeDamage(treeHealth.maxHealth, gameObject);
            }
            else
            {
                Destroy(trees[i].gameObject);
            }
        }
    }

    private bool HasTargetTag(UnitHealth unit)
    {
        if (targetTags == null || targetTags.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < targetTags.Length; i++)
        {
            if (!string.IsNullOrEmpty(targetTags[i]) && unit.CompareTag(targetTags[i]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Creates a tinted cylinder as a placeholder visual so the tornado is visible
    /// without requiring any art asset. Destroyed automatically with this GameObject.
    /// </summary>
    private void BuildPlaceholderVisual()
    {
        // Only build the placeholder when no renderer already exists on this GO.
        if (GetComponentInChildren<Renderer>() != null)
        {
            return;
        }

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "TornadoVisual";
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = new Vector3(damageRadius * 2f, 0.6f, damageRadius * 2f);

        // Remove the auto-added collider so it does not interfere with physics queries.
        Collider col = visual.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        Renderer rend = visual.GetComponent<Renderer>();
        if (rend != null)
        {
            // Instance the material so we don't tint the shared primitive material.
            rend.material = new Material(rend.sharedMaterial)
            {
                color = new Color(0.55f, 0.7f, 1f, 0.5f)
            };
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.6f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, damageRadius));
    }
}
