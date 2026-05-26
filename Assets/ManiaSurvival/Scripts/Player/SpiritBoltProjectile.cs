using UnityEngine;

public class SpiritBoltProjectile : MonoBehaviour
{
    [Header("Projectile")]
    public float projectileSpeed = 10f;
    public int damage = 8;
    public float knockbackDistance = 2f;
    public float lifetime = 3f;
    public float hitRadius = 0.25f;

    [Header("Target")]
    public string targetTag = "Monster";

    private Transform owner;
    private Vector3 previousPosition;
    private float lifeTimer;
    private bool hasHit;

    private void OnEnable()
    {
        previousPosition = transform.position;
        lifeTimer = Mathf.Max(0f, lifetime);
        hasHit = false;
    }

    private void Update()
    {
        if (hasHit)
        {
            return;
        }

        float moveDistance = projectileSpeed * Time.deltaTime;
        Vector3 direction = transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = Vector3.forward;
        }
        else
        {
            direction = direction.normalized;
        }

        Vector3 currentPosition = transform.position;
        Vector3 nextPosition = currentPosition + direction * moveDistance;

        if (TryHitMonster(currentPosition, nextPosition, direction))
        {
            return;
        }

        previousPosition = currentPosition;
        transform.position = nextPosition;

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Debug.Log("Spirit Bolt miss/lifetime expired");
            Destroy(gameObject);
        }
    }

    public void Initialize(Transform projectileOwner, float speed, int damageAmount, float knockbackAmount, float projectileLifetime)
    {
        owner = projectileOwner;
        projectileSpeed = speed;
        damage = damageAmount;
        knockbackDistance = knockbackAmount;
        lifetime = projectileLifetime;
        lifeTimer = Mathf.Max(0f, lifetime);
        previousPosition = transform.position;
        hasHit = false;
    }

    private bool TryHitMonster(Vector3 startPosition, Vector3 endPosition, Vector3 direction)
    {
        Vector3 castDirection = endPosition - startPosition;
        float castDistance = castDirection.magnitude;

        if (castDistance <= 0.001f)
        {
            return false;
        }

        castDirection /= castDistance;

        if (!Physics.SphereCast(startPosition, hitRadius, castDirection, out RaycastHit hit, castDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        UnitHealth targetHealth = hit.collider.GetComponentInParent<UnitHealth>();
        if (targetHealth == null || targetHealth.IsDead || !targetHealth.CompareTag(targetTag))
        {
            return false;
        }

        if (owner != null && targetHealth.transform == owner)
        {
            return false;
        }

        hasHit = true;
        targetHealth.TakeDamage(damage, gameObject);
        ApplyKnockback(targetHealth, direction);
        Debug.Log("Spirit Bolt hit: " + targetHealth.name);
        Destroy(gameObject);
        return true;
    }

    private void ApplyKnockback(UnitHealth targetHealth, Vector3 direction)
    {
        Vector3 knockbackDirection = direction;
        knockbackDirection.y = 0f;

        if (knockbackDirection.sqrMagnitude <= 0.001f)
        {
            knockbackDirection = transform.forward;
            knockbackDirection.y = 0f;
        }

        if (knockbackDirection.sqrMagnitude <= 0.001f)
        {
            knockbackDirection = Vector3.forward;
        }

        knockbackDirection = knockbackDirection.normalized;

        CharacterController characterController = targetHealth.GetComponent<CharacterController>();
        if (characterController != null && characterController.enabled)
        {
            characterController.Move(knockbackDirection * knockbackDistance);
            return;
        }

        Rigidbody rigidbody = targetHealth.GetComponent<Rigidbody>();
        if (rigidbody != null && !rigidbody.isKinematic)
        {
            rigidbody.AddForce(knockbackDirection * knockbackDistance, ForceMode.Impulse);
            return;
        }

        targetHealth.transform.position += knockbackDirection * knockbackDistance;
    }
}
