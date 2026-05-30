using UnityEngine;

/// <summary>
/// Short-lived brood minion that chases survivors and deals contact damage.
/// </summary>
[DisallowMultipleComponent]
public class BroodlingMinion : MonoBehaviour
{
    public float moveSpeed = 5.5f;
    public float contactRadius = 1.1f;
    public int contactDamage = 5;
    public float lifetime = 12f;

    private PredatorClassManager owner;
    private LayerMask targetLayers;
    private string survivorTag;
    private float lifeRemaining;
    private float nextContactTime;

    public void Initialize(
        PredatorClassManager predatorOwner,
        LayerMask layers,
        string tag,
        float lifeSeconds,
        int damage,
        float speed)
    {
        owner = predatorOwner;
        targetLayers = layers;
        survivorTag = tag;
        lifetime = Mathf.Max(1f, lifeSeconds);
        lifeRemaining = lifetime;
        contactDamage = Mathf.Max(1, damage);
        moveSpeed = Mathf.Max(1f, speed);
        owner?.RegisterBroodling(this);
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
            nextContactTime = Time.time + 0.5f;
            GameObject source = owner != null ? owner.gameObject : gameObject;
            target.TakeDamage(contactDamage, source);
        }
    }

    private UnitHealth FindClosestSurvivor()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 12f, targetLayers, QueryTriggerInteraction.Ignore);
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

    public static GameObject SpawnRuntimeCapsule(Vector3 position, Color tint)
    {
        GameObject minion = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        minion.name = "Broodling_Runtime";
        minion.transform.position = position;
        minion.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);

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
