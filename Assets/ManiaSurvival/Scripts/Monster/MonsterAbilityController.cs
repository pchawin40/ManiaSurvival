using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(UnitHealth))]
public class MonsterAbilityController : MonoBehaviour
{
    [Header("Roar")]
    public bool roarEnabled = true;
    public float roarRadius = 5f;
    public float roarCooldown = 10f;
    public float roarSlowDuration = 3f;
    [Range(0.65f, 0.75f)] public float roarSlowMultiplier = 0.7f;
    public LayerMask targetLayers = ~0;
    [Tooltip("Tags that count as valid targets. Use only tags that are defined in your project.")]
    public string[] targetTags = { "Survivor" };

    [Header("Roar UI")]
    [Tooltip("Label shown on the Roar ability button. Change per predator type.")]
    public string roarDisplayName = "Roar";
    public AbilityCooldownButton roarCooldownButton;

    [Header("Leap")]
    public bool leapEnabled = true;
    [Tooltip("Distance travelled during the leap (units).")]
    public float leapDistance = 6f;
    [Tooltip("Time the leap motion lasts. Shorter = snappier.")]
    public float leapDuration = 0.22f;
    public float leapCooldown = 10f;
    [Tooltip("Radius checked on landing for survivor hits.")]
    public float leapLandingRadius = 1.75f;
    [Tooltip("Flat HP damage applied on a clean landing hit. ~20 % of default 100 HP.")]
    public int leapDamage = 20;

    [Header("Leap UI")]
    [Tooltip("Label shown on the Leap ability button. Change per predator type.")]
    public string leapDisplayName = "Leap";
    public AbilityCooldownButton leapCooldownButton;

    [Header("Input")]
    public bool keyboardInputEnabled = true;
    public Key roarKey = Key.R;
    public Key leapKey = Key.F;

    [Header("Rules")]
    [Tooltip("When true, roar can only be cast while local player controls Monster mode.")]
    public bool requireMonsterControlled = true;

    [Header("References")]
    public LocalRoleController localRoleController;
    public ManiaGameManager gameManager;

    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool drawRoarRadius = true;

    private UnitHealth unitHealth;
    private CharacterController characterController;

    // ── Roar state ─────────────────────────────────────────────────────────────
    private float nextRoarTime;
    private readonly HashSet<SurvivorMovement> affectedSurvivors = new HashSet<SurvivorMovement>();

    // ── Leap state ──────────────────────────────────────────────────────────────
    private float nextLeapTime;
    private bool isLeaping;

    public bool IsLeaping => isLeaping;

