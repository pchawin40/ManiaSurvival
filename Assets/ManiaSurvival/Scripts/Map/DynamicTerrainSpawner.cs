using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central helper for spawning temporary battlefield terrain props safely.
/// </summary>
[DisallowMultipleComponent]
public class DynamicTerrainSpawner : MonoBehaviour
{
    public static DynamicTerrainSpawner Instance { get; private set; }

    public const string RuntimeParentName = "DynamicTerrainRuntime";

    [Header("Prefabs (optional)")]
    public GameObject wallPrefab;
    public GameObject rampPrefab;
    public GameObject platformPrefab;
    public GameObject craterPrefab;
    public GameObject fireZonePrefab;
    public GameObject slowZonePrefab;
    public GameObject rockPrefab;
    public GameObject jumpPadPrefab;
    public GameObject vineWallPrefab;
    public GameObject bridgePrefab;

    [Header("Limits")]
    public int maxDynamicTerrainProps = 35;
    public int maxActiveCraterMarks = 20;
    public int maxActiveFireZones = 8;
    public int maxActiveWalls = 10;

    [Header("Spawn Safety")]
    public float minDistanceFromPlayers = 1.5f;
    public float defaultWarningDuration = 0.35f;
    public bool enableDebugLogs = true;

    private Transform runtimeParent;
    private readonly List<DynamicTerrainProp> allProps = new List<DynamicTerrainProp>();
    private readonly List<DynamicTerrainProp> craterProps = new List<DynamicTerrainProp>();
    private readonly List<DynamicTerrainProp> fireZoneProps = new List<DynamicTerrainProp>();
    private readonly List<DynamicTerrainProp> wallProps = new List<DynamicTerrainProp>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapAfterSceneLoad()
    {
        if (FindFirstObjectByType<DynamicTerrainSpawner>() != null)
        {
            return;
        }

        GameObject managers = GameObject.Find("Managers");
        GameObject host = new GameObject("DynamicTerrainSpawner");
        if (managers != null)
        {
            host.transform.SetParent(managers.transform, false);
        }

        host.AddComponent<DynamicTerrainSpawner>();
        host.AddComponent<TerrainDebugHotkeys>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        EnsureRuntimeParent();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static DynamicTerrainSpawner GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        DynamicTerrainSpawner existing = FindFirstObjectByType<DynamicTerrainSpawner>();
        if (existing != null)
        {
            return existing;
        }

        GameObject host = new GameObject("DynamicTerrainSpawner");
        return host.AddComponent<DynamicTerrainSpawner>();
    }

    public DynamicTerrainProp SpawnWall(Vector3 position, Quaternion rotation, float length, float duration)
    {
        if (!TryPrepareSpawn(position, DynamicTerrainPropType.Wall, out Vector3 safePos))
        {
            return null;
        }

        if (!EnforceTypeLimit(DynamicTerrainPropType.Wall))
        {
            return null;
        }

        GameObject instance = CreateFromPrefabOrFallback(wallPrefab, "DynamicWall", safePos, rotation);
        instance.transform.localScale = new Vector3(Mathf.Max(0.5f, length), 1.2f, 0.55f);

        DynamicTerrainProp prop = ConfigureProp(
            instance,
            DynamicTerrainPropType.Wall,
            duration,
            defaultWarningDuration,
            blockMovement: true,
            survivorFx: false,
            predatorFx: false,
            dps: 0f,
            slowMult: 1f);

        RegisterProp(prop, wallProps);
        LogSpawn("Wall", safePos);
        return prop;
    }

