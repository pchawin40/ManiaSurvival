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

    [Header("Mobile Button Input")]
    [Tooltip("How long the button must be held before entering aim mode.")]
    public float holdToAimThreshold = 0.18f;
    [Tooltip("Quick tap can auto-aim to nearest valid monster target.")]
    public bool quickTapUsesNearestTarget = true;
    [Tooltip("Search range for quick tap nearest-target aiming.")]
    public float quickTapTargetRange = 8f;
    [Tooltip("Layers used to find quick-tap targets.")]
    public LayerMask targetLayers = ~0;
    [Tooltip("Tags considered valid for quick tap auto-aim.")]
    public string[] targetTags = { "Monster", "Predator" };

    private UnitHealth unitHealth;
    private SurvivorMana mana;
    private float nextCastTime;
    private bool isAiming;
    private Vector3 currentAimDirection = Vector3.forward;
    private bool buttonHeld;
    private float buttonPressTime;
    private bool hasAimPointerScreenPosition;
    private Vector2 aimPointerScreenPosition;
    private bool hasButtonPressScreenPosition;
    private Vector2 buttonPressScreenPosition;

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
        if (buttonHeld && !isAiming && Time.time - buttonPressTime >= Mathf.Max(0.01f, holdToAimThreshold))
        {
            BeginAimSpiritBolt();
        }

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

            if (!buttonHeld && Mouse.current.leftButton.wasPressedThisFrame)
            {
                FireAimedSpiritBolt();
                return;
            }
        }

        UpdateAimPreview();
    }

    public void BeginAimSpiritBolt()
    {
        if (isAiming)
        {
            return;
        }

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

        if (aimCamera == null)
        {
            Debug.LogWarning("Spirit Bolt missing camera. Falling back to forward/pointer aim.");
        }

        if (projectilePrefab == null)
        {
            Debug.Log("Spirit Bolt blocked");
            return;
        }

        isAiming = true;
        currentAimDirection = Vector3.zero;
        SetAimLineVisible(true);
        UpdateAimPreview();
        Debug.Log("aiming started");
    }

    public void OnSpiritBoltButtonPressed(Vector2 screenPosition)
    {
        buttonHeld = true;
        buttonPressTime = Time.time;
        hasAimPointerScreenPosition = true;
        aimPointerScreenPosition = screenPosition;
        hasButtonPressScreenPosition = true;
        buttonPressScreenPosition = screenPosition;
    }

    public void OnSpiritBoltButtonDragged(Vector2 screenPosition)
    {
        hasAimPointerScreenPosition = true;
        aimPointerScreenPosition = screenPosition;

        if (isAiming)
        {
            UpdateAimPreview();
        }
    }

    public void OnSpiritBoltButtonReleased(Vector2 screenPosition, float heldDuration)
    {
        hasAimPointerScreenPosition = true;
        aimPointerScreenPosition = screenPosition;

        bool wasAiming = isAiming;
        buttonHeld = false;

        if (wasAiming || heldDuration >= Mathf.Max(0.01f, holdToAimThreshold))
        {
            FireAimedSpiritBoltFromButton();
            return;
        }

        Vector3 quickDirection = GetQuickFireDirection();
        if (FireSpiritBolt(quickDirection, false))
        {
            Debug.Log("quick tap Spirit Bolt fired");
            return;
        }

        Debug.Log("quick tap Spirit Bolt canceled");
    }

    // Backward compatibility wrappers for existing UI hookups.
    public void OnSpiritBoltButtonDown()
    {
        OnSpiritBoltButtonPressed(GetDefaultPointerPosition());
    }

    public void OnSpiritBoltButtonDrag(Vector2 screenPosition)
    {
        OnSpiritBoltButtonDragged(screenPosition);
    }

    public void OnSpiritBoltButtonUp()
    {
        OnSpiritBoltButtonReleased(GetDefaultPointerPosition(), 0f);
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

        EndAimingState();

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

    public void FireAimedSpiritBoltFromButton()
    {
        FireAimedSpiritBolt();
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

            if (!IsMonsterTarget(targetHealth))
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

    private bool IsMonsterTarget(UnitHealth targetHealth)
    {
        return targetHealth != null
            && (targetHealth.CompareTag("Monster") || targetHealth.CompareTag("Predator"));
    }

    private void UpdateAimPreview()
    {
        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        bool hasScreenPoint = TryGetAimScreenPoint(out Vector2 screenPoint);
        if (hasScreenPoint && TryGetRaycastAimDirection(screenPoint, out Vector3 raycastDirection, out float raycastLineDistance))
        {
            currentAimDirection = raycastDirection;
            DrawAimLine(currentAimDirection, raycastLineDistance);
            return;
        }

        Vector3 fallbackDirection = GetFallbackAimDirection();
        if (fallbackDirection.sqrMagnitude <= 0.001f)
        {
            currentAimDirection = Vector3.zero;
            SetAimLineVisible(false);
            return;
        }

        currentAimDirection = fallbackDirection.normalized;
        DrawAimLine(currentAimDirection, aimLineLength);
    }

    private void CancelAiming()
    {
        if (!isAiming)
        {
            return;
        }

        EndAimingState();
        currentAimDirection = Vector3.forward;
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

    private bool TryGetAimScreenPoint(out Vector2 screenPoint)
    {
        if (hasAimPointerScreenPosition)
        {
            screenPoint = aimPointerScreenPosition;
            return true;
        }

        if (Mouse.current != null)
        {
            screenPoint = Mouse.current.position.ReadValue();
            return true;
        }

        screenPoint = Vector2.zero;
        return false;
    }

    private bool TryGetRaycastAimDirection(Vector2 screenPoint, out Vector3 direction, out float lineDistance)
    {
        direction = Vector3.zero;
        lineDistance = Mathf.Max(0.01f, aimLineLength);

        if (aimCamera == null)
        {
            return false;
        }

        if (groundMask.value == 0)
        {
            return false;
        }

        Ray ray = aimCamera.ScreenPointToRay(screenPoint);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        Vector3 startPosition = GetSpawnPosition();
        Vector3 flatDirection = hit.point - transform.position;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        direction = flatDirection.normalized;
        lineDistance = Mathf.Min(Mathf.Max(0.01f, aimLineLength), Vector3.Distance(startPosition, hit.point));
        return true;
    }

    private Vector3 GetFallbackAimDirection()
    {
        Vector3 fallbackDirection = transform.forward;
        fallbackDirection.y = 0f;

        if (hasAimPointerScreenPosition && hasButtonPressScreenPosition)
        {
            Vector2 pointerDelta = aimPointerScreenPosition - buttonPressScreenPosition;
            if (pointerDelta.sqrMagnitude > 36f)
            {
                if (aimCamera != null)
                {
                    Vector3 camForward = aimCamera.transform.forward;
                    Vector3 camRight = aimCamera.transform.right;
                    camForward.y = 0f;
                    camRight.y = 0f;

                    if (camForward.sqrMagnitude > 0.001f && camRight.sqrMagnitude > 0.001f)
                    {
                        fallbackDirection =
                            camRight.normalized * pointerDelta.x +
                            camForward.normalized * pointerDelta.y;
                    }
                }
                else
                {
                    fallbackDirection = transform.right * pointerDelta.x + transform.forward * pointerDelta.y;
                }
            }
        }

        fallbackDirection.y = 0f;
        return fallbackDirection.sqrMagnitude > 0.001f ? fallbackDirection.normalized : Vector3.zero;
    }

    private void DrawAimLine(Vector3 direction, float distance)
    {
        if (aimLine == null)
        {
            return;
        }

        Vector3 startPosition = GetSpawnPosition();
        aimLine.useWorldSpace = true;
        aimLine.enabled = true;
        aimLine.positionCount = 2;
        aimLine.SetPosition(0, startPosition);
        aimLine.SetPosition(1, startPosition + direction.normalized * Mathf.Max(0.01f, distance));
    }

    private void EndAimingState()
    {
        isAiming = false;
        buttonHeld = false;
        hasAimPointerScreenPosition = false;
        hasButtonPressScreenPosition = false;
        SetAimLineVisible(false);
    }

    private Vector3 GetQuickFireDirection()
    {
        if (quickTapUsesNearestTarget)
        {
            UnitHealth target = FindNearestQuickTapTarget();
            if (target != null)
            {
                Vector3 toTarget = target.transform.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.001f)
                {
                    return toTarget.normalized;
                }
            }
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
    }

    private UnitHealth FindNearestQuickTapTarget()
    {
        float range = Mathf.Max(0.1f, quickTapTargetRange);
        Collider[] hits = Physics.OverlapSphere(transform.position, range, targetLayers, QueryTriggerInteraction.Ignore);

        UnitHealth nearest = null;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth target = hits[i].GetComponentInParent<UnitHealth>();
            if (target == null || target.IsDead || !HasAnyTargetTag(target))
            {
                continue;
            }

            if (target.transform == transform)
            {
                continue;
            }

            float distSqr = (target.transform.position - transform.position).sqrMagnitude;
            if (distSqr < nearestDistanceSqr)
            {
                nearest = target;
                nearestDistanceSqr = distSqr;
            }
        }

        return nearest;
    }

    private bool HasAnyTargetTag(UnitHealth target)
    {
        if (targetTags == null || targetTags.Length == 0)
        {
            return IsMonsterTarget(target);
        }

        for (int i = 0; i < targetTags.Length; i++)
        {
            string tag = targetTags[i];
            if (!string.IsNullOrEmpty(tag) && target.CompareTag(tag))
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 GetDefaultPointerPosition()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }

        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
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
