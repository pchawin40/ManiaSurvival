using UnityEngine;

[RequireComponent(typeof(UnitHealth))]
public class SurvivorBlinkAbility : MonoBehaviour
{
    [Header("Blink")]
    public float blinkDistance = 6f;
    public float blinkCooldown = 9f;
    public float collisionCheckRadius = 0.4f;
    [Tooltip("When true, AbilityController owns mana/cooldown. This component only moves.")]
    public bool useAbilityControllerAuthority = true;

    [Header("Legacy Mana (ignored when AbilityController is active)")]
    public string abilityDisplayName = "Blink";
    [Min(0)] public int manaCost = 6;

    [Header("UI")]
    public AbilityCooldownButton cooldownButton;

    [Header("Collision")]
    [Tooltip("Legacy blocker mask used for backward compatibility when Path/Landing masks are not assigned.")]
    public LayerMask obstacleLayers = ~0;
    [Tooltip("Solid blockers checked along blink travel path (walls, trees, obstacles, boundaries). " +
             "Leave empty to use obstacleLayers.")]
    public LayerMask pathBlockerLayers;
    [Tooltip("Solid blockers checked at landing position (walls, trees, obstacles, boundaries). " +
             "Leave empty to use obstacleLayers.")]
    public LayerMask landingBlockerLayers;
    [Tooltip("Hazard layers like HellPit/Water. Blink destination is rejected if inside these volumes.")]
    public LayerMask hazardLayers;

    [Header("VFX")]
    public GameObject blinkVfxPrefab;

    private UnitHealth unitHealth;
    private CharacterController characterController;
    private SurvivorMovement survivorMovement;
    private SurvivorMana legacyMana;
    private float nextBlinkTime;

    private void Awake()
    {
        unitHealth = GetComponent<UnitHealth>();
        characterController = GetComponent<CharacterController>();
        survivorMovement = GetComponent<SurvivorMovement>();
        if (!useAbilityControllerAuthority)
        {
            legacyMana = SurvivorMana.EnsureOn(gameObject, manaCost);
            if (cooldownButton != null)
            {
                cooldownButton.SetAbilityInfo(abilityDisplayName, manaCost);
            }
        }
    }

    public bool TryBlinkStep()
    {
        if (unitHealth == null)
        {
            unitHealth = GetComponent<UnitHealth>();
        }

        if (unitHealth == null || unitHealth.IsDead)
        {
            Debug.Log("[AbilityBlock] Blink blocked: unit dead");
            return false;
        }

        if (!useAbilityControllerAuthority)
        {
            if (cooldownButton != null && cooldownButton.IsCoolingDown)
            {
                return false;
            }

            if (Time.time < nextBlinkTime)
            {
                return false;
            }
        }

        Vector3 blinkDirection = GetBlinkDirection();
        if (blinkDirection.sqrMagnitude <= 0.001f)
        {
            Debug.Log("[AbilityBlock] Blink blocked: no direction");
            return false;
        }

        if (!TryGetBlinkDestination(blinkDirection, out Vector3 destination, out string failReason))
        {
            if (string.IsNullOrEmpty(failReason))
            {
                failReason = "blocked path";
            }

            Debug.Log("[AbilityBlock] Blink blocked: " + failReason);
            return false;
        }

        if (!useAbilityControllerAuthority && manaCost > 0)
        {
            if (legacyMana == null)
            {
                legacyMana = SurvivorMana.EnsureOn(gameObject, manaCost);
            }

            if (legacyMana == null || !legacyMana.SpendMana(manaCost))
            {
                return false;
            }
        }

        Vector3 origin = transform.position;

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        transform.position = destination;

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        float traveled = Vector3.Distance(origin, destination);

        if (blinkVfxPrefab != null)
        {
            Instantiate(blinkVfxPrefab, destination, Quaternion.identity);
        }

        if (!useAbilityControllerAuthority)
        {
            nextBlinkTime = Time.time + blinkCooldown;
            if (cooldownButton != null)
            {
                cooldownButton.StartCooldown(blinkCooldown);
            }
        }

        Debug.Log("[SurvivorAbility] Blink cast direction " + blinkDirection + " distance "
            + traveled.ToString("0.0"));
        return true;
    }