    public DynamicTerrainProp SpawnRamp(Vector3 position, Quaternion rotation, float duration)
    {
        if (!TryPrepareSpawn(position, DynamicTerrainPropType.Ramp, out Vector3 safePos))
        {
            return null;
        }

        GameObject instance = CreateFromPrefabOrFallback(rampPrefab, "DynamicRamp", safePos, rotation);
        if (rampPrefab == null)
        {
            instance.transform.localScale = new Vector3(2.4f, 0.35f, 3.2f);
            Transform body = instance.transform.childCount > 0 ? instance.transform.GetChild(0) : instance.transform;
            body.localRotation = Quaternion.Euler(18f, 0f, 0f);
        }

        DynamicTerrainProp prop = ConfigureProp(
            instance,
            DynamicTerrainPropType.Ramp,
            duration,
            0f,
            blockMovement: true,
            survivorFx: false,
            predatorFx: false,
            dps: 0f,
            slowMult: 1f);

        RegisterProp(prop, null);
        LogSpawn("Ramp", safePos);
        return prop;
    }

    public DynamicTerrainProp SpawnPlatform(Vector3 position, Vector3 size, float duration)
    {
        if (!TryPrepareSpawn(position, DynamicTerrainPropType.Platform, out Vector3 safePos))
        {
            return null;
        }

        GameObject instance = CreateFromPrefabOrFallback(platformPrefab, "DynamicPlatform", safePos, Quaternion.identity);
        instance.transform.localScale = new Vector3(Mathf.Max(1f, size.x), Mathf.Max(0.2f, size.y), Mathf.Max(1f, size.z));

        DynamicTerrainProp prop = ConfigureProp(
            instance,
            DynamicTerrainPropType.Platform,
            duration,
            0f,
            blockMovement: true,
            survivorFx: false,
            predatorFx: false,
            dps: 0f,
            slowMult: 1f);

        RegisterProp(prop, null);
        LogSpawn("Platform", safePos);
        return prop;
    }

    public DynamicTerrainProp SpawnCrater(Vector3 position, float radius, float duration)
    {
        if (!TryPrepareSpawn(position, DynamicTerrainPropType.Crater, out Vector3 safePos))
        {
            return null;
        }

        if (!EnforceTypeLimit(DynamicTerrainPropType.Crater))
        {
            return null;
        }

        float safeRadius = Mathf.Max(0.35f, radius);
        GameObject instance = CreateFromPrefabOrFallback(craterPrefab, "DynamicCrater", safePos, Quaternion.identity);
        instance.transform.localScale = new Vector3(safeRadius * 2f, 0.06f, safeRadius * 2f);

        DynamicTerrainProp prop = ConfigureProp(
            instance,
            DynamicTerrainPropType.Crater,
            duration,
            0f,
            blockMovement: false,
            survivorFx: false,
            predatorFx: false,
            dps: 0f,
            slowMult: 1f);

        RegisterProp(prop, craterProps);
        LogSpawn("Crater", safePos);
        return prop;
    }

    public DynamicTerrainProp SpawnFireZone(Vector3 position, float radius, float duration, float dps)
    {
        if (!TryPrepareSpawn(position, DynamicTerrainPropType.FireZone, out Vector3 safePos))
        {
            return null;
        }

        if (!EnforceTypeLimit(DynamicTerrainPropType.FireZone))
        {
            return null;
        }

        float safeRadius = Mathf.Max(0.5f, radius);
        GameObject instance = CreateFromPrefabOrFallback(fireZonePrefab, "DynamicFireZone", safePos, Quaternion.identity);
        instance.transform.localScale = new Vector3(safeRadius * 2f, 0.05f, safeRadius * 2f);

        DynamicTerrainProp prop = ConfigureProp(
            instance,
            DynamicTerrainPropType.FireZone,
            duration,
            0.15f,
            blockMovement: false,
            survivorFx: true,
            predatorFx: false,
            dps: Mathf.Max(0f, dps),
            slowMult: 1f);

        RegisterProp(prop, fireZoneProps);
        LogSpawn("FireZone", safePos);
        return prop;
    }

