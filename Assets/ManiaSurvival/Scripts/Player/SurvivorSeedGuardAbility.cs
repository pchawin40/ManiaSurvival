using UnityEngine;

[RequireComponent(typeof(UnitHealth))]
public class SurvivorSeedGuardAbility : MonoBehaviour
{
    [Header("Seed Guard")]
    public GameObject treantPrefab;
    public float treeSearchRadius = 6f;
    public int maxActiveTreants = 2;
    public float cooldown = 20f;

    [Header("Fallback Summon (No Nearby Tree)")]
    [Tooltip("If true, cast can still succeed by summoning a treant near the survivor when no tree is nearby.")]
    public bool allowFallbackSummonIfNoTree = true;
    [Tooltip("How far from the survivor to try placing a fallback treant.")]
    public float fallbackSummonDistance = 1.8f;
    [Tooltip("How many candidate points to test for fallback placement.")]
    public int fallbackPlacementAttempts = 8;
    [Tooltip("Radius used when checking if fallback placement is clear.")]
    public float fallbackClearanceRadius = 0.45f;
    [Tooltip("Solid blockers for fallback placement checks.")]
    public LayerMask fallbackBlockerLayers = ~0;
    [Tooltip("Hazard layers for fallback placement checks.")]
    public LayerMask fallbackHazardLayers;

    [Header("Mana")]
    public string abilityDisplayName = "Seed Guard";
    [Min(0)] public int manaCost = 6;

    [Header("UI")]
    public AbilityCooldownButton cooldownButton;

    private UnitHealth unitHealth;
    private SurvivorMana mana;
    private float nextCastTime;

    private void Awake()
    {
        unitHealth = GetComponent<UnitHealth>();
        mana = SurvivorMana.EnsureOn(gameObject, manaCost);

        if (cooldownButton != null)
        {
            cooldownButton.SetAbilityInfo(abilityDisplayName, manaCost);
        }
    }

    public void CastSeedGuard()
    {
        if (cooldownButton != null && cooldownButton.IsCoolingDown)
        {
            Debug.Log("Seed Guard on cooldown");
            return;
        }

        if (unitHealth == null)
        {
            unitHealth = GetComponent<UnitHealth>();
        }

        if (unitHealth == null || unitHealth.IsDead)
        {
            Debug.Log("Seed Guard blocked");
            return;
        }

        if (Time.time < nextCastTime)
        {
            Debug.Log("Seed Guard on cooldown");
            return;
        }

        if (treantPrefab == null)
        {
            Debug.Log("Seed Guard blocked");
            return;
        }

        if (GetActiveTreantCount() >= maxActiveTreants)
        {
            Debug.Log("Seed Guard blocked");
            return;
        }

        NeutralTree tree = FindNearestTree();

        Vector3 spawnPosition;
        Quaternion spawnRotation;

        if (tree != null)
        {
            spawnPosition = tree.transform.position;
            spawnRotation = tree.transform.rotation;
        }
        else
        {
            if (!allowFallbackSummonIfNoTree || !TryGetFallbackSpawnPosition(out spawnPosition, out spawnRotation))
            {
                Debug.Log("No nearby tree");
                return;
            }
        }

        if (manaCost > 0)
        {
            if (mana == null)
            {
                mana = SurvivorMana.EnsureOn(gameObject, manaCost);
            }

            if (mana == null || !mana.SpendMana(manaCost))
            {
                Debug.Log("Seed Guard blocked: not enough mana");
                return;
            }
        }

        if (tree != null)
        {
            Destroy(tree.gameObject);
        }

        GameObject treantObject = Instantiate(treantPrefab, spawnPosition, spawnRotation);
        TreantMinion treant = treantObject.GetComponent<TreantMinion>();
        if (treant != null)
        {
            treant.Initialize(transform);
        }

        nextCastTime = Time.time + cooldown;
        if (cooldownButton != null)
        {
            cooldownButton.StartCooldown(cooldown);
        }
    }

    private NeutralTree FindNearestTree()
    {
        NeutralTree[] trees = FindObjectsByType<NeutralTree>(FindObjectsSortMode.None);
        NeutralTree nearest = null;
        float nearestDistanceSqr = treeSearchRadius * treeSearchRadius;

        for (int i = 0; i < trees.Length; i++)
        {
            NeutralTree tree = trees[i];
            if (tree == null)
            {
                continue;
            }

            float distanceSqr = (tree.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr > nearestDistanceSqr)
            {
                continue;
            }

            nearest = tree;
            nearestDistanceSqr = distanceSqr;
        }

        return nearest;
    }

    private int GetActiveTreantCount()
    {
        TreantMinion[] treants = FindObjectsByType<TreantMinion>(FindObjectsSortMode.None);
        int activeCount = 0;

        for (int i = 0; i < treants.Length; i++)
        {
            TreantMinion treant = treants[i];
            if (treant == null)
            {
                continue;
            }

            if (!treant.IsExpired)
            {
                activeCount++;
            }
        }

        return activeCount;
    }

    private bool TryGetFallbackSpawnPosition(out Vector3 spawnPosition, out Quaternion spawnRotation)
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        float distance = Mathf.Max(0.5f, fallbackSummonDistance);
        float clearance = Mathf.Max(0.1f, fallbackClearanceRadius);
        int attempts = Mathf.Max(1, fallbackPlacementAttempts);

        for (int i = 0; i < attempts; i++)
        {
            float t = (float)i / attempts;
            float angle = t * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
            Vector3 candidate = transform.position + offset;

            if (fallbackHazardLayers.value != 0 &&
                Physics.CheckSphere(candidate, clearance, fallbackHazardLayers, QueryTriggerInteraction.Collide))
            {
                continue;
            }

            if (fallbackBlockerLayers.value != 0 &&
                Physics.CheckSphere(candidate, clearance, fallbackBlockerLayers, QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            spawnPosition = candidate;
            Vector3 lookDirection = (candidate - transform.position);
            lookDirection.y = 0f;
            spawnRotation = lookDirection.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(lookDirection.normalized)
                : transform.rotation;
            return true;
        }

        return false;
    }
}
