using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility that spawns low-poly primitive parkour props for prototype chase gameplay.
/// Menu: Mania Survival > Map > Generate Prototype Props / Clear Generated Props
/// </summary>
public class PrototypeMapPropsGenerator : EditorWindow
{
    public const string ParentObjectName = "PrototypeMapProps_Auto";

    private const int ObstacleLayer = 7; // Obstacle
    private const float CentralClearRadius = 5f;
    private const float ArenaInnerMargin = 2f;

    private bool ensureArenaBounds = true;
    private bool logPlacementSummary = true;

    [MenuItem("Mania Survival/Map/Generate Prototype Props")]
    public static void GenerateFromMenu()
    {
        PrototypeMapPropsGenerator window = GetWindow<PrototypeMapPropsGenerator>("Prototype Map Props");
        window.minSize = new Vector2(320f, 220f);
        window.GenerateProps();
    }

    [MenuItem("Mania Survival/Map/Clear Generated Props")]
    public static void ClearFromMenu()
    {
        ClearGeneratedProps();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Prototype Map Props", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Adds simple primitive trees, rocks, walls, crates, and logs under "
            + ParentObjectName + ".\n"
            + "Blocking props use solid colliders on the Obstacle layer. "
            + "Decor props are visual-only.\n"
            + "Props avoid Hellfire Pit, Heaven portal, and the central safe read zone.",
            MessageType.Info);

        EditorGUILayout.Space();
        ensureArenaBounds = EditorGUILayout.Toggle("Ensure ArenaBounds In Scene", ensureArenaBounds);
        logPlacementSummary = EditorGUILayout.Toggle("Log Placement Summary", logPlacementSummary);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Prototype Props", GUILayout.Height(32f)))
        {
            GenerateProps();
        }

