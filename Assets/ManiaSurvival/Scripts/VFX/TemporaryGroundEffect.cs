using UnityEngine;

[DisallowMultipleComponent]
public class TemporaryGroundEffect : MonoBehaviour
{
    [Header("Lifetime")]
    public float duration = 2.5f;
    public bool destroyAfterDuration = true;

    [Header("Scale")]
    public float startScale = 1f;
    public float endScale = 1f;

    [Header("Fade")]
    public bool fadeOut = true;

    [Header("Collision")]
    public bool blockMovement;

    private float spawnTime;
    private Renderer cachedRenderer;
    private Color startColor = Color.white;
    private Material runtimeMaterial;

    public static TemporaryGroundEffect Spawn(
        Vector3 worldPosition,
        Color tint,
        float effectDuration,
        float radius,
        GameObject prefab,
        bool verboseLog)
    {
        Vector3 groundedPosition = SnapToGround(worldPosition);
        TemporaryGroundEffect effect;

        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab, groundedPosition, Quaternion.identity);
            effect = instance.GetComponent<TemporaryGroundEffect>();
            if (effect == null)
            {
                effect = instance.AddComponent<TemporaryGroundEffect>();
            }
        }
        else
        {
            GameObject instance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            instance.name = "TemporaryGroundEffect";
            effect = instance.AddComponent<TemporaryGroundEffect>();
            effect.ApplyPlaceholderVisual(tint, radius);
        }

        effect.duration = Mathf.Max(0.1f, effectDuration);
        effect.startScale = radius;
        effect.endScale = radius;
        effect.spawnTime = Time.time;
        effect.transform.position = groundedPosition + Vector3.up * 0.03f;
        effect.ConfigureCollision();

        if (verboseLog)
        {
            Debug.Log("[GroundEffect] Spawned temporary effect at position "
                + groundedPosition + " duration " + effect.duration.ToString("0.00") + "s");
        }

        return effect;
    }

    public static TemporaryGroundEffect SpawnScorch(
        Vector3 worldPosition,
        Color tint,
        float effectDuration,
        float radius,
        bool verboseLog,
        string logTag = "GroundEffect")
    {
        TemporaryGroundEffect effect = Spawn(
            worldPosition,
            tint,
            effectDuration,
            radius,
            null,
            false);

        if (verboseLog)
        {
            Debug.Log("[" + logTag + "] Crater spawned at "
                + worldPosition + ", duration " + effectDuration.ToString("0.0") + "s");
        }

        return effect;
    }

    private static Vector3 SnapToGround(Vector3 worldPosition)
    {
        Vector3 rayStart = worldPosition + Vector3.up * 8f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        return worldPosition;
    }

    private void ApplyPlaceholderVisual(Color tint, float radius)
    {
        transform.localScale = new Vector3(radius * 2f, 0.04f, radius * 2f);
        cachedRenderer = GetComponent<Renderer>();
        if (cachedRenderer == null)
        {
            return;
        }

        runtimeMaterial = new Material(Shader.Find("Standard"));
        startColor = tint;
        runtimeMaterial.color = tint;
        cachedRenderer.material = runtimeMaterial;
    }

    private void ConfigureCollision()
    {
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            return;
        }

        collider.isTrigger = !blockMovement;
        collider.enabled = blockMovement;
    }

    private void Update()
    {
        float elapsed = Time.time - spawnTime;
        float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
        float scale = Mathf.Lerp(startScale, endScale, t);
        transform.localScale = new Vector3(scale * 2f, 0.04f, scale * 2f);

        if (fadeOut && runtimeMaterial != null)
        {
            Color color = startColor;
            color.a = Mathf.Lerp(startColor.a, 0f, t);
            runtimeMaterial.color = color;
        }

        if (destroyAfterDuration && elapsed >= duration)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}

