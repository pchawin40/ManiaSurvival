using System.Collections;
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
///   - Dragon Leap   : telegraphed leap / slam, small AoE ground impact (mana + cooldown gated).
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
    [Tooltip("Master toggle for NPC chaos abilities.")]
    public bool enableChaosAbilities = true;

    [Tooltip("How often the NPC checks whether it can cast a chaos ability.")]
    public float npcAbilityCheckInterval = 2f;

    [Tooltip("If true, only cast while ManiaGameManager reports State == Playing.")]
    public bool castOnlyDuringPlaying = true;

    [Tooltip("Legacy random cast delay — kept for compatibility but unused when chaos loop is active.")]
    public float minCastInterval = 8f;

    [Tooltip("Legacy random cast delay — kept for compatibility but unused when chaos loop is active.")]
    public float maxCastInterval = 14f;

    // ── NPC Mana ──────────────────────────────────────────────────────────────

    [Header("NPC Mana")]
    public bool useNpcMana = true;
    public float npcMaxMana = 100f;
    public float npcManaRegenPerSecond = 5f;

    // ── Ability 4: Dragon Leap ────────────────────────────────────────────────

    [Header("Ability 4 — Dragon Leap")]
    public bool enableDragonLeap = true;
    public float dragonLeapManaCost = 20f;
    public float dragonLeapCooldown = 14f;
    public float dragonLeapTargetRadius = 14f;
    public float dragonLeapWarningDuration = 0.65f;
    public float dragonLeapTravelDuration = 0.35f;
    public float dragonLeapImpactRadius = 3f;
    public int dragonLeapDamage = 8;
    public float dragonLeapArcHeight = 2.5f;

    // ── Shared spawn settings (used by all abilities) ─────────────────────────

    [Header("Shared Spawn Settings")]
    [Tooltip("Fill these once and every ability uses them for valid-position search.")]
    public MapSpawnSettings spawnSettings;

    // ── Ability 1: Tree Patch ─────────────────────────────────────────────────

    [Header("Ability 1 — Chaos Trees")]
    public float chaosTreeManaCost = 10f;
    public float chaosTreeCooldown = 12f;
    public int chaosTreeSpawnCountMin = 2;
    public int chaosTreeSpawnCountMax = 4;
    [Tooltip("Seconds before spawned chaos trees auto-despawn. 0 = persist until destroyed by abilities.")]
    public float chaosTreeLifetime = 0f;
    public float chaosCastRadius = 18f;
    public float minDistanceFromPlayers = 3f;

    [Header("Ability 1 — Tree Patch (shared prefab)")]
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
    public float tornadoManaCost = 15f;
    public float tornadoCooldown = 14f;
    [Tooltip("Optional: assign a prefab that has a TornadoHazard component. " +
             "If empty, a runtime TornadoHazard GameObject is created instead.")]
    public GameObject tornadoPrefab;
    public int tornadoWeight = 3;

    [Header("Ability 4 — Wild Zone")]
    public bool enableWildZone = true;
    public float wildZoneManaCost = 12f;
    public float wildZoneCooldown = 15f;
    public float wildZoneRadius = 3.5f;
    public float wildZoneDuration = 6f;
    public int wildZoneDamagePerTick = 4;
    public int wildZoneWeight = 2;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // ── Internal state ────────────────────────────────────────────────────────

    private enum ChaosAbilityId
    {
        TreePatch,
        SpawnHelpfulItem,
        Tornado,
        DragonLeap,
        WildZone,
    }

    private ManiaGameManager gameManager;
    private UnitMana npcMana;
    private float nextAbilityCheckTime;
    private float nextTreeReadyTime;
    private float nextTornadoReadyTime;
    private float nextWildZoneReadyTime;
    private float nextDragonLeapReadyTime;
    private bool dragonLeapInProgress;
    private readonly List<GameObject> trackedHelpfulItems = new List<GameObject>();

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        gameManager = ManiaGameManager.Instance;

        if (useNpcMana)
        {
            npcMana = UnitMana.EnsureOn(gameObject, false);
            npcMana.maxMana = npcMaxMana;
            npcMana.currentMana = npcMaxMana;
            npcMana.manaRegenPerSecond = npcManaRegenPerSecond;
        }
    }

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<ManiaGameManager>();
        }

        nextAbilityCheckTime = Time.time + Random.Range(0.5f, npcAbilityCheckInterval);
    }

    private void Update()
    {
        if (!enableChaosAbilities)
        {
            return;
        }

        if (castOnlyDuringPlaying && (gameManager == null || !gameManager.IsPlaying))
        {
            return;
        }

        if (dragonLeapInProgress)
        {
            return;
        }

        if (Time.time < nextAbilityCheckTime)
        {
            return;
        }

        nextAbilityCheckTime = Time.time + Mathf.Max(0.5f, npcAbilityCheckInterval);
        TryCastChaosAbility();
    }

    private void TryCastChaosAbility()
    {
        List<ChaosAbilityId> ready = BuildReadyAbilities();
        if (ready.Count == 0)
        {
            return;
        }

        ChaosAbilityId ability = ready[Random.Range(0, ready.Count)];
        switch (ability)
        {
            case ChaosAbilityId.TreePatch:
                if (TryCastChaosTrees())
                {
                    SpendAbilityMana(chaosTreeManaCost);
                    nextTreeReadyTime = Time.time + chaosTreeCooldown;
                }
                break;
            case ChaosAbilityId.Tornado:
                if (TryCastTornado())
                {
                    SpendAbilityMana(tornadoManaCost);
                    nextTornadoReadyTime = Time.time + tornadoCooldown;
                }
                break;
            case ChaosAbilityId.DragonLeap:
                TryBeginDragonLeap();
                break;
            case ChaosAbilityId.WildZone:
                if (TryCastWildZone())
                {
                    SpendAbilityMana(wildZoneManaCost);
                    nextWildZoneReadyTime = Time.time + wildZoneCooldown;
                }
                break;
            case ChaosAbilityId.SpawnHelpfulItem:
                CastSpawnHelpfulItem();
                break;
        }
    }

    private List<ChaosAbilityId> BuildReadyAbilities()
    {
        List<ChaosAbilityId> ready = new List<ChaosAbilityId>(5);

        if (Time.time >= nextTreeReadyTime && HasNpcMana(chaosTreeManaCost) && treePrefab != null)
        {
            ready.Add(ChaosAbilityId.TreePatch);
            ready.Add(ChaosAbilityId.TreePatch);
        }

        if (enableDragonLeap && Time.time >= nextDragonLeapReadyTime && HasNpcMana(dragonLeapManaCost))
        {
            ready.Add(ChaosAbilityId.DragonLeap);
        }

        if (Time.time >= nextTornadoReadyTime && HasNpcMana(tornadoManaCost))
        {
            ready.Add(ChaosAbilityId.Tornado);
            ready.Add(ChaosAbilityId.Tornado);
        }

        if (enableWildZone && Time.time >= nextWildZoneReadyTime && HasNpcMana(wildZoneManaCost))
        {
            ready.Add(ChaosAbilityId.WildZone);
        }

        if (helpfulItemPrefabs != null && helpfulItemPrefabs.Length > 0)
        {
            ready.Add(ChaosAbilityId.SpawnHelpfulItem);
        }

        return ready;
    }

    private bool HasNpcMana(float cost)
    {
        if (!useNpcMana || cost <= 0f)
        {
            return true;
        }

        if (npcMana == null)
        {
            npcMana = UnitMana.EnsureOn(gameObject, false);
        }

        return npcMana != null && npcMana.HasMana(cost);
    }

    private void SpendAbilityMana(float cost)
    {
        if (!useNpcMana || cost <= 0f || npcMana == null)
        {
            return;
        }

        npcMana.SpendMana(cost);
    }

    private bool TryCastChaosTrees()
    {
        LogNpcAbility("Cast Chaos Trees");
        return CastTreePatch(true);
    }

    private bool TryCastTornado()
    {
        LogNpcAbility("Cast Tornado");
        CastTornado();
        return true;
    }

    private bool TryCastWildZone()
    {
        if (!MapSpawnUtility.TryGetValidPositionNear(
                transform.position,
                chaosCastRadius,
                spawnSettings,
                out Vector3 pos))
        {
            return false;
        }

        if (IsTooCloseToAnyPlayer(pos, minDistanceFromPlayers))
        {
            return false;
        }

        GameObject zoneHost = new GameObject("ChaosWildZone");
        zoneHost.transform.position = pos;
        ChaosWildZone zone = zoneHost.AddComponent<ChaosWildZone>();
        zone.Initialize(wildZoneRadius, wildZoneDuration, wildZoneDamagePerTick, gameObject);
        LogNpcAbility("Cast Wild Zone");
        return true;
    }

    private bool IsTooCloseToAnyPlayer(Vector3 pos, float minDistance)
    {
        UnitHealth[] units = FindObjectsByType<UnitHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            UnitHealth unit = units[i];
            if (unit == null || unit.IsDead)
            {
                continue;
            }

            if (!unit.CompareTag("Survivor") && !unit.CompareTag("Monster") && !unit.CompareTag("Predator"))
            {
                continue;
            }

            Vector3 delta = unit.transform.position - pos;
            delta.y = 0f;
            if (delta.sqrMagnitude < minDistance * minDistance)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryBeginDragonLeap()
    {
        if (dragonLeapInProgress)
        {
            return false;
        }

        if (Time.time < nextDragonLeapReadyTime)
        {
            LogNpcAbility("Waiting for cooldown Dragon Leap");
            return false;
        }

        if (useNpcMana)
        {
            if (npcMana == null)
            {
                npcMana = UnitMana.EnsureOn(gameObject, false);
            }

            if (npcMana != null && !npcMana.HasMana(dragonLeapManaCost))
            {
                LogNpcAbility("Not enough mana for Dragon Leap");
                return false;
            }
        }

        if (!TryFindDragonLeapLanding(out Vector3 landing))
        {
            return false;
        }

        StartCoroutine(DragonLeapCoroutine(landing));
        return true;
    }

    private bool TryFindDragonLeapLanding(out Vector3 landing)
    {
        landing = transform.position;

        UnitHealth[] survivors = FindObjectsByType<UnitHealth>(FindObjectsSortMode.None);
        UnitHealth nearest = null;
        float nearestSqr = float.MaxValue;

        for (int i = 0; i < survivors.Length; i++)
        {
            UnitHealth candidate = survivors[i];
            if (candidate == null || candidate.IsDead || !candidate.CompareTag("Survivor"))
            {
                continue;
            }

            float sqr = (candidate.transform.position - transform.position).sqrMagnitude;
            if (sqr <= dragonLeapTargetRadius * dragonLeapTargetRadius && sqr < nearestSqr)
            {
                nearest = candidate;
                nearestSqr = sqr;
            }
        }

        if (nearest != null)
        {
            landing = nearest.transform.position;
            landing.y = transform.position.y;
            return true;
        }

        return MapSpawnUtility.TryGetValidPositionNear(
            transform.position,
            dragonLeapTargetRadius,
            spawnSettings,
            out landing);
    }

    private IEnumerator DragonLeapCoroutine(Vector3 landing)
    {
        dragonLeapInProgress = true;

        if (useNpcMana && npcMana != null)
        {
            if (!npcMana.SpendMana(dragonLeapManaCost))
            {
                LogNpcAbility("Not enough mana for Dragon Leap");
                dragonLeapInProgress = false;
                yield break;
            }

            LogNpcAbility("Cast Dragon Leap, spent " + dragonLeapManaCost.ToString("0") + " mana");
        }
        else
        {
            LogNpcAbility("Cast Dragon Leap");
        }

        nextDragonLeapReadyTime = Time.time + Mathf.Max(0.5f, dragonLeapCooldown);

        GameObject warning = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        warning.name = "DragonLeapWarning";
        Collider warningCollider = warning.GetComponent<Collider>();
        if (warningCollider != null)
        {
            Destroy(warningCollider);
        }

        warning.transform.position = landing + Vector3.up * 0.05f;
        warning.transform.localScale = new Vector3(dragonLeapImpactRadius * 2f, 0.08f, dragonLeapImpactRadius * 2f);
        Renderer warningRenderer = warning.GetComponent<Renderer>();
        if (warningRenderer != null)
        {
            Material warningMat = new Material(Shader.Find("Standard"));
            warningMat.color = new Color(1f, 0.55f, 0.1f, 0.55f);
            warningRenderer.material = warningMat;
        }

        yield return new WaitForSeconds(Mathf.Max(0.05f, dragonLeapWarningDuration));
        if (warning != null)
        {
            Destroy(warning);
        }

        Vector3 start = transform.position;
        float duration = Mathf.Max(0.05f, dragonLeapTravelDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 flat = Vector3.Lerp(start, landing, t);
            flat.y = start.y + Mathf.Sin(t * Mathf.PI) * dragonLeapArcHeight;
            transform.position = flat;
            yield return null;
        }

        transform.position = landing;
        ApplyDragonLeapImpact(landing);
        dragonLeapInProgress = false;
    }

    private void ApplyDragonLeapImpact(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, dragonLeapImpactRadius, ~0, QueryTriggerInteraction.Ignore);
        int hitCount = 0;

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth health = hits[i].GetComponentInParent<UnitHealth>();
            if (health == null || health.IsDead)
            {
                continue;
            }

            if (!health.CompareTag("Survivor") && !health.CompareTag("Monster") && !health.CompareTag("Predator"))
            {
                continue;
            }

            health.TakeDamage(Mathf.Max(1, dragonLeapDamage), gameObject);
            hitCount++;
        }

        TemporaryGroundEffect.Spawn(
            center,
            new Color(0.85f, 0.35f, 0.1f, 0.7f),
            1.2f,
            dragonLeapImpactRadius,
            null,
            enableDebugLogs);

        if (enableDebugLogs)
        {
            LogNpcAbility("Dragon Leap impact hit " + hitCount + " units");
        }
    }

    private void LogNpcAbility(string message)
    {
        Debug.Log("[NPCAbility] " + message);
    }

    // ── ABILITY 1: Chaos Trees ─────────────────────────────────────────────────

    private bool CastTreePatch(bool chaosMode = false)
    {
        if (treePrefab == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[ChaosCaster] Tree patch skipped: treePrefab not assigned.");
            }

            return false;
        }

        Vector3 searchCenter = transform.position;
        if (!MapSpawnUtility.TryGetValidPositionNear(searchCenter, chaosCastRadius, spawnSettings, out Vector3 patchCenter))
        {
            if (!MapSpawnUtility.TryGetValidPosition(spawnSettings, out patchCenter))
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning("[ChaosCaster] Tree patch skipped: no valid centre position found.");
                }

                return false;
            }
        }

        NeutralTree[] existing = FindObjectsByType<NeutralTree>(FindObjectsSortMode.None);
        int minCount = chaosMode ? chaosTreeSpawnCountMin : treePatchMinCount;
        int maxCount = chaosMode ? chaosTreeSpawnCountMax : treePatchMaxCount;
        int targetCount = Random.Range(
            Mathf.Max(1, minCount),
            Mathf.Max(1, maxCount) + 1);

        int spawned = 0;
        Vector3[] batchPos = new Vector3[targetCount];
        float playerBuffer = chaosMode ? minDistanceFromPlayers : 2f;

        for (int i = 0; i < targetCount; i++)
        {
            if (!MapSpawnUtility.TryGetValidPositionNear(patchCenter, treePatchRadius, spawnSettings, out Vector3 treePos))
            {
                continue;
            }

            if (IsTooCloseToAnyPlayer(treePos, playerBuffer))
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

            if (chaosMode && chaosTreeLifetime > 0f)
            {
                Destroy(t.gameObject, chaosTreeLifetime);
            }
        }

        if (enableDebugLogs)
        {
            Debug.Log("[ChaosCaster] Tree patch: " + spawned + "/" + targetCount +
                      " trees spawned near " + patchCenter.ToString("F2"));
        }

        return spawned > 0;
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