    public DynamicTerrainProp SpawnSlowZone(Vector3 position, float radius, float duration, float slowMultiplier)
    {
        if (!TryPrepareSpawn(position, DynamicTerrainPropType.SlowZone, out Vector3 safePos))
        {
            return null;
        }

        float safeRadius = Mathf.Max(0.5f, radius);
        GameObject instance = CreateFromPrefabOrFallback(slowZonePrefab, "DynamicSlowZone", safePos, Quaternion.identity);
        instance.transform.localScale = new Vector3(safeRadius * 2f, 0.05f, safeRadius * 2f);

        DynamicTerrainProp prop = ConfigureProp(
            instance,
            DynamicTerrainPropType.SlowZone,
            duration,
            0.15f,
            blockMovement: false,
            survivorFx: true,
            predatorFx: false,
            dps: 0f,
            slowMult: Mathf.Clamp(slowMultiplier, 0.1f, 1f));

        RegisterProp(prop, null);
        LogSpawn("SlowZone", safePos);
        return prop;
    }

    public DynamicTerrainProp SpawnRockObstacle(Vector3 position, float duration)
    {
        if (!TryPrepareSpawn(position, DynamicTerrainPropType.Rock, out Vector3 safePos))
        {
            return null;
        }

        GameObject instance = CreateFromPrefabOrFallback(rockPrefab, "DynamicRock", safePos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        if (rockPrefab == null)
        {
            instance.transform.localScale = Vector3.one * Random.Range(0.75f, 1.1f);
        }

        DynamicTerrainProp prop = ConfigureProp(
            instance,
            DynamicTerrainPropType.Rock,
            duration,
            defaultWarningDuration,
            blockMovement: true,
            survivorFx: false,
            predatorFx: false,
            dps: 0f,
            slowMult: 1f);

        RegisterProp(prop, null);
        LogSpawn("Rock", safePos);
        return prop;
    }

    public DynamicTerrainProp SpawnVineWall(Vector3 position, Quaternion rotation, float length, float duration)
    {
        if (!TryPrepareSpawn(position, DynamicTerrainPropType.VineWall, out Vector3 safePos))
        {
            return null;
        }

        if (!EnforceTypeLimit(DynamicTerrainPropType.VineWall))
        {
            return null;
        }

        GameObject instance = CreateFromPrefabOrFallback(vineWallPrefab != null ? vineWallPrefab : wallPrefab, "DynamicVineWall", safePos, rotation);
        instance.transform.localScale = new Vector3(Mathf.Max(0.5f, length), 1.1f, 0.45f);

        DynamicTerrainProp prop = ConfigureProp(
            instance,
            DynamicTerrainPropType.VineWall,
            duration,
            defaultWarningDuration,
            blockMovement: true,
            survivorFx: false,
            predatorFx: false,
            dps: 0f,
            slowMult: 1f);

        RegisterProp(prop, wallProps);
        LogSpawn("VineWall", safePos);
        return prop;
    }

    public DynamicTerrainProp SpawnJumpPad(Vector3 position, float duration)
    {
        if (!TryPrepareSpawn(position, DynamicTerrainPropType.JumpPad, out Vector3 safePos))
        {
            return null;
        }

        GameObject instance = CreateFromPrefabOrFallback(jumpPadPrefab, "DynamicJumpPad", safePos, Quaternion.identity);
        if (jumpPadPrefab == null)
        {
            instance.transform.localScale = new Vector3(1.6f, 0.08f, 1.6f);
        }

        JumpPad jumpPad = instance.GetComponent<JumpPad>();
        if (jumpPad == null)
        {
            jumpPad = instance.AddComponent<JumpPad>();
        }

        DynamicTerrainProp prop = ConfigureProp(
            instance,
            DynamicTerrainPropType.JumpPad,
            duration,
            0f,
            blockMovement: false,
            survivorFx: false,
            predatorFx: false,
            dps: 0f,
            slowMult: 1f);

        RegisterProp(prop, null);
        LogSpawn("JumpPad", safePos);
        return prop;
    }

    public void SpawnConeScorchMarks(Vector3 origin, Vector3 forward, float range, float halfAngle, int markCount, float radius, float duration)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();
        markCount = Mathf.Max(1, markCount);

        for (int i = 0; i < markCount; i++)
        {
            float t = (i + 1f) / (markCount + 1f);
            float yaw = Random.Range(-halfAngle * 0.85f, halfAngle * 0.85f);
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * forward;
            Vector3 point = origin + dir * (range * t);
            SpawnCrater(point, radius, duration);
        }
    }

