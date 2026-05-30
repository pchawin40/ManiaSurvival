using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Generates dense prototype terrain: forest clusters, rocks, ramps, platforms, parkour blocks.
/// Menu: Mania Survival/Map/Create Prototype Terrain Props
/// </summary>
public static class CreatePrototypeArenaProps
{
    public const string ParentObjectName = "PrototypeMapProps_Auto";

    private static readonly string[] LegacyParentNames =
    {
        "PrototypeArenaTerrain_Auto",
        ParentObjectName
    };

    // Tunable generation counts (editor constants).
    private const int TreeCount = 32;
    private const int RockCount = 16;
    private const int RampCount = 5;
    private const int PlatformCount = 4;
    private const int SteppingBlockCount = 10;
    private const int WallSegmentCount = 6;
    private const int JumpPadCount = 2;
    private const int BridgeCount = 2;

    private const int ObstacleLayer = 7;
    private const float CentralClearRadius = 6f;
    private const float ArenaInnerMargin = 2f;
    private const float MinPropSeparation = 1.75f;
    private const float SpawnAvoidRadius = 3.5f;
    private const float PortalAvoidRadius = 5f;
    private const float HellPitAvoidRadius = 7f;

    private static readonly List<Vector3> PlacedCenters = new List<Vector3>();
    private static readonly HashSet<int> GeneratedInstanceIds = new HashSet<int>();
    private static GenerationStats stats;
    private static float cachedFallbackGroundY = float.NaN;

    [MenuItem("Mania Survival/Map/Create Prototype Terrain Props")]
    public static void CreateTerrainProps()
    {
        GenerateLayout(includeParkour: true, includeBridges: true);
    }

    [MenuItem("Mania Survival/Map/Generate Dense Forest Test")]
    public static void GenerateDenseForestTest()
    {
        GenerateLayout(includeParkour: false, includeBridges: false, forestOnly: true);
    }

    [MenuItem("Mania Survival/Map/Clear Prototype Terrain Props")]
    public static void ClearExisting()
    {
        bool removedAny = false;
        for (int i = 0; i < LegacyParentNames.Length; i++)
        {
            GameObject existing = GameObject.Find(LegacyParentNames[i]);
            if (existing == null)
            {
                continue;
            }

            Undo.DestroyObjectImmediate(existing);
            removedAny = true;
            Debug.Log("[PrototypeMapProps] Cleared '" + LegacyParentNames[i] + "'.");
        }

        if (removedAny)
        {
            EditorSceneDirtyUtility.MarkActiveSceneDirtyIfEditing();
        }
    }

    private static void GenerateLayout(bool includeParkour, bool includeBridges, bool forestOnly = false)
    {
        ClearExisting();
        ResetGenerationState();

        Transform parent = CreateParent();
        stats.parent = parent;
        int targetTrees = forestOnly ? TreeCount + 8 : TreeCount;
        int targetRocks = forestOnly ? RockCount + 6 : RockCount;

        PlaceForestCluster("Forest_NW", -14f, -6f, 14f, 6f, targetTrees / 2);
        PlaceForestCluster("Forest_NE", 6f, -6f, 14f, 6f, targetTrees - (targetTrees / 2));
        PlaceRockCluster("Rocks_SW", -15f, -15f, -4f, -4f, targetRocks / 2);
        PlaceRockCluster("Rocks_SE", 4f, -14f, 14f, -4f, targetRocks - (targetRocks / 2));

        if (!forestOnly)
        {
            PlaceChokeWalls(WallSegmentCount);
            if (includeParkour)
            {
                PlacePlatformsAndRamps(PlatformCount, RampCount);
                PlaceSteppingPath(SteppingBlockCount);
                PlaceJumpPads(JumpPadCount);
            }

            if (includeBridges)
            {
                PlaceBridges(BridgeCount);
            }
        }

        EnsureDynamicTerrainBootstrap();
        Undo.RegisterCreatedObjectUndo(parent.gameObject, "Create Prototype Terrain Props");
        EditorSceneDirtyUtility.MarkActiveSceneDirtyIfEditing();
        LogGenerationSummary();
    }

