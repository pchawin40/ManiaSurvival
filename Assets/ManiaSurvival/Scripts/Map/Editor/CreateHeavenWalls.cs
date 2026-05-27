using UnityEditor;
using UnityEngine;

public static class CreateHeavenWalls
{
    [MenuItem("Mania Survival/Map/Create Heaven Walls Around Selected Platform")]
    public static void CreateWalls()
    {
        Transform platform = Selection.activeTransform;

        if (platform == null)
        {
            Debug.LogError("Select HeavenSafeZonePlatform first.");
            return;
        }

        Renderer renderer = platform.GetComponent<Renderer>();

        if (renderer == null)
        {
            Debug.LogError("Selected object needs a Renderer so I can read its size.");
            return;
        }

        Bounds bounds = renderer.bounds;

        float minX = bounds.min.x;
        float maxX = bounds.max.x;
        float minZ = bounds.min.z;
        float maxZ = bounds.max.z;

        float centerX = bounds.center.x;
        float centerZ = bounds.center.z;

        float wallThickness = 0.6f;
        float wallHeight = 2.5f;
        float wallY = 1.25f;

        // Opening should face main arena.
        // For your screenshot, Heaven is around Z -100, so opening faces +Z.
        float openingWidth = Mathf.Min(bounds.size.x * 0.35f, 5f);

        GameObject parent = new GameObject("HeavenWalls_Auto");
        Undo.RegisterCreatedObjectUndo(parent, "Create Heaven Walls");

        // Back wall: negative Z side
        CreateWall(
            "HeavenWall_Back",
            new Vector3(centerX, wallY, minZ - wallThickness / 2f),
            new Vector3(bounds.size.x + wallThickness * 2f, wallHeight, wallThickness),
            parent.transform
        );

        // Left wall
        CreateWall(
            "HeavenWall_Left",
            new Vector3(minX - wallThickness / 2f, wallY, centerZ),
            new Vector3(wallThickness, wallHeight, bounds.size.z + wallThickness * 2f),
            parent.transform
        );

        // Right wall
        CreateWall(
            "HeavenWall_Right",
            new Vector3(maxX + wallThickness / 2f, wallY, centerZ),
            new Vector3(wallThickness, wallHeight, bounds.size.z + wallThickness * 2f),
            parent.transform
        );

        // Front wall split into two pieces, leaving opening in the middle
        float sideWallLength = (bounds.size.x - openingWidth) / 2f;

        CreateWall(
            "HeavenWall_Front_Left",
            new Vector3(minX + sideWallLength / 2f, wallY, maxZ + wallThickness / 2f),
            new Vector3(sideWallLength, wallHeight, wallThickness),
            parent.transform
        );

        CreateWall(
            "HeavenWall_Front_Right",
            new Vector3(maxX - sideWallLength / 2f, wallY, maxZ + wallThickness / 2f),
            new Vector3(sideWallLength, wallHeight, wallThickness),
            parent.transform
        );

        // Gate pillars at the opening
        float leftGateX = centerX - openingWidth / 2f;
        float rightGateX = centerX + openingWidth / 2f;
        float gateZ = maxZ + wallThickness / 2f;

        CreateWall(
            "HeavenGate_LeftPillar",
            new Vector3(leftGateX, wallY + 0.5f, gateZ),
            new Vector3(0.8f, wallHeight + 1f, 0.8f),
            parent.transform
        );

        CreateWall(
            "HeavenGate_RightPillar",
            new Vector3(rightGateX, wallY + 0.5f, gateZ),
            new Vector3(0.8f, wallHeight + 1f, 0.8f),
            parent.transform
        );

        Selection.activeGameObject = parent;

        Debug.Log("Created Heaven walls around: " + platform.name);
    }

    private static void CreateWall(string name, Vector3 position, Vector3 scale, Transform parent)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(wall, "Create Heaven Wall");

        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.transform.SetParent(parent);

        Renderer r = wall.GetComponent<Renderer>();
        r.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
    }
}