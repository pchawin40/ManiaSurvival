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