    private void Awake()
    {
        unitHealth = GetComponent<UnitHealth>();
        characterController = GetComponent<CharacterController>();

        if (localRoleController == null)
        {
            localRoleController = FindFirstObjectByType<LocalRoleController>();
        }

        if (gameManager == null)
        {
            gameManager = ManiaGameManager.Instance;
        }

        if (roarCooldownButton != null)
        {
            roarCooldownButton.SetAbilityInfo(roarDisplayName, 0);
        }

        if (leapCooldownButton != null)
        {
            leapCooldownButton.SetAbilityInfo(leapDisplayName, 0);
        }
    }

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<ManiaGameManager>();
        }
    }

    private void Update()
    {
        if (!keyboardInputEnabled || Keyboard.current == null)
        {
            return;
        }

        if (roarEnabled && roarKey != Key.None && Keyboard.current[roarKey].wasPressedThisFrame)
        {
            TryUseRoar();
        }

        if (leapEnabled && leapKey != Key.None && Keyboard.current[leapKey].wasPressedThisFrame)
        {
            TryUseLeap();
        }
    }

    public void TryUseRoar()
    {
        if (!roarEnabled)
        {
            return;
        }

        if (unitHealth == null)
        {
            unitHealth = GetComponent<UnitHealth>();
        }

        if (unitHealth == null || unitHealth.IsDead)
        {
            return;
        }

        if (gameManager != null && gameManager.State != ManiaGameState.Playing)
        {
            return;
        }

        if (requireMonsterControlled && localRoleController != null && localRoleController.controlMode != PlayerControlMode.MonsterControlled)
        {
            if (showDebugLogs)
            {
                Debug.Log("[MonsterAbilityController] Roar blocked: not in Monster control mode.");
            }

            return;
        }

        if (roarCooldownButton != null && roarCooldownButton.IsCoolingDown)
        {
            if (showDebugLogs)
            {
                Debug.Log("[MonsterAbilityController] Roar on cooldown (button).");
            }

            return;
        }

        if (Time.time < nextRoarTime)
        {
            if (showDebugLogs)
            {
                float remaining = Mathf.Max(0f, nextRoarTime - Time.time);
                Debug.Log("[MonsterAbilityController] Roar on cooldown: " + remaining.ToString("0.0") + "s remaining.");
            }

            return;
        }

        nextRoarTime = Time.time + Mathf.Max(0.1f, roarCooldown);

        if (roarCooldownButton != null)
        {
            roarCooldownButton.StartCooldown(roarCooldown);
        }

        ApplyRoarSlow();
    }

    // Public wrapper for future mobile button hookup.
    public void TriggerRoarFromButton()
    {
        TryUseRoar();
    }

    private void ApplyRoarSlow()
    {
        affectedSurvivors.Clear();

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            Mathf.Max(0.1f, roarRadius),
            targetLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            SurvivorMovement movement = hits[i].GetComponentInParent<SurvivorMovement>();
            if (movement == null)
            {
                continue;
            }

            UnitHealth survivorHealth = movement.GetComponent<UnitHealth>();
            if (survivorHealth == null || survivorHealth.IsDead)
            {
                continue;
            }

            if (!HasAnyTargetTag(survivorHealth))
            {
                continue;
            }

            if (affectedSurvivors.Add(movement))
            {
                movement.ApplyTemporarySpeedMultiplier(roarSlowMultiplier, roarSlowDuration);
            }
        }

        if (!showDebugLogs)
        {
            return;
        }

        if (affectedSurvivors.Count == 0)
        {
            Debug.Log("[MonsterAbilityController] Roar activated. No survivors in range.");
            return;
        }

        string[] names = new string[affectedSurvivors.Count];
        int index = 0;
        foreach (SurvivorMovement movement in affectedSurvivors)
        {
            names[index++] = movement.name;
        }

        Debug.Log("[MonsterAbilityController] Roar activated. Slowed " + affectedSurvivors.Count + " survivor(s): " + string.Join(", ", names));
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  LEAP
    // ══════════════════════════════════════════════════════════════════════════

    public void TryUseLeap()
    {
        if (!leapEnabled)
        {
            return;
        }

        if (unitHealth == null)
        {
            unitHealth = GetComponent<UnitHealth>();
        }

        if (unitHealth == null || unitHealth.IsDead)
        {
            return;
        }

        if (gameManager != null && gameManager.State != ManiaGameState.Playing)
        {
            return;
        }

        if (requireMonsterControlled && localRoleController != null &&
            localRoleController.controlMode != PlayerControlMode.MonsterControlled)
        {
            if (showDebugLogs)
            {
                Debug.Log("[MonsterAbilityController] Leap blocked: not in Monster control mode.");
            }

            return;
        }

        if (isLeaping)
        {
            return;
        }

        if (leapCooldownButton != null && leapCooldownButton.IsCoolingDown)
        {
            if (showDebugLogs)
            {
                Debug.Log("[MonsterAbilityController] Leap on cooldown (button).");
            }

            return;
        }

        if (Time.time < nextLeapTime)
        {
            if (showDebugLogs)
            {
                float remaining = Mathf.Max(0f, nextLeapTime - Time.time);
                Debug.Log("Predator Leap on cooldown: " + remaining.ToString("0.0") + "s");
            }

            return;
        }

        nextLeapTime = Time.time + Mathf.Max(0.1f, leapCooldown);

        if (leapCooldownButton != null)
        {
            leapCooldownButton.StartCooldown(leapCooldown);
        }

        StartCoroutine(LeapCoroutine());
    }

    // Public wrapper for future mobile button hookup.
    public void TriggerLeapFromButton()
    {
        TryUseLeap();
    }

    private System.Collections.IEnumerator LeapCoroutine()
    {
        isLeaping = true;

        if (showDebugLogs)
        {
            Debug.Log("Predator Leap started");
        }

        Vector3 leapDir = transform.forward;
        leapDir.y = 0f;
        leapDir = leapDir.sqrMagnitude > 0.001f ? leapDir.normalized : Vector3.forward;

        float speed = leapDistance / Mathf.Max(0.01f, leapDuration);
        float elapsed = 0f;
        float distanceTravelled = 0f;
        float gravVelocity = 0f;
        const float leapGravity = -20f;

        while (elapsed < leapDuration && distanceTravelled < leapDistance)
        {
            float dt = Time.deltaTime;
            float step = Mathf.Min(speed * dt, leapDistance - distanceTravelled);

            if (characterController != null && characterController.isGrounded)
            {
                gravVelocity = -1f;
            }

            gravVelocity += leapGravity * dt;

            Vector3 move = leapDir * step + Vector3.up * gravVelocity * dt;

            CollisionFlags flags = characterController != null
                ? characterController.Move(move)
                : CollisionFlags.None;

            distanceTravelled += step;
            elapsed += dt;

            if ((flags & CollisionFlags.Sides) != 0)
            {
                if (showDebugLogs)
                {
                    Debug.Log("[MonsterAbilityController] Leap stopped by wall.");
                }

                break;
            }

            yield return null;
        }

        isLeaping = false;
        ApplyLeapLandingDamage();
    }

    private void ApplyLeapLandingDamage()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            Mathf.Max(0.1f, leapLandingRadius),
            targetLayers,
            QueryTriggerInteraction.Ignore);

        bool hitAny = false;

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth survivorHealth = hits[i].GetComponentInParent<UnitHealth>();
            if (survivorHealth == null || survivorHealth.IsDead)
            {
                continue;
            }

            if (!HasAnyTargetTag(survivorHealth))
            {
                continue;
            }

            // Apply damage but never reduce below 1 HP — no one-shots.
            int dmg = Mathf.Max(1, leapDamage);
            dmg = Mathf.Min(dmg, survivorHealth.currentHealth - 1);
            if (dmg <= 0)
            {
                continue;
            }

            survivorHealth.TakeDamage(dmg, gameObject);

            if (showDebugLogs)
            {
                Debug.Log("Predator Leap hit " + survivorHealth.name);
            }

            hitAny = true;
        }

        if (!hitAny && showDebugLogs)
        {
            Debug.Log("Predator Leap missed");
        }
    }

    private bool HasAnyTargetTag(UnitHealth target)
    {
        if (targetTags == null || targetTags.Length == 0)
        {
            return true;
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

    private void OnDrawGizmosSelected()
    {
        if (drawRoarRadius)
        {
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, roarRadius));
        }

        if (leapEnabled)
        {
            // Show leap reach and landing-hit radius in cyan.
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
            Vector3 leapEnd = transform.position + transform.forward * leapDistance;
            Gizmos.DrawLine(transform.position, leapEnd);
            Gizmos.DrawWireSphere(leapEnd, Mathf.Max(0.1f, leapLandingRadius));
        }
    }
}
