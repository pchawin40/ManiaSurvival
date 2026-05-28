using UnityEngine;

[RequireComponent(typeof(UnitHealth))]
public class SurvivorSeedGuardAbility : MonoBehaviour
{
    [Header("Seed Guard")]
    public GameObject treantPrefab;
    public float treeSearchRadius = 6f;
    public int maxActiveTreants = 2;
    public float cooldown = 20f;

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
        if (tree == null)
        {
            Debug.Log("No nearby tree");
            return;
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

        Vector3 spawnPosition = tree.transform.position;
        Quaternion spawnRotation = tree.transform.rotation;

        Destroy(tree.gameObject);

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
}
