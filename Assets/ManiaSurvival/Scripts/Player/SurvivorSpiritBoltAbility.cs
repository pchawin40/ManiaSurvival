using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Aim")]
    public Camera aimCamera;
    public LayerMask groundMask;
    public LineRenderer aimLine;
    public float aimLineLength = 8f;

    [Header("Mana")]
    public string abilityDisplayName = "Spirit Bolt";
    [Min(0)] public int manaCost = 4;

    [Header("UI")]
    public AbilityCooldownButton cooldownButton;

    private UnitHealth unitHealth;
    private SurvivorMana mana;
    private float nextCastTime;
    private bool isAiming;
    private Vector3 currentAimDirection = Vector3.forward;

    private void Awake()
    {
        unitHealth = GetComponent<UnitHealth>();
        mana = SurvivorMana.EnsureOn(gameObject, manaCost);

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        if (cooldownButton != null)
        {
            cooldownButton.SetAbilityInfo(abilityDisplayName, manaCost);
        }

        SetAimLineVisible(false);
    }

    private void Update()
    {
        if (!isAiming)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelAiming();
            return;
        }

        if (Mouse.current != null)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                CancelAiming();
                return;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                FireAimedSpiritBolt();
                return;
            }
        }

        UpdateAimPreview();
    }

    public void BeginAimSpiritBolt()
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

        if (manaCost > 0)
        {
            if (mana == null)
            {
                mana = SurvivorMana.EnsureOn(gameObject, manaCost);
            }

            if (mana == null || !mana.HasMana(manaCost))
            {
                Debug.Log("Spirit Bolt blocked: not enough mana");
                return;
            }
        }

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        if (aimCamera == null || groundMask.value == 0)
        {
            Debug.LogWarning("Spirit Bolt missing camera/ground warning");
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.Log("Spirit Bolt blocked");
            return;
        }

        isAiming = true;
        SetAimLineVisible(true);
        UpdateAimPreview();
        Debug.Log("aiming started");
    }

    public void CastSpiritBolt()
    {
        FireSpiritBolt(GetCastDirection(), false);
    }

    private void FireAimedSpiritBolt()
    {
        if (!isAiming)
        {
            return;
        }

        isAiming = false;
        SetAimLineVisible(false);

        if (currentAimDirection.sqrMagnitude <= 0.001f)
        {
            Debug.Log("aiming canceled");
            return;
        }

        if (FireSpiritBolt(currentAimDirection, true))
        {
            Debug.Log("fired aimed bolt");
        }
        else
        {
            Debug.Log("aiming canceled");
        }
    }

    private bool FireSpiritBolt(Vector3 castDirection, bool aimedFire)
    {
        if (cooldownButton != null && cooldownButton.IsCoolingDown)
        {
            Debug.Log("Spirit Bolt on cooldown");
            return false;
        }

        if (unitHealth == null)
        {
            unitHealth = GetComponent<UnitHealth>();
        }

        if (unitHealth == null || unitHealth.IsDead)
        {
            Debug.Log("Spirit Bolt blocked");
            return false;
        }

        if (Time.time < nextCastTime)
        {
            Debug.Log("Spirit Bolt on cooldown");
            return false;
        }

        if (projectilePrefab == null)
        {
            Debug.Log("Spirit Bolt blocked");
            return false;
        }

        if (castDirection.sqrMagnitude <= 0.001f)
        {
            Debug.Log("Spirit Bolt blocked");
            return false;
        }

        if (manaCost > 0)
        {
            if (mana == null)
            {
                mana = SurvivorMana.EnsureOn(gameObject, manaCost);
            }

            if (mana == null || !mana.SpendMana(manaCost))
            {
                Debug.Log("Spirit Bolt blocked: not enough mana");
                return false;
            }
        }

        Debug.Log("close range check");
        if (TryCloseRangeHit(castDirection))
        {
            nextCastTime = Time.time + cooldown;
            if (cooldownButton != null)
            {
                cooldownButton.StartCooldown(cooldown);
            }

            if (!aimedFire)
            {
                Debug.Log("Spirit Bolt cast");
            }

            return true;
        }

        Vector3 spawnPosition = GetSpawnPosition();
        GameObject projectileObject = Instantiate(projectilePrefab, spawnPosition, Quaternion.LookRotation(castDirection));
        SpiritBoltProjectile projectile = projectileObject.GetComponent<SpiritBoltProjectile>();

        if (projectile == null)
        {
            Debug.Log("Spirit Bolt blocked");
            Destroy(projectileObject);
            return false;
        }

        projectile.Initialize(transform, castDirection, projectileSpeed, damage, knockbackDistance, projectileLifetime);

        nextCastTime = Time.time + cooldown;
        if (cooldownButton != null)
        {
            cooldownButton.StartCooldown(cooldown);
        }

        Debug.Log("projectile fired normally");
        return true;
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

    private void UpdateAimPreview()
    {
        if (aimCamera == null || groundMask.value == 0)
        {
            Debug.LogWarning("Spirit Bolt missing camera/ground warning");
            CancelAiming();
            return;
        }

        Ray ray = aimCamera.ScreenPointToRay(Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundMask, QueryTriggerInteraction.Ignore))
        {
            currentAimDirection = Vector3.zero;
            SetAimLineVisible(false);
            return;
        }

        Vector3 startPosition = GetSpawnPosition();
        Vector3 targetPoint = hit.point;
        Vector3 flatDirection = targetPoint - transform.position;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude <= 0.001f)
        {
            currentAimDirection = Vector3.zero;
            SetAimLineVisible(false);
            return;
        }

        currentAimDirection = flatDirection.normalized;

        if (aimLine != null)
        {
            aimLine.useWorldSpace = true;
            aimLine.enabled = true;
            aimLine.positionCount = 2;
            aimLine.SetPosition(0, startPosition);
            float lineDistance = Mathf.Min(Mathf.Max(0.01f, aimLineLength), Vector3.Distance(startPosition, hit.point));
            aimLine.SetPosition(1, startPosition + currentAimDirection * lineDistance);
        }
    }

    private void CancelAiming()
    {
        if (!isAiming)
        {
            return;
        }

        isAiming = false;
        currentAimDirection = Vector3.forward;
        SetAimLineVisible(false);
        Debug.Log("aiming canceled");
    }

    private void SetAimLineVisible(bool visible)
    {
        if (aimLine != null)
        {
            aimLine.useWorldSpace = true;
            aimLine.enabled = visible;
            if (!visible)
            {
                aimLine.positionCount = 0;
            }
        }
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
