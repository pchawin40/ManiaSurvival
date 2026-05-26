using UnityEngine;

[RequireComponent(typeof(UnitHealth))]
public class SurvivorSpiritBoltAbility : MonoBehaviour
{
    [Header("Spirit Bolt")]
    public SpiritBoltProjectile projectilePrefab;
    public Transform firePoint;
    public Vector3 spawnOffset = new Vector3(0f, 1f, 1.25f);
    public float projectileSpeed = 10f;
    public int damage = 8;
    public float knockbackDistance = 2f;
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

        Vector3 spawnPosition = GetSpawnPosition();
        SpiritBoltProjectile projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.LookRotation(castDirection));
        projectile.Initialize(transform, projectileSpeed, damage, knockbackDistance, projectileLifetime);

        nextCastTime = Time.time + cooldown;
        if (cooldownButton != null)
        {
            cooldownButton.StartCooldown(cooldown);
        }

        Debug.Log("Spirit Bolt cast");
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
}
