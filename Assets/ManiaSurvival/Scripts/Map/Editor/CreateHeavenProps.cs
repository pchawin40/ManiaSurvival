using UnityEditor;
using UnityEngine;

public static class CreateHeavenProps
{
    [MenuItem("Mania Survival/Map/Create Heaven Props Around Selected Platform")]
    public static void CreateProps()
    {
        Transform platform = Selection.activeTransform;

        if (platform == null)
        {
            Debug.LogError("Select HeavenSafeZonePlatform first.");
            return;
        }

        Renderer platformRenderer = platform.GetComponent<Renderer>();

        if (platformRenderer == null)
        {
            Debug.LogError("Selected object needs a Renderer so I can read its bounds.");
            return;
        }

        Bounds b = platformRenderer.bounds;

        float centerX = b.center.x;
        float centerZ = b.center.z;

        float minZ = b.min.z;
        float maxZ = b.max.z;

        GameObject parent = new GameObject("HeavenProps_Auto");
        Undo.RegisterCreatedObjectUndo(parent, "Create Heaven Props");

        // Materials
        Material blackMat = CreateOrFindMaterial("Heaven_PortalBlack_Mat", new Color(0.01f, 0.005f, 0.03f));
        Material goldMat = CreateOrFindMaterial("Heaven_Gold_Mat", new Color(1f, 0.75f, 0.2f));
        Material whiteMat = CreateOrFindMaterial("Heaven_White_Mat", new Color(0.9f, 0.9f, 0.85f));
        Material blueMat = CreateOrFindMaterial("Heaven_WaterBlue_Mat", new Color(0.15f, 0.45f, 1f));
        Material itemMat = CreateOrFindMaterial("Heaven_ItemGlow_Mat", new Color(0.25f, 0.9f, 1f));
        Material auraMat = CreateOrFindMaterial("Heaven_Aura_Mat", new Color(0.35f, 0.8f, 1f, 0.35f));

        // Opening side is +Z based on your current setup
        float gateZ = maxZ + 0.45f;

        // 1. Black portal door in the opening
        GameObject portalDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(portalDoor, "Create Portal Door");
        portalDoor.name = "Heaven_BlackPortalDoor";
        portalDoor.transform.SetParent(parent.transform);
        portalDoor.transform.position = new Vector3(centerX, 1.8f, gateZ + 0.15f);
        portalDoor.transform.localScale = new Vector3(3.2f, 3.6f, 0.25f);
        portalDoor.GetComponent<Renderer>().sharedMaterial = blackMat;

        // 2. Portal glow frame
        GameObject portalFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(portalFrame, "Create Portal Frame");
        portalFrame.name = "Heaven_PortalGoldFrame";
        portalFrame.transform.SetParent(parent.transform);
        portalFrame.transform.position = new Vector3(centerX, 1.8f, gateZ);
        portalFrame.transform.localScale = new Vector3(3.8f, 4.1f, 0.15f);
        portalFrame.GetComponent<Renderer>().sharedMaterial = goldMat;

        // Put black door slightly in front of frame visually
        portalDoor.transform.position = new Vector3(centerX, 1.8f, gateZ + 0.25f);

        // 3. Angel NPC placeholder
        GameObject angel = new GameObject("Heaven_AngelNPC_StandStill");
        Undo.RegisterCreatedObjectUndo(angel, "Create Angel NPC");
        angel.transform.SetParent(parent.transform);
        angel.transform.position = new Vector3(centerX - 2.5f, 0f, centerZ - 2f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Angel_Body";
        body.transform.SetParent(angel.transform);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(0.7f, 1.1f, 0.7f);
        body.GetComponent<Renderer>().sharedMaterial = whiteMat;

        GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        halo.name = "Angel_Halo";
        halo.transform.SetParent(angel.transform);
        halo.transform.localPosition = new Vector3(0f, 2.35f, 0f);
        halo.transform.localScale = new Vector3(0.55f, 0.08f, 0.55f);
        halo.GetComponent<Renderer>().sharedMaterial = goldMat;

        GameObject wingL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wingL.name = "Angel_LeftWing";
        wingL.transform.SetParent(angel.transform);
        wingL.transform.localPosition = new Vector3(-0.55f, 1.25f, -0.15f);
        wingL.transform.localScale = new Vector3(0.15f, 0.9f, 0.55f);
        wingL.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
        wingL.GetComponent<Renderer>().sharedMaterial = whiteMat;

        GameObject wingR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wingR.name = "Angel_RightWing";
        wingR.transform.SetParent(angel.transform);
        wingR.transform.localPosition = new Vector3(0.55f, 1.25f, -0.15f);
        wingR.transform.localScale = new Vector3(0.15f, 0.9f, 0.55f);
        wingR.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
        wingR.GetComponent<Renderer>().sharedMaterial = whiteMat;

        // 4. Item pedestal where Speed Boots will go
        GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(pedestal, "Create Item Pedestal");
        pedestal.name = "Heaven_ItemPedestal_SpeedBootsSpot";
        pedestal.transform.SetParent(parent.transform);
        pedestal.transform.position = new Vector3(centerX + 2.5f, 0.25f, centerZ - 2f);
        pedestal.transform.localScale = new Vector3(0.9f, 0.25f, 0.9f);
        pedestal.GetComponent<Renderer>().sharedMaterial = goldMat;

        GameObject itemOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Undo.RegisterCreatedObjectUndo(itemOrb, "Create Item Orb");
        itemOrb.name = "Heaven_SpeedBootsPlaceholderOrb";
        itemOrb.transform.SetParent(parent.transform);
        itemOrb.transform.position = new Vector3(centerX + 2.5f, 1.05f, centerZ - 2f);
        itemOrb.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
        itemOrb.GetComponent<Renderer>().sharedMaterial = itemMat;

        // 5. Basic waterfall / holy fountain at back
        GameObject waterfall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(waterfall, "Create Waterfall");
        waterfall.name = "Heaven_BackWaterfall";
        waterfall.transform.SetParent(parent.transform);
        waterfall.transform.position = new Vector3(centerX, 1.6f, minZ + 0.65f);
        waterfall.transform.localScale = new Vector3(3.5f, 3.2f, 0.25f);
        waterfall.GetComponent<Renderer>().sharedMaterial = blueMat;

        GameObject pool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(pool, "Create Healing Pool");
        pool.name = "Heaven_HealingPool";
        pool.transform.SetParent(parent.transform);
        pool.transform.position = new Vector3(centerX, 0.08f, minZ + 2.1f);
        pool.transform.localScale = new Vector3(2.2f, 0.08f, 2.2f);
        pool.GetComponent<Renderer>().sharedMaterial = blueMat;

        // 6. Heaven blessing trigger zone placeholder
        GameObject blessingZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(blessingZone, "Create Blessing Zone");
        blessingZone.name = "Heaven_BlessingTrigger_HealManaItem";
        blessingZone.transform.SetParent(parent.transform);
        blessingZone.transform.position = new Vector3(centerX, 0.15f, centerZ);
        blessingZone.transform.localScale = new Vector3(b.size.x * 0.8f, 0.25f, b.size.z * 0.8f);

        Renderer blessingRenderer = blessingZone.GetComponent<Renderer>();
        blessingRenderer.sharedMaterial = auraMat;

        Collider blessingCollider = blessingZone.GetComponent<Collider>();
        blessingCollider.isTrigger = true;

        Selection.activeGameObject = parent;

        Debug.Log("Created Heaven props around: " + platform.name);
    }

    private static Material CreateOrFindMaterial(string name, Color color)
    {
        string folder = "Assets/ManiaSurvival/Materials";
        string path = folder + "/" + name + ".mat";

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;
        }

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;

        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();

        return mat;
    }
}