    public void SpawnPoisonZone(Vector3 position, float radius, float duration, float dps, float slowMultiplier)
    {
        DynamicTerrainProp zone = SpawnSlowZone(position, radius, duration, slowMultiplier);
        if (zone == null)
        {
            return;
        }

        zone.damagePerSecond = Mathf.Max(0f, dps);
        zone.affectsSurvivors = true;
    }

    internal static void NotifyPropRemoved(DynamicTerrainProp prop)
    {
        if (Instance == null || prop == null)
        {
            return;
        }

        Instance.allProps.Remove(prop);
        Instance.craterProps.Remove(prop);
        Instance.fireZoneProps.Remove(prop);
        Instance.wallProps.Remove(prop);
    }

    private bool TryPrepareSpawn(Vector3 position, DynamicTerrainPropType type, out Vector3 safePosition)
    {
        safePosition = SnapToGround(position);

        if (PlayableBoundsHelper.IsForbiddenSpawnPosition(safePosition, 1.25f))
        {
            if (enableDebugLogs)
            {
                Debug.Log("[Terrain] Blocked spawn: forbidden zone at " + safePosition.ToString("F1"));
            }

            return false;
        }

        if (!PlayableBoundsHelper.IsPositionInsidePlayableBounds(safePosition))
        {
            if (enableDebugLogs)
            {
                Debug.Log("[Terrain] Blocked spawn: outside playable bounds at " + safePosition.ToString("F1"));
            }

            return false;
        }

        if (ArenaBounds.Instance != null)
        {
            safePosition = ArenaBounds.Instance.ClampPosition(safePosition);
        }

        if (IsTooCloseToAnyPlayer(safePosition))
        {
            if (type == DynamicTerrainPropType.Wall
                || type == DynamicTerrainPropType.VineWall
                || type == DynamicTerrainPropType.Rock)
            {
                if (enableDebugLogs)
                {
                    Debug.Log("[Terrain] Blocked spawn: too close to player");
                }

                return false;
            }

            Vector3 push = Random.insideUnitSphere;
            push.y = 0f;
            if (push.sqrMagnitude <= 0.01f)
            {
                push = Vector3.right;
            }

            safePosition += push.normalized * minDistanceFromPlayers;
            if (ArenaBounds.Instance != null)
            {
                safePosition = ArenaBounds.Instance.ClampPosition(safePosition);
            }
        }

        PruneNullProps();

        if (allProps.Count >= maxDynamicTerrainProps)
        {
            RemoveOldest(allProps);
            if (enableDebugLogs)
            {
                Debug.Log("[Terrain] Spawn skipped: active limit reached (removed oldest prop).");
            }
        }

        return true;
    }

    private bool EnforceTypeLimit(DynamicTerrainPropType type)
    {
        List<DynamicTerrainProp> list = null;
        int limit = 0;

        switch (type)
        {
            case DynamicTerrainPropType.Crater:
                list = craterProps;
                limit = maxActiveCraterMarks;
                break;
            case DynamicTerrainPropType.FireZone:
                list = fireZoneProps;
                limit = maxActiveFireZones;
                break;
            case DynamicTerrainPropType.Wall:
            case DynamicTerrainPropType.VineWall:
                list = wallProps;
                limit = maxActiveWalls;
                break;
            default:
                return true;
        }

        PruneList(list);
        if (list.Count < limit)
        {
            return true;
        }

        RemoveOldest(list);
        if (enableDebugLogs)
        {
            Debug.Log("[Terrain] Spawn skipped: active limit reached (removed oldest " + type + ").");
        }

        return true;
    }

