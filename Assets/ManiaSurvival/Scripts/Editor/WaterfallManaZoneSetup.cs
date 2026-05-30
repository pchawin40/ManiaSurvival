using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WaterfallManaZoneSetup
{
    private const string ZoneName = "WaterfallManaRegenZone";
    private const string SampleScenePath = "Assets/ManiaSurvival/Scenes/SampleScene.unity";

    [MenuItem("Mania Survival/Map/Setup Waterfall Mana Regen Zone")]
    public static void SetupWaterfallManaRegenZone()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != SampleScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        }

        GameObject existing = GameObject.Find(ZoneName);
        if (existing != null)
        {
            Debug.Log("[MapSetup] " + ZoneName + " already exists.");
            Selection.activeGameObject = existing;
            return;
        }

        Transform parent = FindParent();
        Vector3 position = ResolveZonePosition();

        GameObject zoneRoot = new GameObject(ZoneName);
        zoneRoot.transform.SetParent(parent, false);
        zoneRoot.transform.position = position;

        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        indicator.name = "ManaZoneIndicator";
        indicator.transform.SetParent(zoneRoot.transform, false);
        indicator.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        indicator.transform.localScale = new Vector3(8f, 0.05f, 8f);
        Object.DestroyImmediate(indicator.GetComponent<Collider>());

        MeshRenderer renderer = indicator.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.15f, 0.75f, 1f, 0.45f);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = 3000;
            renderer.sharedMaterial = material;
        }

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "ManaZoneWalkableFloor";
        floor.transform.SetParent(zoneRoot.transform, false);
        floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        floor.transform.localScale = new Vector3(8f, 0.1f, 8f);
        Collider floorCollider = floor.GetComponent<Collider>();
        if (floorCollider != null)
        {
            floorCollider.isTrigger = false;
        }

        BoxCollider trigger = zoneRoot.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 1f, 0f);
        trigger.size = new Vector3(8f, 2f, 8f);

        ManaRegenZone zone = zoneRoot.AddComponent<ManaRegenZone>();
        zone.bonusManaRegenPerSecond = 20f;
        zone.showDebugLogs = true;

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = zoneRoot;
        Debug.Log("[MapSetup] Created " + ZoneName + " at " + position);
    }

    private static Transform FindParent()
    {
        GameObject portals = GameObject.Find("Portals");
        if (portals != null)
        {
            Transform heavenProps = portals.transform.Find("HeavenProps_Auto");
            if (heavenProps != null)
            {
                return heavenProps;
            }

            return portals.transform;
        }

        GameObject world = GameObject.Find("World");
        return world != null ? world.transform : null;
    }

    private static Vector3 ResolveZonePosition()
    {
        GameObject waterfall = GameObject.Find("Heaven_BackWaterfall");
        if (waterfall != null)
        {
            Vector3 pos = waterfall.transform.position;
            pos.y = 0.05f;
            pos.z += 4f;
            return pos;
        }

        return new Vector3(0f, 0.05f, -105f);
    }
}