    private static void ResetGenerationState()
    {
        PlacedCenters.Clear();
        GeneratedInstanceIds.Clear();
        cachedFallbackGroundY = float.NaN;
        stats = new GenerationStats();
    }

    private static Transform CreateParent()
    {
        GameObject parentObject = new GameObject(ParentObjectName);
        Transform root = FindMapRoot();
        if (root != null)
        {
            parentObject.transform.SetParent(root, false);
        }

        parentObject.transform.localPosition = Vector3.zero;
        RegisterGeneratedRoot(parentObject);
        return parentObject.transform;
    }

    private static Transform FindMapRoot()
    {
        GameObject mapRoot = GameObject.Find("MapRoot");
        if (mapRoot != null)
        {
            return mapRoot.transform;
        }

        GameObject world = GameObject.Find("World");
        return world != null ? world.transform : null;
    }

    private static void EnsureDynamicTerrainBootstrap()
    {
        if (Object.FindFirstObjectByType<DynamicTerrainSpawner>() == null)
        {
            GameObject managers = GameObject.Find("Managers");
            GameObject spawnerHost = new GameObject("DynamicTerrainSpawner");
            if (managers != null)
            {
                spawnerHost.transform.SetParent(managers.transform, false);
            }

            spawnerHost.AddComponent<DynamicTerrainSpawner>();
            spawnerHost.AddComponent<TerrainDebugHotkeys>();
        }

        if (GameObject.Find(DynamicTerrainSpawner.RuntimeParentName) == null)
        {
            GameObject runtime = new GameObject(DynamicTerrainSpawner.RuntimeParentName);
            Transform world = FindMapRoot();
            if (world != null)
            {
                runtime.transform.SetParent(world, false);
            }
        }
    }

    private static void PlaceForestCluster(string prefix, float minX, float maxX, float minZ, float maxZ, int count)
    {
        int placed = 0;
        int attempts = 0;
        int maxAttempts = count * 12;
        while (placed < count && attempts < maxAttempts)
        {
            attempts++;
            Vector3 xz = RandomXZ(minX, maxX, minZ, maxZ);
            float trunkHeight = Random.Range(2.5f, 4.2f);
            float trunkRadius = Random.Range(0.35f, 0.55f);
            float canopyScale = Random.Range(2.2f, 3.4f);
            if (!IsValidPropPosition(xz, trunkRadius + 0.6f))
            {
                continue;
            }

            CreateTree(prefix + "_" + placed, xz, Random.Range(0f, 360f), trunkHeight, trunkRadius, canopyScale);
            placed++;
            stats.trees++;
        }

        stats.skippedForest += Mathf.Max(0, count - placed);
    }

    private static void PlaceRockCluster(string prefix, float minX, float maxX, float minZ, float maxZ, int count)
    {
        int placed = 0;
        int attempts = 0;
        int maxAttempts = count * 12;
        while (placed < count && attempts < maxAttempts)
        {
            attempts++;
            Vector3 xz = RandomXZ(minX, maxX, minZ, maxZ);
            float scale = Random.Range(0.75f, 1.45f);
            bool stump = Random.value < 0.35f;
            if (!IsValidPropPosition(xz, scale * 0.65f))
            {
                continue;
            }

            if (stump)
            {
                CreateStump(prefix + "_Stump_" + placed, xz, scale);
            }
            else
            {
                CreateRock(prefix + "_Rock_" + placed, xz, Random.Range(0f, 360f), scale, blocking: true);
            }

            placed++;
            stats.rocks++;
        }

        stats.skippedRocks += Mathf.Max(0, count - placed);
    }