    private GameObject CreateFromPrefabOrFallback(GameObject prefab, string fallbackName, Vector3 position, Quaternion rotation)
    {
        EnsureRuntimeParent();

        GameObject instance;
        if (prefab != null)
        {
            instance = Instantiate(prefab, position, rotation, runtimeParent);
        }
        else
        {
            instance = new GameObject(fallbackName);
            instance.transform.SetParent(runtimeParent, false);
            instance.transform.position = position;
            instance.transform.rotation = rotation;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(instance.transform, false);
            body.transform.localPosition = Vector3.up * 0.5f;
        }

        return instance;
    }

    private DynamicTerrainProp ConfigureProp(
        GameObject instance,
        DynamicTerrainPropType type,
        float duration,
        float warning,
        bool blockMovement,
        bool survivorFx,
        bool predatorFx,
        float dps,
        float slowMult)
    {
        DynamicTerrainProp prop = instance.GetComponent<DynamicTerrainProp>();
        if (prop == null)
        {
            prop = instance.AddComponent<DynamicTerrainProp>();
        }

        prop.Configure(
            type,
            Mathf.Max(0.5f, duration),
            warning,
            blockMovement,
            survivorFx,
            predatorFx,
            dps,
            slowMult,
            0f,
            true,
            enableDebugLogs);

        return prop;
    }

    private void RegisterProp(DynamicTerrainProp prop, List<DynamicTerrainProp> typedList)
    {
        if (prop == null)
        {
            return;
        }

        allProps.Add(prop);
        typedList?.Add(prop);
    }

    private void EnsureRuntimeParent()
    {
        if (runtimeParent != null)
        {
            return;
        }

        GameObject existing = GameObject.Find(RuntimeParentName);
        if (existing == null)
        {
            existing = new GameObject(RuntimeParentName);
            Transform world = GameObject.Find("World")?.transform;
            if (world != null)
            {
                existing.transform.SetParent(world, false);
            }
        }

        runtimeParent = existing.transform;
    }

    private static Vector3 SnapToGround(Vector3 worldPosition)
    {
        Vector3 rayStart = worldPosition + Vector3.up * 100f;
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 250f, ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            if (Instance != null && Instance.enableDebugLogs)
            {
                Debug.Log("[Terrain] Ground raycast failed at " + worldPosition.ToString("F1") + ", using raw position");
            }

            return worldPosition;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null || col.isTrigger)
            {
                continue;
            }

            string lower = col.gameObject.name.ToLowerInvariant();
            if (lower.Contains("water") || lower.Contains("hell") || lower.Contains("lava")
                || lower.Contains("portal") || lower.Contains("heaven"))
            {
                continue;
            }

            return hits[i].point;
        }

        return hits[0].point;
    }

    private bool IsTooCloseToAnyPlayer(Vector3 pos)
    {
        UnitHealth[] units = FindObjectsByType<UnitHealth>(FindObjectsSortMode.None);
        float minSqr = minDistanceFromPlayers * minDistanceFromPlayers;

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
            if (delta.sqrMagnitude < minSqr)
            {
                return true;
            }
        }

        return false;
    }

    private void PruneNullProps()
    {
        allProps.RemoveAll(p => p == null);
        PruneList(craterProps);
        PruneList(fireZoneProps);
        PruneList(wallProps);
    }

    private static void PruneList(List<DynamicTerrainProp> list)
    {
        if (list == null)
        {
            return;
        }

        list.RemoveAll(p => p == null);
    }

    private static void RemoveOldest(List<DynamicTerrainProp> list)
    {
        if (list == null || list.Count == 0)
        {
            return;
        }

        DynamicTerrainProp oldest = list[0];
        float oldestTime = float.MaxValue;

        for (int i = 0; i < list.Count; i++)
        {
            DynamicTerrainProp candidate = list[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.SpawnTimestamp < oldestTime)
            {
                oldestTime = candidate.SpawnTimestamp;
                oldest = candidate;
            }
        }

        if (oldest != null)
        {
            Destroy(oldest.gameObject);
        }
    }

    private void LogSpawn(string label, Vector3 position)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.Log("[Terrain] Spawned " + label + " at " + position.ToString("F1"));
    }
}
