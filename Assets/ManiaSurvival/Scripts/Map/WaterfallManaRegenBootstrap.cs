using UnityEngine;

/// <summary>
/// Ensures a mana regen trigger exists at the Heaven waterfall when the scene loads.
/// SampleScene ships without the editor-only setup step, so this keeps the waterfall functional in play mode.
/// </summary>
public static class WaterfallManaRegenBootstrap
{
    private const string ZoneName = "WaterfallManaRegenZone";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureZone()
    {
        if (GameObject.Find(ZoneName) != null)
        {
            return;
        }

        Transform parent = ResolveParent();
        Vector3 position = ResolveZonePosition();

        GameObject zoneRoot = new GameObject(ZoneName);
        if (parent != null)
        {
            zoneRoot.transform.SetParent(parent, false);
        }

        zoneRoot.transform.position = position;

        BoxCollider trigger = zoneRoot.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 1f, 0f);
        trigger.size = new Vector3(10f, 2.5f, 10f);

        ManaRegenZone zone = zoneRoot.AddComponent<ManaRegenZone>();
        zone.survivorBonusManaRegenPerSecond = 2f;
        zone.predatorBonusManaRegenPerSecond = 10f;
        zone.bonusManaRegenPerSecond = 2f;
        zone.showDebugLogs = false;
    }

    private static Transform ResolveParent()
    {
        GameObject heavenProps = GameObject.Find("HeavenProps_Auto");
        if (heavenProps != null)
        {
            return heavenProps.transform;
        }

        GameObject portals = GameObject.Find("Portals");
        return portals != null ? portals.transform : null;
    }

    private static Vector3 ResolveZonePosition()
    {
        GameObject pool = GameObject.Find("Heaven_HealingPool");
        if (pool != null)
        {
            Vector3 pos = pool.transform.position;
            pos.y = 0.05f;
            return pos;
        }

        GameObject waterfall = GameObject.Find("Heaven_BackWaterfall");
        if (waterfall != null)
        {
            Vector3 pos = waterfall.transform.position;
            pos.y = 0.05f;
            pos.z += 2.5f;
            return pos;
        }

        return new Vector3(0f, 0.05f, -108f);
    }
}
