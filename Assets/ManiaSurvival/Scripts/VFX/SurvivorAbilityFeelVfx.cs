using UnityEngine;

public static class SurvivorAbilityFeelVfx
{
    public static void SpawnBioticDartMuzzleFlash(Vector3 position, Vector3 forward, float lifetime = 0.18f)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "BioticDartMuzzleVFX";
        RemoveCollider(flash);
        flash.transform.position = position + forward * 0.35f + Vector3.up * 1.1f;
        flash.transform.localScale = Vector3.one * 0.35f;
        ApplyColor(flash, new Color(0.35f, 0.95f, 0.55f, 0.75f));
        Object.Destroy(flash, lifetime);
    }

    public static void SpawnBioticDartConeFlash(
        Vector3 origin,
        Vector3 forward,
        float range,
        float halfAngleDegrees,
        float lifetime = 0.22f)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();
        origin += Vector3.up * 0.9f;

        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = "BioticDartConeLineVFX";
        RemoveCollider(line);
        line.transform.position = origin + forward * (range * 0.45f);
        line.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        line.transform.localScale = new Vector3(0.18f, 0.08f, Mathf.Max(1f, range * 0.9f));
        ApplyColor(line, new Color(0.35f, 0.95f, 0.55f, 0.55f));
        Object.Destroy(line, lifetime);

        float spread = Mathf.Clamp(halfAngleDegrees, 10f, 45f);
        for (int i = -1; i <= 1; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, spread * i, 0f) * forward;
            GameObject streak = GameObject.CreatePrimitive(PrimitiveType.Cube);
            streak.name = "BioticDartConeStreakVFX";
            RemoveCollider(streak);
            streak.transform.position = origin + dir * (range * 0.35f);
            streak.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            streak.transform.localScale = new Vector3(0.12f, 0.06f, range * 0.55f);
            ApplyColor(streak, new Color(0.45f, 1f, 0.65f, 0.45f));
            Object.Destroy(streak, lifetime * 0.85f);
        }
    }

    public static void SpawnBroodlingDeathPop(Vector3 position, float lifetime = 0.28f)
    {
        GameObject pop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pop.name = "BroodlingDeathPopVFX";
        RemoveCollider(pop);
        pop.transform.position = position + Vector3.up * 0.45f;
        pop.transform.localScale = Vector3.one * 0.42f;
        ApplyColor(pop, new Color(0.45f, 0.95f, 0.25f, 0.75f));
        Object.Destroy(pop, lifetime);
    }

    public static void SpawnHealPulseRing(Vector3 center, float radius, float lifetime = 0.45f)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "HealPulseRingVFX";
        RemoveCollider(ring);
        ring.transform.position = center + Vector3.up * 0.05f;
        ring.transform.localScale = new Vector3(radius * 2f, 0.04f, radius * 2f);
        ApplyColor(ring, new Color(0.45f, 0.85f, 1f, 0.55f));
        Object.Destroy(ring, lifetime);

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "HealPulseCoreVFX";
        RemoveCollider(core);
        core.transform.position = center + Vector3.up * 0.8f;
        core.transform.localScale = Vector3.one * 0.55f;
        ApplyColor(core, new Color(0.55f, 1f, 0.75f, 0.65f));
        Object.Destroy(core, lifetime * 0.65f);
    }

    public static void SpawnBlinkFlash(Vector3 position, float lifetime = 0.22f)
    {
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "BlinkFlashVFX";
        RemoveCollider(flash);
        flash.transform.position = position + Vector3.up * 0.9f;
        flash.transform.localScale = Vector3.one * 0.9f;
        ApplyColor(flash, new Color(0.72f, 0.62f, 1f, 0.7f));
        Object.Destroy(flash, lifetime);
    }

    public static void SpawnSanctuaryWindupRing(Vector3 center, float radius, float lifetime = 0.35f)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "SanctuaryWindupVFX";
        RemoveCollider(ring);
        ring.transform.position = center + Vector3.up * 0.06f;
        ring.transform.localScale = new Vector3(radius * 1.4f, 0.05f, radius * 1.4f);
        ApplyColor(ring, new Color(1f, 0.82f, 0.35f, 0.5f));
        Object.Destroy(ring, lifetime);
    }

    private static void RemoveCollider(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();
        if (col != null)
        {
            Object.Destroy(col);
        }
    }

    private static void ApplyColor(GameObject obj, Color color)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Mode", 3f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;
        renderer.sharedMaterial = mat;
        Object.Destroy(mat, 2f);
    }
}