    private static void PlaceChokeWalls(int count)
    {
        Vector3[] wallSpots =
        {
            new Vector3(-4f, 0f, 8f),
            new Vector3(4f, 0f, 8f),
            new Vector3(-8f, 0f, 2f),
            new Vector3(8f, 0f, -2f),
            new Vector3(-3f, 0f, -10f),
            new Vector3(5f, 0f, -11f),
            new Vector3(-11f, 0f, -6f),
            new Vector3(11f, 0f, 6f)
        };

        for (int i = 0; i < count && i < wallSpots.Length; i++)
        {
            Vector3 xz = wallSpots[i];
            if (!IsValidPropPosition(xz, 1.6f))
            {
                stats.skippedWalls++;
                continue;
            }

            CreateWall("ChokeWall_" + i, xz, Random.Range(-25f, 25f), Random.Range(2.4f, 3.6f));
            stats.walls++;
        }
    }

    private static void PlacePlatformsAndRamps(int platformCount, int rampCount)
    {
        Vector3[] platformSpots =
        {
            new Vector3(-11f, 0f, 11f),
            new Vector3(11f, 0f, 11f),
            new Vector3(-12f, 0f, -2f),
            new Vector3(12f, 0f, 2f),
            new Vector3(0f, 0f, 13f)
        };

        Vector3[] rampSpots =
        {
            new Vector3(-9f, 0f, 9f),
            new Vector3(9f, 0f, 9f),
            new Vector3(-10f, 0f, -5f),
            new Vector3(10f, 0f, -3f),
            new Vector3(-2f, 0f, 11f),
            new Vector3(2f, 0f, 11f)
        };

        int rampsPlaced = 0;
        for (int i = 0; i < rampCount && i < rampSpots.Length; i++)
        {
            Vector3 xz = rampSpots[i];
            if (!IsValidPropPosition(xz, 2f))
            {
                stats.skippedRamps++;
                continue;
            }

            CreateRamp("Ramp_" + i, xz, Random.Range(0f, 360f));
            rampsPlaced++;
            stats.ramps++;
        }

        int platformsPlaced = 0;
        for (int i = 0; i < platformCount && i < platformSpots.Length; i++)
        {
            Vector3 xz = platformSpots[i];
            if (!IsValidPropPosition(xz, 2.5f))
            {
                stats.skippedPlatforms++;
                continue;
            }

            float deckHeight = Random.Range(1.1f, 1.8f);
            Vector3 deckSize = new Vector3(Random.Range(3.2f, 4.8f), 0.35f, Random.Range(3.2f, 4.8f));
            CreatePlatform("Platform_" + i, xz, deckHeight, deckSize);
            platformsPlaced++;
            stats.platforms++;
        }

        if (platformsPlaced > 0 && rampsPlaced == 0)
        {
            Debug.LogWarning("[PrototypeMapProps] Platforms placed but no ramps — some high ground may need Leap abilities to reach.");
        }
    }

    private static void PlaceSteppingPath(int count)
    {
        Vector3 start = new Vector3(5f, 0f, -12f);
        Vector3 step = new Vector3(1.6f, 0f, 1.4f);
        for (int i = 0; i < count; i++)
        {
            Vector3 xz = start + step * i;
            if (!IsValidPropPosition(xz, 0.8f))
            {
                stats.skippedSteps++;
                continue;
            }

            float blockHeight = 0.35f + (i * 0.12f);
            CreateSteppingBlock("Step_" + i, xz, blockHeight);
            stats.steppingBlocks++;
        }
    }

    private static void PlaceJumpPads(int count)
    {
        Vector3[] padSpots =
        {
            new Vector3(-7f, 0f, -1f),
            new Vector3(7f, 0f, -8f),
            new Vector3(-13f, 0f, 4f),
            new Vector3(13f, 0f, -5f)
        };

        for (int i = 0; i < count && i < padSpots.Length; i++)
        {
            Vector3 xz = padSpots[i];
            if (!IsValidPropPosition(xz, 1.4f))
            {
                stats.skippedJumpPads++;
                continue;
            }

            CreateJumpPad("JumpPad_" + i, xz, Random.Range(0f, 360f));
            stats.jumpPads++;
        }
    }

