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
    public string[] targetTags = { "Survivor", "Camper", "Player" };

    [Header("Input")]
    public bool keyboardInputEnabled = true;
    public Key roarKey = Key.R;

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
    private float nextRoarTime;
    private readonly HashSet<SurvivorMovement> affectedSurvivors = new HashSet<SurvivorMovement>();

    private void Awake()
    {
        unitHealth = GetComponent<UnitHealth>();

        if (localRoleController == null)
        {
            localRoleController = FindFirstObjectByType<LocalRoleController>();
        }

        if (gameManager == null)
        {
            gameManager = ManiaGameManager.Instance;
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
        if (!keyboardInputEnabled || !roarEnabled || Keyboard.current == null || roarKey == Key.None)
        {
            return;
        }

        if (Keyboard.current[roarKey].wasPressedThisFrame)
        {
            TryUseRoar();
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
        if (!drawRoarRadius)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, roarRadius));
    }
}