public class ChaosWildZone : MonoBehaviour
{
    private float radius;
    private float duration;
    private int damagePerTick;
    private GameObject damageSource;
    private float tickInterval = 1f;
    private float nextTickTime;

    public void Initialize(float zoneRadius, float zoneDuration, int tickDamage, GameObject source)
    {
        radius = Mathf.Max(0.5f, zoneRadius);
        duration = Mathf.Max(0.5f, zoneDuration);
        damagePerTick = Mathf.Max(1, tickDamage);
        damageSource = source;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "WildZoneVisual";
        Collider col = visual.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = Vector3.up * 0.05f;
        visual.transform.localScale = new Vector3(radius * 2f, 0.06f, radius * 2f);
        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0.55f, 0.15f, 0.85f, 0.35f);
            renderer.material = mat;
        }

        Destroy(gameObject, duration);
    }

    private void Update()
    {
        if (Time.time < nextTickTime)
        {
            return;
        }

        nextTickTime = Time.time + tickInterval;
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth health = hits[i].GetComponentInParent<UnitHealth>();
            if (health == null || health.IsDead)
            {
                continue;
            }

            if (!health.CompareTag("Survivor") && !health.CompareTag("Monster") && !health.CompareTag("Predator"))
            {
                continue;
            }

            health.TakeDamage(damagePerTick, damageSource != null ? damageSource : gameObject);
        }
    }
}