    private static void PlaceBridges(int count)
    {
        Vector3[] bridgeSpots =
        {
            new Vector3(-6f, 0f, -6f),
            new Vector3(6f, 0f, 5f)
        };

        for (int i = 0; i < count && i < bridgeSpots.Length; i++)
        {
            Vector3 xz = bridgeSpots[i];
            if (!IsValidPropPosition(xz, 2f))
            {
                stats.skippedBridges++;
                continue;
            }

            CreateBridge("Bridge_" + i, xz, i == 0 ? 0f : 90f);
            stats.bridges++;
        }
    }

    private static void CreateTree(
        string name,
        Vector3 xz,
        float yaw,
        float trunkHeight,
        float trunkRadius,
        float canopyScale)
    {
        float groundY = GetGroundY(xz);
        GameObject root = CreateRoot(name, xz, groundY, yaw, stats.parent);

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localPosition = new Vector3(0f, trunkHeight * 0.5f, 0f);
        trunk.transform.localScale = new Vector3(trunkRadius * 2f, trunkHeight * 0.5f, trunkRadius * 2f);
        ApplyColor(trunk, new Color(0.42f, 0.28f, 0.14f));
        ConfigureSolid(trunk);

        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = "Canopy";
        canopy.transform.SetParent(root.transform, false);
        canopy.transform.localPosition = new Vector3(0f, trunkHeight + canopyScale * 0.35f, 0f);
        canopy.transform.localScale = Vector3.one * canopyScale;
        ApplyColor(canopy, Random.value < 0.35f
            ? new Color(0.12f, 0.42f, 0.18f)
            : new Color(0.18f, 0.52f, 0.22f));
        Object.DestroyImmediate(canopy.GetComponent<Collider>());

        RegisterPlacement(xz);
    }

