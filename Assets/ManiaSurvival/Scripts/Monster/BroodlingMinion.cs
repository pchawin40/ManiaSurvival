using UnityEngine;

/// <summary>
/// Short-lived brood minion that chases survivors and deals contact damage.
/// </summary>
[DisallowMultipleComponent]
public class BroodlingMinion : MonoBehaviour
{
    public float moveSpeed = 4.2f;
    public float contactRadius = 1f;
    public int contactDamage = 2;
    public float lifetime = 9f;
    public float hatchDuration = 0.75f;
    public float contactInterval = 0.85f;

    private PredatorClassManager owner;
    private LayerMask targetLayers;
    private string survivorTag;
    private float lifeRemaining;
    private float nextContactTime;
    private float hatchRemaining;
    private bool isHatched;
    private Renderer cachedRenderer;
    private Color activeTint = new Color(0.4f, 0.9f, 0.25f, 1f);
    private Color hatchTint = new Color(0.25f, 0.45f, 0.18f, 1f);

    public void Initialize(
        PredatorClassManager predatorOwner,
        LayerMask layers,
        string tag,
        float lifeSeconds,
        int damage,
        float speed,
        float hatchSeconds,
        float contactCooldown)
    {
        owner = predatorOwner;
        targetLayers = layers;
        survivorTag = tag;
        lifetime = Mathf.Max(1f, lifeSeconds);
        lifeRemaining = lifetime;
        contactDamage = Mathf.Max(1, damage);
        moveSpeed = Mathf.Max(1f, speed);
        hatchDuration = Mathf.Max(0f, hatchSeconds);
        hatchRemaining = hatchDuration;
        contactInterval = Mathf.Clamp(contactCooldown, 0.35f, 2f);
        isHatched = hatchDuration <= 0f;
        cachedRenderer = GetComponentInChildren<Renderer>();
        ApplyHatchVisual();
    }

    private void OnDestroy()
    {
        owner?.UnregisterBroodling(this);
    }

    private void Update()
    {
        lifeRemaining -= Time.deltaTime;
        if (lifeRemaining <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (!isHatched)
        {
            hatchRemaining -= Time.deltaTime;
            if (hatchRemaining <= 0f)
            {
                isHatched = true;
                ApplyHatchVisual();
            }

            return;
        }

        UnitHealth target = FindClosestSurvivor();
        if (target == null)
        {
            return;
        }

        Vector3 dir = target.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            transform.position += dir.normalized * moveSpeed * Time.deltaTime;
        }

        if (dir.sqrMagnitude <= contactRadius * contactRadius && Time.time >= nextContactTime)
        {
            nextContactTime = Time.time + contactInterval;
            GameObject source = owner != null ? owner.gameObject : gameObject;
            target.TakeDamage(contactDamage, source);
        }
    }

    private void ApplyHatchVisual()
    {
        if (cachedRenderer == null)
        {
            return;
        }

        Material mat = cachedRenderer.material;
        mat.color = isHatched ? activeTint : hatchTint;
    }

    private UnitHealth FindClosestSurvivor()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 10f, targetLayers, QueryTriggerInteraction.Ignore);
        UnitHealth best = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth health = hits[i].GetComponentInParent<UnitHealth>();
            if (health == null || health.IsDead || !health.CompareTag(survivorTag))
            {
                continue;
            }

            if (owner != null && health.gameObject == owner.gameObject)
            {
                continue;
            }

            float sqr = (health.transform.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                best = health;
                bestSqr = sqr;
            }
        }

        return best;
    }

    public static GameObject SpawnRuntimeCapsule(Vector3 position, Color tint, float scale)
    {
        GameObject minion = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        minion.name = "Broodling_Runtime";
        minion.transform.position = position;
        float safeScale = Mathf.Clamp(scale, 0.45f, 0.85f);
        minion.transform.localScale = new Vector3(safeScale, safeScale, safeScale);

        Collider col = minion.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        Renderer renderer = minion.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = tint;
            renderer.material = mat;
        }

        return minion;
    }
}