        if (GUILayout.Button("Clear Generated Props", GUILayout.Height(28f)))
        {
            ClearGeneratedProps();
        }
    }

    private void GenerateProps()
    {
        ClearGeneratedProps();

        if (ensureArenaBounds)
        {
            EnsureArenaBoundsInScene();
        }

        Transform parent = CreateParentTransform();
        List<PropPlacement> placements = BuildPrototypeLayout();
        int blockingCount = 0;
        int visualCount = 0;
        int skippedCount = 0;

        for (int i = 0; i < placements.Count; i++)
        {
            PropPlacement placement = placements[i];
            if (!IsPlacementAllowed(placement))
            {
                skippedCount++;
                continue;
            }

            GameObject prop = CreateProp(placement, parent);
            if (prop == null)
            {
                skippedCount++;
                continue;
            }

            if (placement.blocksMovement)
            {
                blockingCount++;
            }
            else
            {
                visualCount++;
            }
        }

        Undo.RegisterCreatedObjectUndo(parent.gameObject, "Generate Prototype Props");
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        if (logPlacementSummary)
        {
            Debug.Log("[PrototypeMapProps] Generated " + (blockingCount + visualCount)
                + " props (" + blockingCount + " blocking, " + visualCount + " visual-only, "
                + skippedCount + " skipped) under '" + ParentObjectName + "'.");
        }
    }

    public static void ClearGeneratedProps()
    {
        GameObject existing = GameObject.Find(ParentObjectName);
        if (existing == null)
        {
            Debug.Log("[PrototypeMapProps] Nothing to clear — '" + ParentObjectName + "' was not found.");
            return;
        }

        Undo.DestroyObjectImmediate(existing);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[PrototypeMapProps] Cleared '" + ParentObjectName + "'.");
    }

    private static Transform CreateParentTransform()
    {
        GameObject parentObject = new GameObject(ParentObjectName);
        Transform worldRoot = FindWorldRoot();
        parentObject.transform.SetParent(worldRoot, false);
        parentObject.transform.localPosition = Vector3.zero;
        parentObject.transform.localRotation = Quaternion.identity;
        parentObject.transform.localScale = Vector3.one;
        return parentObject.transform;
    }

    private static Transform FindWorldRoot()
    {
        GameObject world = GameObject.Find("World");
        if (world != null)
        {
            return world.transform;
        }

        GameObject mapRoot = GameObject.Find("MapRoot");
        if (mapRoot != null)
        {
            return mapRoot.transform;
        }

        return null;
    }

    private static void EnsureArenaBoundsInScene()
    {
        ArenaBounds existing = Object.FindFirstObjectByType<ArenaBounds>();
        if (existing != null)
        {
            return;
        }

        GameObject boundsObject = new GameObject("ArenaBounds");
        ArenaBounds bounds = boundsObject.AddComponent<ArenaBounds>();
        bounds.minX = -17f;
        bounds.maxX = 17f;
        bounds.minZ = -17f;
        bounds.maxZ = 17f;
        bounds.fixBorderCollidersOnAwake = true;

        Transform worldRoot = FindWorldRoot();
        if (worldRoot != null)
        {
            boundsObject.transform.SetParent(worldRoot, false);
        }

        Undo.RegisterCreatedObjectUndo(boundsObject, "Create ArenaBounds");
        Debug.Log("[PrototypeMapProps] Added ArenaBounds (-17..17) to the active scene.");
    }

    private static List<PropPlacement> BuildPrototypeLayout()
    {
        return new List<PropPlacement>
        {
            // North kite loop — small walls with gaps for entry/exit.
            Prop(PrototypePropType.SmallWall, "Wall_NW", -8f, 12f, 35f, true, true, new Vector3(2.4f, 1.2f, 0.55f)),
            Prop(PrototypePropType.SmallWall, "Wall_NCenter", 2f, 13f, 0f, true, true, new Vector3(2.8f, 1.2f, 0.55f)),
            Prop(PrototypePropType.SmallWall, "Wall_NE", 10f, 12f, -25f, true, true, new Vector3(2.4f, 1.2f, 0.55f)),

            // East Hook line-of-sight blockers.
            Prop(PrototypePropType.SmallWall, "Wall_E_High", 14f, 5f, 90f, true, true, new Vector3(0.55f, 1.35f, 2.2f)),
            Prop(PrototypePropType.SmallWall, "Wall_E_Low", 14f, -3f, 90f, true, true, new Vector3(0.55f, 1.1f, 2f)),
            Prop(PrototypePropType.Log, "LogBarricade_SE", 12f, -9f, 15f, true, true, new Vector3(2.6f, 0.55f, 0.55f)),

            // Rock anchors for loops and corners.
            Prop(PrototypePropType.Rock, "Rock_NE", 11f, 9f, 20f, true, true, new Vector3(1.4f, 0.9f, 1.2f)),
            Prop(PrototypePropType.Rock, "Rock_E", 15f, 1f, -10f, true, true, new Vector3(1.5f, 1f, 1.3f)),
            Prop(PrototypePropType.Rock, "Rock_SE", 9f, -12f, 35f, true, false, new Vector3(1.2f, 0.75f, 1.1f)),
            Prop(PrototypePropType.Rock, "Rock_S", 1f, -14f, 0f, true, false, new Vector3(1.3f, 0.8f, 1.4f)),
            Prop(PrototypePropType.Rock, "Rock_SW", -12f, -11f, 15f, true, false, new Vector3(1.35f, 0.85f, 1.25f)),
            Prop(PrototypePropType.Rock, "Rock_W", -14f, -2f, -20f, true, true, new Vector3(1.5f, 1f, 1.2f)),

            // Crates and logs for micro-cover.
            Prop(PrototypePropType.Crate, "Crate_NE", 7f, 7f, 12f, true, false, new Vector3(1f, 1f, 1f)),
            Prop(PrototypePropType.Crate, "Crate_N", -5f, 8f, -18f, true, false, new Vector3(0.95f, 0.95f, 0.95f)),
            Prop(PrototypePropType.Log, "LogBarricade_W", -13f, 2f, 90f, true, true, new Vector3(2.4f, 0.55f, 0.55f)),

            // Trees — trunk blocks, canopy visual-only.
            Prop(PrototypePropType.Tree, "Tree_W", -10f, 5f, 0f, true, true, Vector3.one),
            Prop(PrototypePropType.Tree, "Tree_NW", -11f, 12f, 40f, true, true, Vector3.one),
            Prop(PrototypePropType.Tree, "Tree_N", 5f, 10f, -30f, true, true, Vector3.one),
            Prop(PrototypePropType.Tree, "Tree_NE", 13f, 8f, 60f, true, true, Vector3.one),
            Prop(PrototypePropType.Tree, "Tree_E", 13f, -6f, -45f, true, true, Vector3.one),
            Prop(PrototypePropType.Tree, "Tree_SW", -8f, -8f, 25f, true, false, Vector3.one),
            Prop(PrototypePropType.Tree, "Tree_S", -12f, -5f, 10f, true, false, Vector3.one),
            Prop(PrototypePropType.Tree, "Tree_SE", 8f, -5f, -15f, true, false, Vector3.one),

            // Visual-only decor — no movement block, adds map readability.
            Prop(PrototypePropType.Rock, "RockDecor_1", 4f, -10f, 0f, false, false, new Vector3(0.55f, 0.35f, 0.5f)),
            Prop(PrototypePropType.Rock, "RockDecor_2", -2f, 11f, 0f, false, false, new Vector3(0.45f, 0.3f, 0.4f)),
            Prop(PrototypePropType.Rock, "RockDecor_3", 6f, 14f, 0f, false, false, new Vector3(0.5f, 0.32f, 0.45f)),
            Prop(PrototypePropType.Rock, "RockDecor_4", -14f, 8f, 0f, false, false, new Vector3(0.48f, 0.3f, 0.42f)),
            Prop(PrototypePropType.Log, "Stump_1", 3f, 5f, 0f, false, false, new Vector3(0.9f, 0.25f, 0.9f)),
            Prop(PrototypePropType.Log, "Stump_2", -8f, -3f, 0f, false, false, new Vector3(0.85f, 0.22f, 0.85f)),
            Prop(PrototypePropType.Log, "LogDecor_1", 10f, 3f, 70f, false, false, new Vector3(1.6f, 0.22f, 0.35f)),
            Prop(PrototypePropType.Log, "LogDecor_2", -4f, -12f, -20f, false, false, new Vector3(1.4f, 0.2f, 0.32f)),
            Prop(PrototypePropType.Log, "LogDecor_3", 0f, -11f, 10f, false, false, new Vector3(1.5f, 0.2f, 0.34f)),
        };
    }

    private static PropPlacement Prop(
        PrototypePropType type,
        string name,
        float x,
        float z,
        float yRotation,
        bool blocksMovement,
        bool blocksLineOfSight,
        Vector3 scale)
    {
        return new PropPlacement
        {
            propType = type,
            objectName = name,
            position = new Vector3(x, 0f, z),
            yRotation = yRotation,
            blocksMovement = blocksMovement,
            blocksLineOfSight = blocksLineOfSight,
            scale = scale
        };
    }

    private bool IsPlacementAllowed(PropPlacement placement)
    {
        Vector3 pos = placement.position;

        if (pos.magnitude <= CentralClearRadius)
        {
            return false;
        }

        if (!IsInsideArena(pos))
        {
            return false;
        }

        if (IsInsideHellPit(pos))
        {
            return false;
        }

        if (IsInsideHeavenPortalZone(pos))
        {
            return false;
        }

        if (IsInsideNoSpawnZone(pos))
        {
            return false;
        }

        if (IsNearNamedHazard(pos, "Water", 3.5f))
        {
            return false;
        }

        return true;
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
            Vector3 size = Vector3.Scale(pit.localDamageSize, pit.transform.lossyScale);
            Vector3 half = size * 0.5f;
            half.x += 1.5f;
            half.z += 1.5f;

            if (Mathf.Abs(pos.x - center.x) <= half.x && Mathf.Abs(pos.z - center.z) <= half.z)
            {
                return true;
            }
        }

        GameObject hellPit = GameObject.Find("HellPit");
        if (hellPit != null && Vector3.Distance(new Vector3(pos.x, 0f, pos.z), new Vector3(hellPit.transform.position.x, 0f, hellPit.transform.position.z)) <= 7f)
        {
            return true;
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
            if (Vector3.Distance(new Vector3(pos.x, 0f, pos.z), new Vector3(portalPos.x, 0f, portalPos.z)) <= 4.5f)
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

    private static bool IsNearNamedHazard(Vector3 pos, string objectName, float radius)
    {
        GameObject hazard = GameObject.Find(objectName);
        if (hazard == null)
        {
            return false;
        }

        Vector3 hazardPos = hazard.transform.position;
        return Vector3.Distance(new Vector3(pos.x, 0f, pos.z), new Vector3(hazardPos.x, 0f, hazardPos.z)) <= radius;
    }

    private static GameObject CreateProp(PropPlacement placement, Transform parent)
    {
        switch (placement.propType)
        {
            case PrototypePropType.Tree:
                return CreateTree(placement, parent);
            case PrototypePropType.Rock:
                return CreateRock(placement, parent);
            case PrototypePropType.SmallWall:
                return CreateSmallWall(placement, parent);
            case PrototypePropType.Crate:
                return CreateCrate(placement, parent);
            case PrototypePropType.Log:
                return CreateLog(placement, parent);
            default:
                return null;
        }
    }

    private static GameObject CreateTree(PropPlacement placement, Transform parent)
    {
        GameObject root = CreateRoot(placement, parent);

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 1.25f, 0f);
        trunk.transform.localScale = new Vector3(0.75f, 1.25f, 0.75f);
        ApplyColor(trunk, new Color(0.42f, 0.28f, 0.14f));

        if (placement.blocksMovement)
        {
            ConfigureSolidCollider(trunk);
        }
        else
        {
            Object.DestroyImmediate(trunk.GetComponent<Collider>());
        }

        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = "Canopy";
        canopy.transform.SetParent(root.transform, false);
        canopy.transform.localPosition = new Vector3(0f, 2.65f, 0f);
        canopy.transform.localScale = new Vector3(2.2f, 1.6f, 2.2f);
        ApplyColor(canopy, new Color(0.18f, 0.52f, 0.22f));
        Object.DestroyImmediate(canopy.GetComponent<Collider>());

        AddPropMarker(root, placement);
        return root;
    }

    private static GameObject CreateRock(PropPlacement placement, Transform parent)
    {
        GameObject root = CreateRoot(placement, parent);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "RockBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, placement.scale.y * 0.5f, 0f);
        body.transform.localScale = placement.scale;
        body.transform.localRotation = Quaternion.Euler(0f, placement.yRotation * 0.35f, 0f);
        ApplyColor(body, placement.blocksMovement
            ? new Color(0.48f, 0.5f, 0.54f)
            : new Color(0.58f, 0.6f, 0.62f));

        if (placement.blocksMovement)
        {
            ConfigureSolidCollider(body);
        }
        else
        {
            Object.DestroyImmediate(body.GetComponent<Collider>());
        }

        AddPropMarker(root, placement);
        return root;
    }

    private static GameObject CreateSmallWall(PropPlacement placement, Transform parent)
    {
        GameObject root = CreateRoot(placement, parent);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "WallBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, placement.scale.y * 0.5f, 0f);
        body.transform.localScale = placement.scale;
        ApplyColor(body, new Color(0.55f, 0.53f, 0.5f));

        if (placement.blocksMovement)
        {
            ConfigureSolidCollider(body);
        }
        else
        {
            Object.DestroyImmediate(body.GetComponent<Collider>());
        }

        AddPropMarker(root, placement);
        return root;
    }

    private static GameObject CreateCrate(PropPlacement placement, Transform parent)
    {
        GameObject root = CreateRoot(placement, parent);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "CrateBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, placement.scale.y * 0.5f, 0f);
        body.transform.localScale = placement.scale;
        ApplyColor(body, new Color(0.62f, 0.42f, 0.24f));

        if (placement.blocksMovement)
        {
            ConfigureSolidCollider(body);
        }
        else
        {
            Object.DestroyImmediate(body.GetComponent<Collider>());
        }

        AddPropMarker(root, placement);
        return root;
    }

    private static GameObject CreateLog(PropPlacement placement, Transform parent)
    {
        GameObject root = CreateRoot(placement, parent);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "LogBody";
        body.transform.SetParent(root.transform, false);

        bool flatDecor = placement.objectName.Contains("Stump") || placement.objectName.Contains("Decor");
        if (flatDecor)
        {
            body.transform.localRotation = Quaternion.identity;
            body.transform.localPosition = new Vector3(0f, placement.scale.y * 0.5f, 0f);
            body.transform.localScale = placement.scale;
        }
        else
        {
            body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            body.transform.localPosition = new Vector3(0f, placement.scale.y * 0.5f, 0f);
            body.transform.localScale = new Vector3(placement.scale.y, placement.scale.x * 0.5f, placement.scale.y);
        }

        ApplyColor(body, placement.blocksMovement
            ? new Color(0.45f, 0.3f, 0.16f)
            : new Color(0.52f, 0.36f, 0.2f));

        if (placement.blocksMovement)
        {
            ConfigureSolidCollider(body);
        }
        else
        {
            Object.DestroyImmediate(body.GetComponent<Collider>());
        }

        AddPropMarker(root, placement);
        return root;
    }

    private static GameObject CreateRoot(PropPlacement placement, Transform parent)
    {
        GameObject root = new GameObject(placement.objectName);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(placement.position.x, 0f, placement.position.z);
        root.transform.rotation = Quaternion.Euler(0f, placement.yRotation, 0f);
        return root;
    }

    private static void ConfigureSolidCollider(GameObject body)
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
        MeshRenderer renderer = target.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            return;
        }

        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        material.color = color;
        renderer.sharedMaterial = material;
    }

    private static void AddPropMarker(GameObject root, PropPlacement placement)
    {
        PrototypeMapProp marker = root.AddComponent<PrototypeMapProp>();
        marker.propType = placement.propType;
        marker.blocksMovement = placement.blocksMovement;
        marker.blocksLineOfSight = placement.blocksLineOfSight;
    }

    private struct PropPlacement
    {
        public PrototypePropType propType;
        public string objectName;
        public Vector3 position;
        public float yRotation;
        public bool blocksMovement;
        public bool blocksLineOfSight;
        public Vector3 scale;
    }
}