    private static void CreateRock(string name, Vector3 xz, float yaw, float scale, bool blocking)
    {
        float groundY = GetGroundY(xz);
        GameObject root = CreateRoot(name, xz, groundY, yaw, stats.parent);
        GameObject body = GameObject.CreatePrimitive(Random.value < 0.5f ? PrimitiveType.Sphere : PrimitiveType.Cube);
        body.name = "RockBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, scale * 0.45f, 0f);
        body.transform.localScale = new Vector3(scale * Random.Range(0.9f, 1.2f), scale * Random.Range(0.75f, 1.05f), scale * Random.Range(0.85f, 1.15f));
        body.transform.localRotation = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 180f), Random.Range(-10f, 10f));
        ApplyColor(body, new Color(0.46f, 0.48f, 0.52f));
        if (blocking)
        {
            ConfigureSolid(body);
        }
        else
        {
            Object.DestroyImmediate(body.GetComponent<Collider>());
        }

        RegisterPlacement(xz);
    }

    private static void CreateStump(string name, Vector3 xz, float scale)
    {
        float groundY = GetGroundY(xz);
        GameObject root = CreateRoot(name, xz, groundY, Random.Range(0f, 360f), stats.parent);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "StumpBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, scale * 0.22f, 0f);
        body.transform.localScale = new Vector3(scale, scale * 0.35f, scale);
        ApplyColor(body, new Color(0.45f, 0.3f, 0.16f));
        Object.DestroyImmediate(body.GetComponent<Collider>());
        RegisterPlacement(xz);
    }

    private static void CreateRamp(string name, Vector3 xz, float yaw)
    {
        float groundY = GetGroundY(xz);
        GameObject root = CreateRoot(name, xz, groundY, yaw, stats.parent);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "RampBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.45f, 1.35f);
        body.transform.localRotation = Quaternion.Euler(16f, 0f, 0f);
        body.transform.localScale = new Vector3(3.2f, 0.28f, 4.2f);
        ApplyColor(body, new Color(0.56f, 0.53f, 0.48f));
        ConfigureSolid(body);
        RegisterPlacement(xz);
    }

    private static void CreatePlatform(string name, Vector3 xz, float deckHeight, Vector3 deckSize)
    {
        float groundY = GetGroundY(xz);
        GameObject root = CreateRoot(name, xz, groundY, 0f, stats.parent);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "PlatformBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, deckHeight, 0f);
        body.transform.localScale = deckSize;
        ApplyColor(body, new Color(0.5f, 0.48f, 0.44f));
        ConfigureSolid(body);

        GameObject support = GameObject.CreatePrimitive(PrimitiveType.Cube);
        support.name = "Support";
        support.transform.SetParent(root.transform, false);
        support.transform.localPosition = new Vector3(0f, deckHeight * 0.45f, 0f);
        support.transform.localScale = new Vector3(deckSize.x * 0.82f, deckHeight * 0.9f, deckSize.z * 0.82f);
        ApplyColor(support, new Color(0.42f, 0.4f, 0.38f));
        ConfigureSolid(support);

        RegisterPlacement(xz);
    }

    private static void CreateWall(string name, Vector3 xz, float yaw, float length)
    {
        float groundY = GetGroundY(xz);
        GameObject root = CreateRoot(name, xz, groundY, yaw, stats.parent);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "WallBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        body.transform.localScale = new Vector3(length, 1.3f, 0.55f);
        ApplyColor(body, new Color(0.34f, 0.33f, 0.3f));
        ConfigureSolid(body);
        RegisterPlacement(xz);
    }

    private static void CreateSteppingBlock(string name, Vector3 xz, float height)
    {
        float groundY = GetGroundY(xz);
        GameObject root = CreateRoot(name, xz, groundY, 0f, stats.parent);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "StepBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
        body.transform.localScale = new Vector3(1.15f, height, 1.15f);
        ApplyColor(body, new Color(0.58f, 0.55f, 0.5f));
        ConfigureSolid(body);
        RegisterPlacement(xz);
    }

    private static void CreateJumpPad(string name, Vector3 xz, float yaw)
    {
        float groundY = GetGroundY(xz);
        GameObject root = CreateRoot(name, xz, groundY, yaw, stats.parent);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "PadBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        body.transform.localScale = new Vector3(2f, 0.08f, 2f);
        ApplyColor(body, new Color(0.2f, 0.85f, 1f, 0.85f));

        Collider col = body.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        JumpPad jumpPad = root.AddComponent<JumpPad>();
        jumpPad.affectsSurvivors = true;
        jumpPad.affectsPredator = true;
        RegisterPlacement(xz);
    }

    private static void CreateBridge(string name, Vector3 xz, float yaw)
    {
        float groundY = GetGroundY(xz);
        GameObject root = CreateRoot(name, xz, groundY, yaw, stats.parent);
        GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deck.name = "BridgeDeck";
        deck.transform.SetParent(root.transform, false);
        deck.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        deck.transform.localScale = new Vector3(1.2f, 0.18f, 4.5f);
        ApplyColor(deck, new Color(0.52f, 0.46f, 0.38f));
        ConfigureSolid(deck);
        RegisterPlacement(xz);
    }

    private static GameObject CreateRoot(string name, Vector3 xz, float groundY, float yaw, Transform parent)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(xz.x, groundY, xz.z);
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        RegisterGeneratedRoot(root);
        return root;
    }

    private static void RegisterGeneratedRoot(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        GeneratedInstanceIds.Add(root.GetInstanceID());
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                GeneratedInstanceIds.Add(colliders[i].GetInstanceID());
            }
        }
    }

    private static float GetGroundY(Vector3 xzPosition)
    {
        Vector3 origin = new Vector3(xzPosition.x, 120f, xzPosition.z);
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 250f, ~0, QueryTriggerInteraction.Ignore);
        if (hits != null && hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i].collider;
                if (col == null || col.isTrigger)
                {
                    continue;
                }

                if (IsGeneratedPropCollider(col))
                {
                    continue;
                }

                if (!IsLikelyWalkableSurface(col))
                {
                    continue;
                }

                TrackGroundY(hits[i].point.y);
                return hits[i].point.y;
            }
        }

        stats.failedGroundRaycasts++;
        float fallback = GetFallbackGroundY();
        Debug.LogWarning("[PrototypeMapProps] Ground raycast failed at "
            + new Vector3(xzPosition.x, 0f, xzPosition.z) + ", using fallback Y " + fallback.ToString("0.###"));
        TrackGroundY(fallback);
        return fallback;
    }

    private static bool IsLikelyWalkableSurface(Collider col)
    {
        string lower = col.gameObject.name.ToLowerInvariant();
        if (lower.Contains("blocker")
            || lower.Contains("tree")
            || lower.Contains("rock")
            || lower.Contains("trunk")
            || lower.Contains("canopy")
            || lower.Contains("wallbody")
            || lower.Contains("choke"))
        {
            return false;
        }

        if (lower.Contains("floor")
            || lower.Contains("platform")
            || lower.Contains("walkable")
            || lower.Contains("safezone")
            || lower.Contains("safe")
            || lower.Contains("ground")
            || col.gameObject.CompareTag("Ground"))
        {
            return true;
        }

        Transform walker = col.transform;
        while (walker != null)
        {
            if (walker.name == "MapRoot")
            {
                return col.bounds.size.y <= 1.5f;
            }

            walker = walker.parent;
        }

        return col.bounds.size.y <= 0.75f;
    }

    private static bool IsGeneratedPropCollider(Collider col)
    {
        if (col == null)
        {
            return false;
        }

        Transform current = col.transform;
        while (current != null)
        {
            if (GeneratedInstanceIds.Contains(current.gameObject.GetInstanceID()))
            {
                return true;
            }

            if (current.name == ParentObjectName || current.name == "PrototypeArenaTerrain_Auto")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static float GetFallbackGroundY()
    {
        if (!float.IsNaN(cachedFallbackGroundY))
        {
            return cachedFallbackGroundY;
        }

        float bestY = 1f;
        GameObject mapRoot = GameObject.Find("MapRoot");
        if (mapRoot != null)
        {
            Renderer[] renderers = mapRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                string lower = renderer.gameObject.name.ToLowerInvariant();
                if (!lower.Contains("floor") && !lower.Contains("safe") && !lower.Contains("ground"))
                {
                    continue;
                }

                bestY = Mathf.Max(bestY, renderer.bounds.max.y);
            }

            Collider[] colliders = mapRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col == null || col.isTrigger)
                {
                    continue;
                }

                string lower = col.gameObject.name.ToLowerInvariant();
                if (!lower.Contains("floor") && !lower.Contains("safe") && !lower.Contains("ground") && !lower.Contains("walkable"))
                {
                    continue;
                }

                bestY = Mathf.Max(bestY, col.bounds.max.y);
            }
        }

        cachedFallbackGroundY = bestY;
        return cachedFallbackGroundY;
    }

    private static bool IsValidPropPosition(Vector3 xz, float radius)
    {
        Vector3 flat = new Vector3(xz.x, 0f, xz.z);
        if (flat.magnitude <= CentralClearRadius)
        {
            stats.skippedCentralClear++;
            return false;
        }

        if (!IsInsideArena(flat))
        {
            stats.skippedOutsideArena++;
            return false;
        }

        if (IsInsideHellPit(flat))
        {
            stats.skippedHellPit++;
            return false;
        }

        if (IsInsideHeavenPortalZone(flat))
        {
            stats.skippedHeavenPortal++;
            return false;
        }

        if (IsInsideNoSpawnZone(flat))
        {
            stats.skippedNoSpawn++;
            return false;
        }

        if (IsNearPlayerSpawn(flat, SpawnAvoidRadius + radius))
        {
            stats.skippedNearSpawn++;
            return false;
        }

        for (int i = 0; i < PlacedCenters.Count; i++)
        {
            float minDistance = MinPropSeparation + radius;
            if (Vector3.Distance(flat, PlacedCenters[i]) < minDistance)
            {
                stats.skippedTooClose++;
                return false;
            }
        }

        float groundY = GetGroundY(xz);
        if (groundY < 0.25f)
        {
            stats.skippedBelowGround++;
            return false;
        }

        return true;
    }

    private static void RegisterPlacement(Vector3 xz)
    {
        PlacedCenters.Add(new Vector3(xz.x, 0f, xz.z));
    }

    private static void TrackGroundY(float y)
    {
        stats.minGroundY = Mathf.Min(stats.minGroundY, y);
        stats.maxGroundY = Mathf.Max(stats.maxGroundY, y);
    }

    private static Vector3 RandomXZ(float minX, float maxX, float minZ, float maxZ)
    {
        return new Vector3(Random.Range(minX, maxX), 0f, Random.Range(minZ, maxZ));
    }

    private static bool IsInsideArena(Vector3 pos)
    {
        ArenaBounds bounds = Object.FindFirstObjectByType<ArenaBounds>();
        float minX = bounds != null ? bounds.minX : -17f;
        float maxX = bounds != null ? bounds.maxX : 17f;
        float minZ = bounds != null ? bounds.minZ : -17f;
        float maxZ = bounds != null ? bounds.maxZ : 17f;

        return pos.x >= minX + ArenaInnerMargin
            && pos.x <= maxX - ArenaInnerMargin
            && pos.z >= minZ + ArenaInnerMargin
            && pos.z <= maxZ - ArenaInnerMargin;
    }

    private static bool IsInsideHellPit(Vector3 pos)
    {
        HellfirePitDamageZone[] pits = Object.FindObjectsByType<HellfirePitDamageZone>(FindObjectsSortMode.None);
        for (int i = 0; i < pits.Length; i++)
        {
            HellfirePitDamageZone pit = pits[i];
            if (pit == null)
            {
                continue;
            }

            Vector3 center = pit.transform.TransformPoint(pit.localDamageCenter);
            if (Vector3.Distance(new Vector3(pos.x, 0f, pos.z), new Vector3(center.x, 0f, center.z)) <= HellPitAvoidRadius)
            {
                return true;
            }
        }

        GameObject hellPit = GameObject.Find("HellPit");
        if (hellPit != null)
        {
            Vector3 center = hellPit.transform.position;
            if (Vector3.Distance(new Vector3(pos.x, 0f, pos.z), new Vector3(center.x, 0f, center.z)) <= HellPitAvoidRadius)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInsideHeavenPortalZone(Vector3 pos)
    {
        HeavenPortal[] portals = Object.FindObjectsByType<HeavenPortal>(FindObjectsSortMode.None);
        for (int i = 0; i < portals.Length; i++)
        {
            HeavenPortal portal = portals[i];
            if (portal == null)
            {
                continue;
            }

            Vector3 portalPos = portal.transform.position;
            if (Vector3.Distance(new Vector3(pos.x, 0f, pos.z), new Vector3(portalPos.x, 0f, portalPos.z)) <= PortalAvoidRadius)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInsideNoSpawnZone(Vector3 pos)
    {
        NoSpawnZone[] zones = Object.FindObjectsByType<NoSpawnZone>(FindObjectsSortMode.None);
        for (int i = 0; i < zones.Length; i++)
        {
            NoSpawnZone zone = zones[i];
            if (zone == null)
            {
                continue;
            }

            Collider col = zone.GetComponent<Collider>();
            if (col == null)
            {
                continue;
            }

            if (col.bounds.Contains(new Vector3(pos.x, col.bounds.center.y, pos.z)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNearPlayerSpawn(Vector3 pos, float radius)
    {
        SurvivorMovement[] survivors = Object.FindObjectsByType<SurvivorMovement>(FindObjectsSortMode.None);
        for (int i = 0; i < survivors.Length; i++)
        {
            SurvivorMovement movement = survivors[i];
            if (movement == null)
            {
                continue;
            }

            Vector3 spawnPos = movement.transform.position;
            if (Vector3.Distance(new Vector3(pos.x, 0f, pos.z), new Vector3(spawnPos.x, 0f, spawnPos.z)) <= radius)
            {
                return true;
            }
        }

        MonsterPlayerMovement[] monsters = Object.FindObjectsByType<MonsterPlayerMovement>(FindObjectsSortMode.None);
        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterPlayerMovement movement = monsters[i];
            if (movement == null)
            {
                continue;
            }

            Vector3 spawnPos = movement.transform.position;
            if (Vector3.Distance(new Vector3(pos.x, 0f, pos.z), new Vector3(spawnPos.x, 0f, spawnPos.z)) <= radius)
            {
                return true;
            }
        }

        return false;
    }

    private static void ConfigureSolid(GameObject body)
    {
        Collider col = body.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false;
        }

        body.layer = ObstacleLayer;
    }

    private static void ApplyColor(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = color;
        renderer.sharedMaterial = mat;
    }

    private static void LogGenerationSummary()
    {
        string groundRange = stats.minGroundY <= stats.maxGroundY
            ? stats.minGroundY.ToString("0.###") + " to " + stats.maxGroundY.ToString("0.###")
            : "n/a";

        Debug.Log("[PrototypeMapProps] Created:\n"
            + "- Trees: " + stats.trees + "\n"
            + "- Rocks: " + stats.rocks + "\n"
            + "- Ramps: " + stats.ramps + "\n"
            + "- Platforms: " + stats.platforms + "\n"
            + "- Stepping Blocks: " + stats.steppingBlocks + "\n"
            + "- Walls: " + stats.walls + "\n"
            + "- Jump Pads: " + stats.jumpPads + "\n"
            + "- Bridges: " + stats.bridges + "\n"
            + "- GroundY range: " + groundRange + "\n"
            + "- Parent: " + ParentObjectName + "\n"
            + "Skipped: centralClear=" + stats.skippedCentralClear
            + ", outsideArena=" + stats.skippedOutsideArena
            + ", hellPit=" + stats.skippedHellPit
            + ", heavenPortal=" + stats.skippedHeavenPortal
            + ", noSpawn=" + stats.skippedNoSpawn
            + ", nearSpawn=" + stats.skippedNearSpawn
            + ", tooClose=" + stats.skippedTooClose
            + ", belowGround=" + stats.skippedBelowGround
            + ", failedRaycasts=" + stats.failedGroundRaycasts
            + ", forestShortfall=" + stats.skippedForest
            + ", rockShortfall=" + stats.skippedRocks);
    }

    private class GenerationStats
    {
        public Transform parent;
        public int trees;
        public int rocks;
        public int ramps;
        public int platforms;
        public int steppingBlocks;
        public int walls;
        public int jumpPads;
        public int bridges;
        public int skippedCentralClear;
        public int skippedOutsideArena;
        public int skippedHellPit;
        public int skippedHeavenPortal;
        public int skippedNoSpawn;
        public int skippedNearSpawn;
        public int skippedTooClose;
        public int skippedBelowGround;
        public int failedGroundRaycasts;
        public int skippedForest;
        public int skippedRocks;
        public int skippedRamps;
        public int skippedPlatforms;
        public int skippedSteps;
        public int skippedWalls;
        public int skippedJumpPads;
        public int skippedBridges;
        public float minGroundY = float.MaxValue;
        public float maxGroundY = float.MinValue;
    }
}
