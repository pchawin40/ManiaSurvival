using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Generates static parkour terrain props (ramps, platforms, chokepoints, jump pads).
/// Menu: Mania Survival/Map/Create Prototype Terrain Props
/// </summary>
public static class CreatePrototypeArenaProps
{
    public const string ParentObjectName = "PrototypeArenaTerrain_Auto";

    private const int ObstacleLayer = 7;
    private const float CentralClearRadius = 5f;
    private const float ArenaInnerMargin = 2f;

    [MenuItem("Mania Survival/Map/Create Prototype Terrain Props")]
    public static void CreateTerrainProps()
    {
        ClearExisting();

        Transform parent = CreateParent();
        List<TerrainPlacement> placements = BuildLayout();
        int created = 0;
        int skipped = 0;

        for (int i = 0; i < placements.Count; i++)
        {
            TerrainPlacement placement = placements[i];
            if (!IsPlacementAllowed(placement.position))
            {
                skipped++;
                continue;
            }

            if (CreatePlacement(placement, parent))
            {
                created++;
            }
            else
            {
                skipped++;
            }
        }

        EnsureDynamicTerrainBootstrap();
        Undo.RegisterCreatedObjectUndo(parent.gameObject, "Create Prototype Terrain Props");
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[PrototypeArenaTerrain] Created " + created + " props (" + skipped + " skipped) under '" + ParentObjectName + "'.");
    }