    public void Blinkstep()
    {
        TryBlinkStep();
    }

    private Vector3 GetBlinkDirection()
    {
        if (survivorMovement != null)
        {
            return survivorMovement.GetAimDirection();
        }

        Vector3 direction = transform.forward;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
    }

    private bool TryGetBlinkDestination(Vector3 blinkDirection, out Vector3 destination, out string failReason)
    {
        failReason = string.Empty;
        Vector3 origin = transform.position;
        float maxDistance = Mathf.Max(0f, blinkDistance);
        float radius = Mathf.Max(0.01f, collisionCheckRadius);
        LayerMask pathMask = GetPathBlockerMask();
        if (TryFindPathBlocker(origin, blinkDirection, maxDistance, radius, pathMask, out RaycastHit pathHit))
        {
            float safeDistance = Mathf.Max(0f, pathHit.distance - radius);
            if (safeDistance <= 0.05f)
            {
                destination = origin;
                failReason = "Blink blocked by wall/path blocker: " + pathHit.collider.name;
                return false;
            }

            destination = origin + blinkDirection * safeDistance;
            if (!IsDestinationSafe(destination, radius, out failReason))
            {
                return false;
            }

            return true;
        }

        destination = origin + blinkDirection * maxDistance;
        return IsDestinationSafe(destination, radius, out failReason);
    }

    private bool IsDestinationSafe(Vector3 destination, float radius, out string failReason)
    {
        failReason = string.Empty;

        if (hazardLayers.value != 0 &&
            Physics.CheckSphere(destination, radius, hazardLayers, QueryTriggerInteraction.Collide))
        {
            failReason = "Blink landing blocked by hazard.";
            return false;
        }

        LayerMask landingMask = GetLandingBlockerMask();

        Collider[] hits = landingMask.value != 0
            ? Physics.OverlapSphere(destination, radius, landingMask, QueryTriggerInteraction.Ignore)
            : new Collider[0];
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            if (IsPassThroughUnit(hit))
            {
                continue;
            }

            failReason = "Blink landing blocked by: " + hit.name;
            return false;
        }

        Collider[] unitHits = Physics.OverlapSphere(destination, radius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < unitHits.Length; i++)
        {
            Collider hit = unitHits[i];
            if (hit == null)
            {
                continue;
            }

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            UnitHealth otherHealth = hit.GetComponentInParent<UnitHealth>();
            if (otherHealth != null && !otherHealth.IsDead)
            {
                failReason = "Blink landing blocked by: " + hit.name;
                return false;
            }
        }

        return true;
    }

    private LayerMask GetPathBlockerMask()
    {
        return pathBlockerLayers.value != 0 ? pathBlockerLayers : obstacleLayers;
    }

    private LayerMask GetLandingBlockerMask()
    {
        return landingBlockerLayers.value != 0 ? landingBlockerLayers : obstacleLayers;
    }

    private bool TryFindPathBlocker(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        float radius,
        LayerMask mask,
        out RaycastHit blockerHit)
    {
        blockerHit = default;

        if (mask.value == 0)
        {
            return false;
        }

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            radius,
            direction,
            maxDistance,
            mask,
            QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i].collider;
            if (hit == null)
            {
                continue;
            }

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            if (IsPassThroughUnit(hit))
            {
                continue;
            }

            blockerHit = hits[i];
            return true;
        }

        return false;
    }

    private bool IsPassThroughUnit(Collider hit)
    {
        UnitHealth otherHealth = hit.GetComponentInParent<UnitHealth>();
        return otherHealth != null && !otherHealth.IsDead;
    }
}
