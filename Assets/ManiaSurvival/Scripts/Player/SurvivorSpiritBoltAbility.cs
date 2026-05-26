using UnityEngine;

[RequireComponent(typeof(UnitHealth))]
public class SurvivorSpiritBoltAbility : MonoBehaviour
{
    [Header("Spirit Bolt")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public Vector3 spawnOffset = new Vector3(0f, 1f, 0.8f);
    public float projectileSpeed = 10f;
    public float damage = 8f;
    public float knockbackDistance = 2f;
    public float closeRangeHitRadius = 1.2f;
    public float closeRangeForwardOffset = 1.0f;
    public float cooldown = 10f;
    public float projectileLifetime = 3f;

    [Header("UI")]
    public AbilityCooldownButton cooldownButton;

    private UnitHealth unitHealth;
    private float nextCastTime;

    private void Awake()
    {
        unitHealth = GetComponent<UnitHealth>();

        if (cooldownButton == null)
        {
            cooldownButton = FindFirstObjectByType<AbilityCooldownButton>();
        }
    }

    public void CastSpiritBolt()
    {
        if (cooldownButton != null && cooldownButton.IsCoolingDown)
        {
            Debug.Log("Spirit Bolt on cooldown");
            return;
        }

        if (unitHealth == null)
        {
            unitHealth = GetComponent<UnitHealth>();
        }

        if (unitHealth == null || unitHealth.IsDead)
        {
            Debug.Log("Spirit Bolt blocked");
            return;
        }

        if (Time.time < nextCastTime)
        {
            Debug.Log("Spirit Bolt on cooldown");
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.Log("Spirit Bolt blocked");
            return;
        }

        Vector3 castDirection = GetCastDirection();
        if (castDirection.sqrMagnitude <= 0.001f)
        {
            Debug.Log("Spirit Bolt blocked");
            return;
        }

        Debug.Log("close range check");
        if (TryCloseRangeHit(castDirection))
        {
            nextCastTime = Time.time + cooldown;
            if (cooldownButton != null)
            {
                cooldownButton.StartCooldown(cooldown);
            }

            Debug.Log("Spirit Bolt cast");
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition();
        GameObject projectileObject = Instantiate(projectilePrefab, spawnPosition, Quaternion.LookRotation(castDirection));
        SpiritBoltProjectile projectile = projectileObject.GetComponent<SpiritBoltProjectile>();

        if (projectile == null)
        {
            Debug.Log("Spirit Bolt blocked");
            Destroy(projectileObject);
            return;
        }

        projectile.Initialize(transform, castDirection, projectileSpeed, damage, knockbackDistance, projectileLifetime);

        nextCastTime = Time.time + cooldown;
        if (cooldownButton != null)
        {
            cooldownButton.StartCooldown(cooldown);
        }

        Debug.Log("projectile fired normally");
    }

    private Vector3 GetCastDirection()
    {
        Vector3 direction = transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return Vector3.forward;
        }

        return direction.normalized;
    }

    private Vector3 GetSpawnPosition()
    {
        if (firePoint != null)
        {
            return firePoint.position;
        }

        return transform.TransformPoint(spawnOffset);
    }

    private bool TryCloseRangeHit(Vector3 castDirection)
    {
        Vector3 origin = transform.position + castDirection * Mathf.Max(0f, closeRangeForwardOffset);
        float radius = Mathf.Max(0.01f, closeRangeHitRadius);
        Collider[] hits = Physics.OverlapSphere(origin, radius, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth targetHealth = hits[i].GetComponentInParent<UnitHealth>();
            if (targetHealth == null || targetHealth.IsDead)
            {
                continue;
            }

            if (!targetHealth.CompareTag("Monster"))
            {
                continue;
            }

            if (targetHealth.transform == transform)
            {
                continue;
            }

            targetHealth.TakeDamage(Mathf.RoundToInt(damage), gameObject);
            ApplyKnockback(targetHealth, castDirection);
            Debug.Log("close range hit");
            return true;
        }

        return false;
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
