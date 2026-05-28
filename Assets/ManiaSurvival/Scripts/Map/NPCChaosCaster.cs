using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Modular chaos-ability caster that can be attached to any neutral/Heaven NPC.
/// Periodically picks a weighted-random ability and executes it at a valid map position.
/// Only active while ManiaGameManager.IsPlaying.
///
/// Starter abilities
///   1. Tree Patch       — scatter 4–6 trees near a random valid point.
///   2. Spawn Helpful    — drop a random consumable (Invisibility Scroll, etc.) at a valid point.
///   3. Tornado          — summon a short-lived TornadoHazard at a valid point.
///
/// TODO (not yet implemented — add extension points below when stable):
///   - Dragon Slam   : telegraphed leap / slam, small AoE ground impact.
///   - Stampede      : moving hazard wave that crosses the map.
///   - Tsunami       : directional shockwave launched from one map edge.
///   - Healing Rain  : brief AoE heal zone for survivors.
///   - Vine Trap     : temporarily root / immobilise a unit.
///   - Meteor Shower : rapid series of small-radius impact explosions.
///   - Holy Shield   : short-duration invulnerability field for survivors.
/// </summary>
[DisallowMultipleComponent]
public class NPCChaosCaster : MonoBehaviour
{
    // ── Cast timing ───────────────────────────────────────────────────────────

    [Header("Chaos Casting")]
    public bool enableChaosCasting = true;

    [Tooltip("Minimum seconds between chaos casts.")]
    public float minCastInterval = 20f;

    [Tooltip("Maximum seconds between chaos casts.")]
    public float maxCastInterval = 45f;

    [Tooltip("If true, only cast while ManiaGameManager reports State == Playing.")]
    public bool castOnlyDuringPlaying = true;

    // ── Shared spawn settings (used by all abilities) ─────────────────────────

    [Header("Shared Spawn Settings")]
    [Tooltip("Fill these once and every ability uses them for valid-position search.")]
    public MapSpawnSettings spawnSettings;

    // ── Ability 1: Tree Patch ─────────────────────────────────────────────────

    [Header("Ability 1 — Tree Patch")]
    public NeutralTree treePrefab;
    public Transform treeParent;
    public int treePatchMinCount = 4;
    public int treePatchMaxCount = 6;
    public float treePatchRadius = 3f;
    public float treeSpacing = 1.2f;
    [Tooltip("Higher = more likely to be selected relative to other abilities.")]
    public int treePatchWeight = 3;

    // ── Ability 2: Spawn Helpful Item ─────────────────────────────────────────

    [Header("Ability 2 — Spawn Helpful Item")]
    [Tooltip("Any world-item prefabs the caster can drop (InvisibilityScroll, etc.).")]
    public GameObject[] helpfulItemPrefabs;

    [Tooltip("If false and a SpeedBootsPickup already exists in the world, Speed Boots won't be duplicated.")]
    public bool allowDuplicateSpeedBoots = false;

    [Tooltip("Maximum total helpful items on the map before this ability is skipped.")]
    public int maxActiveHelpfulItems = 3;

    public int helpfulItemWeight = 2;

    // ── Ability 3: Tornado ────────────────────────────────────────────────────

    [Header("Ability 3 — Tornado")]
    [Tooltip("Optional: assign a prefab that has a TornadoHazard component. " +
             "If empty, a runtime TornadoHazard GameObject is created instead.")]
    public GameObject tornadoPrefab;
    public int tornadoWeight = 1;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // ── Internal state ────────────────────────────────────────────────────────

    private enum ChaosAbilityId
    {
        TreePatch,
        SpawnHelpfulItem,
        Tornado,
        // Future abilities go here as new enum values — no other code needs to change.
    }