    [MenuItem("Mania Survival/Map/Clear Prototype Terrain Props")]
    public static void ClearExisting()
    {
        GameObject existing = GameObject.Find(ParentObjectName);
        if (existing == null)
        {
            return;
        }

        Undo.DestroyObjectImmediate(existing);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
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

    private static List<TerrainPlacement> BuildLayout()
    {
        return new List<TerrainPlacement>
        {
            // Ramps to raised platforms.
            Ramp("Ramp_NW", -9f, 10f, 0f),
            Ramp("Ramp_NE", 9f, 10f, 180f),
            Ramp("Ramp_SW", -10f, -8f, 45f),
            Ramp("Ramp_SE", 10f, -7f, -135f),

            Platform("Platform_N", 0f, 12f, new Vector3(4.5f, 0.35f, 4.5f)),
            Platform("Platform_W", -12f, 2f, new Vector3(3.5f, 0.35f, 3.5f)),
            Platform("Platform_E", 12f, -1f, new Vector3(3.5f, 0.35f, 3.5f)),

            // Chokepoint walls / rocks.
            Wall("ChokeWall_N", -3f, 7f, 0f, 2.8f),
            Wall("ChokeWall_N2", 3f, 7f, 0f, 2.8f),
            Rock("ChokeRock_S", 2f, -11f, 1.1f),
            Rock("ChokeRock_W", -13f, -4f, 1.2f),

            // Parkour stepping stones toward east loop.
            Step("Step_1", 6f, 4f, 0.35f),
            Step("Step_2", 8f, 6f, 0.35f),
            Step("Step_3", 10f, 8f, 0.35f),

            // Jump pads for escape routes.
            JumpPad("JumpPad_W", -7f, 0f, 90f),
            JumpPad("JumpPad_E", 7f, -5f, -45f),
        };
    }

    private static TerrainPlacement Ramp(string name, float x, float z, float yaw)
    {
        return new TerrainPlacement
        {
            kind = TerrainKind.Ramp,
            objectName = name,
            position = new Vector3(x, 0f, z),
            yRotation = yaw
        };
    }

    private static TerrainPlacement Platform(string name, float x, float z, Vector3 size)
    {
        return new TerrainPlacement
        {
            kind = TerrainKind.Platform,
            objectName = name,
            position = new Vector3(x, 0f, z),
            size = size
        };
    }

    private static TerrainPlacement Wall(string name, float x, float z, float yaw, float length)
    {
        return new TerrainPlacement
        {
            kind = TerrainKind.Wall,
            objectName = name,
            position = new Vector3(x, 0f, z),
            yRotation = yaw,
            size = new Vector3(length, 1.1f, 0.5f)
        };
    }

    private static TerrainPlacement Rock(string name, float x, float z, float scale)
    {
        return new TerrainPlacement
        {
            kind = TerrainKind.Rock,
            objectName = name,
            position = new Vector3(x, 0f, z),
            size = Vector3.one * scale
        };
    }

    private static TerrainPlacement Step(string name, float x, float z, float height)
    {
        return new TerrainPlacement
        {
            kind = TerrainKind.Step,
            objectName = name,
            position = new Vector3(x, 0f, z),
            size = new Vector3(1.1f, height, 1.1f)
        };
    }

    private static TerrainPlacement JumpPad(string name, float x, float z, float yaw)
    {
        return new TerrainPlacement
        {
            kind = TerrainKind.JumpPad,
            objectName = name,
            position = new Vector3(x, 0f, z),
            yRotation = yaw
        };
    }

    private static bool CreatePlacement(TerrainPlacement placement, Transform parent)
    {
        switch (placement.kind)
        {
            case TerrainKind.Ramp:
                return CreateRamp(placement, parent);
            case TerrainKind.Platform:
                return CreatePlatform(placement, parent);
            case TerrainKind.Wall:
                return CreateWall(placement, parent);
            case TerrainKind.Rock:
                return CreateRock(placement, parent);
            case TerrainKind.Step:
                return CreateStep(placement, parent);
            case TerrainKind.JumpPad:
                return CreateJumpPad(placement, parent);
            default:
                return false;
        }
    }

    private static GameObject CreateRoot(TerrainPlacement placement, Transform parent)
    {
        GameObject root = new GameObject(placement.objectName);
        root.transform.SetParent(parent, false);
        root.transform.position = placement.position;
        root.transform.rotation = Quaternion.Euler(0f, placement.yRotation, 0f);
        return root;
    }

    private static bool CreateRamp(TerrainPlacement placement, Transform parent)
    {
        GameObject root = CreateRoot(placement, parent);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "RampBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.55f, 1.2f);
        body.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
        body.transform.localScale = new Vector3(2.6f, 0.3f, 3.4f);
        ApplyColor(body, new Color(0.56f, 0.53f, 0.48f));
        ConfigureSolid(body);
        return true;
    }

    private static bool CreatePlatform(TerrainPlacement placement, Transform parent)
    {
        GameObject root = CreateRoot(placement, parent);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "PlatformBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        body.transform.localScale = placement.size;
        ApplyColor(body, new Color(0.5f, 0.48f, 0.44f));
        ConfigureSolid(body);

        GameObject support = GameObject.CreatePrimitive(PrimitiveType.Cube);
        support.name = "Support";
        support.transform.SetParent(root.transform, false);
        support.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        support.transform.localScale = new Vector3(placement.size.x * 0.85f, 1.2f, placement.size.z * 0.85f);
        ApplyColor(support, new Color(0.42f, 0.4f, 0.38f));
        ConfigureSolid(support);
        return true;
    }

    private static bool CreateWall(TerrainPlacement placement, Transform parent)
    {
        GameObject root = CreateRoot(placement, parent);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "WallBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, placement.size.y * 0.5f, 0f);
        body.transform.localScale = placement.size;
        ApplyColor(body, new Color(0.54f, 0.52f, 0.48f));
        ConfigureSolid(body);
        return true;
    }

    private static bool CreateRock(TerrainPlacement placement, Transform parent)
    {
        GameObject root = CreateRoot(placement, parent);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "RockBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, placement.size.x * 0.45f, 0f);
        body.transform.localScale = placement.size;
        ApplyColor(body, new Color(0.46f, 0.48f, 0.52f));
        ConfigureSolid(body);
        return true;
    }

    private static bool CreateStep(TerrainPlacement placement, Transform parent)
    {
        GameObject root = CreateRoot(placement, parent);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "StepBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, placement.size.y * 0.5f, 0f);
        body.transform.localScale = placement.size;
        ApplyColor(body, new Color(0.58f, 0.55f, 0.5f));
        ConfigureSolid(body);
        return true;
    }

    private static bool CreateJumpPad(TerrainPlacement placement, Transform parent)
    {
        GameObject root = CreateRoot(placement, parent);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "PadBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        body.transform.localScale = new Vector3(1.8f, 0.08f, 1.8f);
        ApplyColor(body, new Color(0.2f, 0.85f, 1f, 0.75f));

        Collider col = body.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        JumpPad jumpPad = root.AddComponent<JumpPad>();
        jumpPad.affectsSurvivors = true;
        jumpPad.affectsPredator = true;
        return true;
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

    private static bool IsPlacementAllowed(Vector3 pos)
    {
        if (pos.magnitude <= CentralClearRadius)
        {
            return false;
        }

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

    private enum TerrainKind
    {
        Ramp,
        Platform,
        Wall,
        Rock,
        Step,
        JumpPad
    }

    private struct TerrainPlacement
    {
        public TerrainKind kind;
        public string objectName;
        public Vector3 position;
        public float yRotation;
        public Vector3 size;
    }
}
