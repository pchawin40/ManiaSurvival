using UnityEngine;

[RequireComponent(typeof(UnitHealth))]
public class SurvivorBlinkAbility : MonoBehaviour
{
    [Header("Blink")]
    public float blinkDistance = 5.5f;
    public float blinkCooldown = 9f;
    public float collisionCheckRadius = 0.4f;

    [Header("Mana")]
    public string abilityDisplayName = "Blink";
    [Min(0)] public int manaCost = 3;

    [Header("UI")]
    public AbilityCooldownButton cooldownButton;

    [Header("Collision")]
    public LayerMask obstacleLayers = ~0;
    [Tooltip("Hazard layers like HellPit/Water. Blink destination is rejected if inside these volumes.")]
    public LayerMask hazardLayers;

    private UnitHealth unitHealth;
    private CharacterController characterController;
    private SurvivorMovement survivorMovement;
    private SurvivorMana mana;
    private float nextBlinkTime;

    private void Awake()
    {
        unitHealth = GetComponent<UnitHealth>();
        characterController = GetComponent<CharacterController>();
        survivorMovement = GetComponent<SurvivorMovement>();
        mana = SurvivorMana.EnsureOn(gameObject, manaCost);
        if (cooldownButton != null)
        {
            cooldownButton.SetAbilityInfo(abilityDisplayName, manaCost);
        }
    }

    public void Blinkstep()
    {
        if (cooldownButton != null && cooldownButton.IsCoolingDown)
        {
            Debug.Log("blink on cooldown");
            return;
        }

        if (unitHealth == null)
        {
            unitHealth = GetComponent<UnitHealth>();
        }

        if (unitHealth == null || unitHealth.IsDead)
        {
            Debug.Log("blink blocked");
            return;
        }

        if (Time.time < nextBlinkTime)
        {
            Debug.Log("blink on cooldown");
            return;
        }

        Vector3 blinkDirection = GetBlinkDirection();
        if (blinkDirection.sqrMagnitude <= 0.001f)
        {
            Debug.Log("blink blocked");
            return;
        }

        if (!TryGetBlinkDestination(blinkDirection, out Vector3 destination))
        {
            Debug.Log("blink blocked");
            return;
        }

        if (manaCost > 0)
        {
            if (mana == null)
            {
                mana = SurvivorMana.EnsureOn(gameObject, manaCost);
            }

            if (mana == null || !mana.SpendMana(manaCost))
            {
                Debug.Log("blink blocked: not enough mana");
                return;
            }
        }

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        transform.position = destination;

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        nextBlinkTime = Time.time + blinkCooldown;
        if (cooldownButton != null)
        {
            cooldownButton.StartCooldown(blinkCooldown);
        }
        Debug.Log("blink used");
    }

    private Vector3 GetBlinkDirection()
    {
        Vector3 direction = transform.forward;
        direction.y = 0f;
        direction = direction.normalized;

        if (survivorMovement != null && survivorMovement.useCameraRelativeMovement && survivorMovement.cameraTransform != null)
        {
            Vector3 cameraForward = survivorMovement.cameraTransform.forward;
            cameraForward.y = 0f;
            if (cameraForward.sqrMagnitude > 0.001f)
            {
                direction = cameraForward.normalized;
            }
        }

        return direction;
    }

    private bool TryGetBlinkDestination(Vector3 blinkDirection, out Vector3 destination)
    {
        Vector3 origin = transform.position;
        float maxDistance = Mathf.Max(0f, blinkDistance);
        float radius = Mathf.Max(0.01f, collisionCheckRadius);

        if (Physics.SphereCast(origin, radius, blinkDirection, out RaycastHit hit, maxDistance, obstacleLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null && (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)))
            {
                destination = origin + blinkDirection * maxDistance;
                return true;
            }

            float safeDistance = Mathf.Max(0f, hit.distance - radius);
            if (safeDistance <= 0.05f)
            {
                destination = origin;
                return false;
            }

            destination = origin + blinkDirection * safeDistance;
            return IsDestinationSafe(destination, radius);
        }

        destination = origin + blinkDirection * maxDistance;
        return IsDestinationSafe(destination, radius);
    }

    private bool IsDestinationSafe(Vector3 destination, float radius)
    {
        if (hazardLayers.value != 0 &&
            Physics.CheckSphere(destination, radius, hazardLayers, QueryTriggerInteraction.Collide))
        {
            return false;
        }

        if (obstacleLayers.value == 0)
        {
            return true;
        }

        Collider[] hits = Physics.OverlapSphere(destination, radius, obstacleLayers, QueryTriggerInteraction.Ignore);
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

            return false;
        }

        return true;
    }
}