public static class PredatorAbilityFeelVfx
{
    public static void SpawnSprayPellets(Vector3 origin, Vector3 forward, float range, float halfAngle, int pelletCount, float lifetime)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();
        pelletCount = Mathf.Max(3, pelletCount);

        for (int i = 0; i < pelletCount; i++)
        {
            float yaw = Random.Range(-halfAngle, halfAngle);
            Vector3 pelletDir = Quaternion.Euler(0f, yaw, 0f) * forward;
            float pelletRange = Random.Range(range * 0.45f, range * 0.95f);

            GameObject streak = GameObject.CreatePrimitive(PrimitiveType.Cube);
            streak.name = "SprayPelletVFX";
            RemoveCollider(streak);
            streak.transform.position = origin + pelletDir * (pelletRange * 0.5f) + Vector3.up * Random.Range(0.25f, 0.65f);
            streak.transform.rotation = Quaternion.LookRotation(pelletDir);
            streak.transform.localScale = new Vector3(0.08f, 0.08f, pelletRange);
            ApplyColor(streak, new Color(1f, 0.55f, 0.12f, 0.85f));
            Object.Destroy(streak, lifetime);
        }
    }

    public static GameObject SpawnHookChainLine(Vector3 start, Vector3 end, Color color, float width)
    {
        GameObject host = new GameObject("HookChainVFX");
        LineRenderer line = host.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = width;
        line.endWidth = width * 0.55f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = color;
        line.numCapVertices = 4;
        return host;
    }

    public static void SpawnBarrageWarningStrip(Vector3 origin, Vector3 forward, float range, float halfAngle, float lifetime)
    {
        forward.y = 0f;
        forward.Normalize();

        GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.name = "BarrageWarningStrip";
        RemoveCollider(strip);
        float width = Mathf.Tan(halfAngle * Mathf.Deg2Rad) * range * 2f;
        strip.transform.position = origin + forward * (range * 0.5f) + Vector3.up * 0.04f;
        strip.transform.rotation = Quaternion.LookRotation(forward);
        strip.transform.localScale = new Vector3(Mathf.Max(2f, width), 0.03f, range);
        ApplyColor(strip, new Color(1f, 0.15f, 0.1f, 0.55f));
        Object.Destroy(strip, lifetime);
    }

    public static void SpawnBombExplosion(Vector3 position, float scale, float lifetime)
    {
        GameObject boom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        boom.name = "BarrageBombVFX";
        RemoveCollider(boom);
        boom.transform.position = position + Vector3.up * 0.55f;
        boom.transform.localScale = Vector3.one * scale;
        ApplyColor(boom, new Color(1f, 0.45f, 0.08f, 0.82f));
        Object.Destroy(boom, lifetime);

        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "BarrageBombFlashVFX";
        RemoveCollider(flash);
        flash.transform.position = position + Vector3.up * 0.35f;
        flash.transform.localScale = Vector3.one * (scale * 1.35f);
        ApplyColor(flash, new Color(1f, 0.85f, 0.35f, 0.45f));
        Object.Destroy(flash, lifetime * 0.55f);
    }

    public static void SpawnTonicChannelRing(Transform parent, float radius, float duration)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "TonicChannelRingVFX";
        RemoveCollider(ring);
        ring.transform.SetParent(parent, false);
        ring.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        ring.transform.localScale = new Vector3(radius * 2.2f, 0.03f, radius * 2.2f);
        ApplyColor(ring, new Color(0.2f, 0.95f, 0.35f, 0.42f));
        Object.Destroy(ring, duration);
    }

    public static void DamageDestructiblePropsInRadius(Vector3 center, float radius, int damage, GameObject source)
    {
        if (damage <= 0 || radius <= 0f)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            DestructiblePropHealth prop = hits[i].GetComponentInParent<DestructiblePropHealth>();
            if (prop == null || !prop.IsAlive)
            {
                continue;
            }

            prop.TakeDamage(damage, source);
        }
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
        renderer.material = mat;
    }
}
