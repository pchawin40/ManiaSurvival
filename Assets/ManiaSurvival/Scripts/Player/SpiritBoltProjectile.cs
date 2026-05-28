using UnityEngine;

public class SpiritBoltProjectile : MonoBehaviour
{
    [Header("Projectile")]
    public float projectileSpeed = 10f;
    public float damage = 8f;
    public float knockbackDistance = 2f;
    public float lifetime = 3f;
    public float hitRadius = 0.25f;

    [Header("Target")]
    public string targetTag = "Monster";

    private Transform owner;
    private Vector3 moveDirection = Vector3.forward;
    private float lifeTimer;
    private bool hasHit;

    private void OnEnable()
    {
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
        Vector3 direction = moveDirection;
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

        transform.position = nextPosition;

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Debug.Log("Spirit Bolt expired");
            Destroy(gameObject);
        }
    }

    public void Initialize(Transform projectileOwner, Vector3 direction, float speed, float damageAmount, float knockbackAmount, float projectileLifetime)
    {
        owner = projectileOwner;
        moveDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
        projectileSpeed = speed;
        damage = damageAmount;
        knockbackDistance = knockbackAmount;
        lifetime = projectileLifetime;
        lifeTimer = Mathf.Max(0f, lifetime);
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
        if (targetHealth == null || targetHealth.IsDead || !IsValidTarget(targetHealth))
        {
            return false;
        }

        if (owner != null && targetHealth.transform == owner)
        {
            return false;
        }

        hasHit = true;
        targetHealth.TakeDamage(Mathf.RoundToInt(damage), gameObject);
        ApplyKnockback(targetHealth, direction);
        Debug.Log("Spirit Bolt hit Monster");
        Destroy(gameObject);
        return true;
    }

    private bool IsValidTarget(UnitHealth targetHealth)
    {
        if (targetHealth == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(targetTag))
        {
            return true;
        }

        if (targetHealth.CompareTag(targetTag))
        {
            return true;
        }

        // Keep legacy setup compatible: default targetTag is "Monster",
        // but many maps use "Predator" for the same unit.
        return targetTag == "Monster" && targetHealth.CompareTag("Predator");
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