    private ManiaGameManager gameManager;
    private float nextCastTime;
    private readonly List<GameObject> trackedHelpfulItems = new List<GameObject>();

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        gameManager = ManiaGameManager.Instance;
    }

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<ManiaGameManager>();
        }

        ScheduleNextCast();
    }

    private void Update()
    {
        if (!enableChaosCasting)
        {
            return;
        }

        if (castOnlyDuringPlaying && (gameManager == null || !gameManager.IsPlaying))
        {
            return;
        }

        if (Time.time < nextCastTime)
        {
            return;
        }

        CastRandomAbility();
        ScheduleNextCast();
    }

    // ── Scheduling ────────────────────────────────────────────────────────────

    private void ScheduleNextCast()
    {
        float lo = Mathf.Min(minCastInterval, maxCastInterval);
        float hi = Mathf.Max(minCastInterval, maxCastInterval);
        nextCastTime = Time.time + Random.Range(lo, hi);

        if (enableDebugLogs)
        {
            Debug.Log("[ChaosCaster] Next chaos cast scheduled at T=" + nextCastTime.ToString("0.0") + "s");
        }
    }

    // ── Ability dispatch ──────────────────────────────────────────────────────

    private void CastRandomAbility()
    {
        ChaosAbilityId ability = PickWeightedAbility();

        if (enableDebugLogs)
        {
            Debug.Log("[ChaosCaster] Casting: " + ability);
        }

        switch (ability)
        {
            case ChaosAbilityId.TreePatch:
                CastTreePatch();
                break;

            case ChaosAbilityId.SpawnHelpfulItem:
                CastSpawnHelpfulItem();
                break;

            case ChaosAbilityId.Tornado:
                CastTornado();
                break;
        }
    }

    private ChaosAbilityId PickWeightedAbility()
    {
        int wTree = Mathf.Max(0, treePatchWeight);
        int wItem = Mathf.Max(0, helpfulItemWeight);
        int wTorn = Mathf.Max(0, tornadoWeight);
        int total = Mathf.Max(1, wTree + wItem + wTorn);

        int roll = Random.Range(0, total);

        if (roll < wTree)
        {
            return ChaosAbilityId.TreePatch;
        }

        if (roll < wTree + wItem)
        {
            return ChaosAbilityId.SpawnHelpfulItem;
        }

        return ChaosAbilityId.Tornado;
    }

    // ── ABILITY 1: Tree Patch ─────────────────────────────────────────────────

    private void CastTreePatch()
    {
        if (treePrefab == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[ChaosCaster] Tree patch skipped: treePrefab not assigned.");
            }

            return;
        }

        if (!MapSpawnUtility.TryGetValidPosition(spawnSettings, out Vector3 patchCenter))
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[ChaosCaster] Tree patch skipped: no valid centre position found.");
            }

            return;
        }

        NeutralTree[] existing = FindObjectsByType<NeutralTree>(FindObjectsSortMode.None);
        int targetCount = Random.Range(
            Mathf.Max(1, treePatchMinCount),
            Mathf.Max(1, treePatchMaxCount) + 1);

        int spawned = 0;
        Vector3[] batchPos = new Vector3[targetCount];

        for (int i = 0; i < targetCount; i++)
        {
            if (!MapSpawnUtility.TryGetValidPositionNear(patchCenter, treePatchRadius, spawnSettings, out Vector3 treePos))
            {
                continue;
            }

            if (IsTooCloseToAny(treePos, existing, treeSpacing) ||
                IsTooCloseToBatch(treePos, batchPos, spawned, treeSpacing))
            {
                continue;
            }

            NeutralTree t = Instantiate(treePrefab, treePos, Quaternion.identity, treeParent);
            t.name = "ChaosTree_Spawned";
            t.SetBlocksMovement(true);
            batchPos[spawned] = treePos;
            spawned++;
        }

        if (enableDebugLogs)
        {
            Debug.Log("[ChaosCaster] Tree patch: " + spawned + "/" + targetCount +
                      " trees spawned near " + patchCenter.ToString("F2"));
        }
    }

    private bool IsTooCloseToAny(Vector3 pos, NeutralTree[] trees, float minDist)
    {
        for (int i = 0; i < trees.Length; i++)
        {
            if (trees[i] == null)
            {
                continue;
            }

            if (Vector3.Distance(trees[i].transform.position, pos) < minDist)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsTooCloseToBatch(Vector3 pos, Vector3[] batch, int count, float minDist)
    {
        for (int i = 0; i < count; i++)
        {
            if (Vector3.Distance(batch[i], pos) < minDist)
            {
                return true;
            }
        }

        return false;
    }

    // ── ABILITY 2: Spawn Helpful Item ─────────────────────────────────────────

    private void CastSpawnHelpfulItem()
    {
        if (helpfulItemPrefabs == null || helpfulItemPrefabs.Length == 0)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[ChaosCaster] Helpful item skipped: helpfulItemPrefabs is empty.");
            }

            return;
        }

        // Purge destroyed objects before checking the cap.
        trackedHelpfulItems.RemoveAll(o => o == null);

        if (trackedHelpfulItems.Count >= maxActiveHelpfulItems)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[ChaosCaster] Helpful item skipped: " + trackedHelpfulItems.Count +
                          "/" + maxActiveHelpfulItems + " items already active.");
            }

            return;
        }

        // Build the list of eligible prefabs.
        bool speedBootsInWorld = FindFirstObjectByType<SpeedBootsPickup>() != null;
        List<GameObject> candidates = new List<GameObject>(helpfulItemPrefabs.Length);

        for (int i = 0; i < helpfulItemPrefabs.Length; i++)
        {
            if (helpfulItemPrefabs[i] == null)
            {
                continue;
            }

            if (!allowDuplicateSpeedBoots && speedBootsInWorld &&
                helpfulItemPrefabs[i].GetComponent<SpeedBootsPickup>() != null)
            {
                continue;
            }

            candidates.Add(helpfulItemPrefabs[i]);
        }

        if (candidates.Count == 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[ChaosCaster] Helpful item skipped: no eligible prefabs after filtering.");
            }

            return;
        }

        if (!MapSpawnUtility.TryGetValidPosition(spawnSettings, out Vector3 pos))
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[ChaosCaster] Helpful item skipped: no valid spawn position found.");
            }

            return;
        }

        GameObject chosen = candidates[Random.Range(0, candidates.Count)];
        GameObject item = Instantiate(chosen, pos, Quaternion.identity);
        trackedHelpfulItems.Add(item);

        if (enableDebugLogs)
        {
            Debug.Log("[ChaosCaster] Helpful item spawned: " + chosen.name +
                      " at " + pos.ToString("F2"));
        }
    }

    // ── ABILITY 3: Tornado ────────────────────────────────────────────────────

    private void CastTornado()
    {
        if (!MapSpawnUtility.TryGetValidPosition(spawnSettings, out Vector3 pos))
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[ChaosCaster] Tornado skipped: no valid spawn position found.");
            }

            return;
        }

        if (tornadoPrefab != null)
        {
            Instantiate(tornadoPrefab, pos, Quaternion.identity);
        }
        else
        {
            // No prefab assigned — create a runtime TornadoHazard with default settings.
            GameObject go = new GameObject("TornadoHazard");
            go.transform.position = pos;
            go.AddComponent<TornadoHazard>();
        }

        if (enableDebugLogs)
        {
            Debug.Log("[ChaosCaster] Tornado spawned at " + pos.ToString("F2"));
        }
    }
}
