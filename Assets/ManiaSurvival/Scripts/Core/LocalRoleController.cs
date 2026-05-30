using UnityEngine;

public enum PlayerControlMode
{
    SurvivorControlled,
    MonsterControlled
}

public class LocalRoleController : MonoBehaviour
{
    [Header("Control")]
    public PlayerControlMode controlMode = PlayerControlMode.SurvivorControlled;
    [Range(0f, 0.5f)] public float joystickDeadZone = 0.15f;

    [Header("References")]
    public SurvivorMovement survivorMovement;
    public SurvivorClassManager survivorClassManager;
    public AbilityController survivorAbilityController;
    public MonsterPlayerMovement monsterMovement;
    public MonsterAI monsterAI;
    public PredatorClassManager predatorClassManager;
    public AbilityController predatorAbilityController;
    public SoulwoodAvatarController soulwoodAvatarController;
    public CameraFollow cameraFollow;
    public bool logCameraTargetChanges = true;

    private void Awake()
    {
        AutoFindReferences();
    }

    private void Start()
    {
        ApplyControlMode();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyControlMode();
        }
    }

    public void SetControlMode(PlayerControlMode newMode)
    {
        controlMode = newMode;
        ApplyControlMode();
    }

    public void ApplyControlMode()
    {
        AutoFindReferences();
        ApplyAllSurvivorControl();
        ApplyStandaloneSurvivorBots();
        ApplyMonsterControl();
        ResetAbilityCooldownsForRoundStart();
        UpdateCameraTarget();
        RefreshAbilityUiLabels();
    }

    private void RefreshAbilityUiLabels()
    {
        ManiaGameUI gameUi = FindFirstObjectByType<ManiaGameUI>();
        if (gameUi == null)
        {
            return;
        }

        gameUi.RefreshAbilityLabels(force: true);
        gameUi.RefreshAbilityInfo(force: true);
    }

    private void ApplyAllSurvivorControl()
    {
        SurvivorMovement[] allSurvivors = FindObjectsByType<SurvivorMovement>(FindObjectsSortMode.None);
        Transform localSurvivorTransform = survivorMovement != null ? survivorMovement.transform : null;

        for (int i = 0; i < allSurvivors.Length; i++)
        {
            SurvivorMovement movement = allSurvivors[i];
            if (movement == null)
            {
                continue;
            }

            bool isLocalPlayerSurvivor = controlMode == PlayerControlMode.SurvivorControlled
                && localSurvivorTransform != null
                && movement.transform == localSurvivorTransform;

            bool enableBot = controlMode == PlayerControlMode.MonsterControlled
                || (controlMode == PlayerControlMode.SurvivorControlled && !isLocalPlayerSurvivor);

            movement.enabled = isLocalPlayerSurvivor;
            if (isLocalPlayerSurvivor)
            {
                movement.SetMoveInput(Vector2.zero);
                movement.SetSprintHeld(false);
            }

            OfflineSurvivorBotAI bot = EnsureSurvivorBotAI(movement);
            bot.enabled = enableBot;

            if (enableBot && controlMode == PlayerControlMode.MonsterControlled)
            {
                Debug.Log("[RoleControl] Monster mode: enabled AI on survivor " + movement.name);
            }
            else if (isLocalPlayerSurvivor)
            {
                Debug.Log("[RoleControl] Survivor mode: local survivor controlled by player, bot disabled on " + movement.name);
            }
        }
    }

    private OfflineSurvivorBotAI EnsureSurvivorBotAI(SurvivorMovement movement)
    {
        OfflineSurvivorBotAI bot = movement.GetComponent<OfflineSurvivorBotAI>();
        if (bot == null)
        {
            bot = movement.gameObject.AddComponent<OfflineSurvivorBotAI>();
        }

        return bot;
    }

    private void ApplyStandaloneSurvivorBots()
    {
        OfflineSurvivorBotAI[] standaloneBots = FindObjectsByType<OfflineSurvivorBotAI>(FindObjectsSortMode.None);
        Transform localSurvivorTransform = survivorMovement != null ? survivorMovement.transform : null;

        for (int i = 0; i < standaloneBots.Length; i++)
        {
            OfflineSurvivorBotAI bot = standaloneBots[i];
            if (bot == null || bot.GetComponent<SurvivorMovement>() != null)
            {
                continue;
            }

            bool isLocalPlayerSurvivor = controlMode == PlayerControlMode.SurvivorControlled
                && localSurvivorTransform != null
                && bot.transform == localSurvivorTransform;

            bool enableBot = controlMode == PlayerControlMode.MonsterControlled
                || (controlMode == PlayerControlMode.SurvivorControlled && !isLocalPlayerSurvivor);

            bot.enabled = enableBot;
        }
    }

    private void ApplyMonsterControl()
    {
        if (monsterMovement != null)
        {
            monsterMovement.enabled = controlMode == PlayerControlMode.MonsterControlled;
            monsterMovement.SetMoveInput(Vector2.zero);
        }

        if (monsterAI != null)
        {
            monsterAI.enabled = controlMode != PlayerControlMode.MonsterControlled;
        }
    }

    private void ResetAbilityCooldownsForRoundStart()
    {
        if (survivorAbilityController != null)
        {
            survivorAbilityController.ResetAllCooldowns();
        }

        if (predatorAbilityController != null)
        {
            predatorAbilityController.ResetAllCooldowns();
        }
    }

    public void SetMoveInput(Vector2 input)
    {
        Vector2 filteredInput = ApplyDeadZone(input);

        if (soulwoodAvatarController != null && soulwoodAvatarController.IsPlayerControlled)
        {
            soulwoodAvatarController.SetMoveInput(filteredInput);
            return;
        }

        if (controlMode == PlayerControlMode.SurvivorControlled)
        {
            if (survivorMovement != null)
            {
                survivorMovement.SetMoveInput(filteredInput);
            }
        }
        else if (monsterMovement != null)
        {
            monsterMovement.SetMoveInput(filteredInput);
        }
    }

    public void SetSprintHeld(bool isHeld)
    {
        if (controlMode == PlayerControlMode.SurvivorControlled && survivorMovement != null)
        {
            survivorMovement.SetSprintHeld(isHeld);
        }
    }

    public void PressDodge()
    {
        if (controlMode == PlayerControlMode.SurvivorControlled && survivorMovement != null)
        {
            survivorMovement.TryDodge();
        }
    }

    public void PressSurvivorPrimary()
    {
        UseSurvivorAbilitySlot(1);
    }

    public void PressSurvivorAbility2()
    {
        UseSurvivorAbilitySlot(2);
    }

    public void PressSurvivorAbility3()
    {
        UseSurvivorAbilitySlot(3);
    }

    public void PressSurvivorUltimate()
    {
        UseSurvivorAbilitySlot(4);
    }

    public void UseSurvivorAbilitySlot(int slotNumber)
    {
        if (controlMode != PlayerControlMode.SurvivorControlled)
        {
            Debug.LogWarning("[LocalRoleController] Ignored survivor slot " + slotNumber + " because control mode is " + controlMode + ".");
            return;
        }

        Debug.Log("[LocalRoleController] Routing slot " + slotNumber + " to survivor AbilityController");

        if (survivorAbilityController == null)
        {
            survivorAbilityController = survivorMovement != null
                ? survivorMovement.GetComponent<AbilityController>()
                : FindFirstObjectByType<AbilityController>();
        }

        if (survivorAbilityController != null)
        {
            survivorAbilityController.UseAbilitySlot(slotNumber);
            return;
        }

        Debug.LogWarning("[LocalRoleController] Missing survivorAbilityController reference on '" + gameObject.name + "'.");
    }

    public void SetSoulwoodAvatarController(SoulwoodAvatarController avatarController)
    {
        soulwoodAvatarController = avatarController;
        SetMoveInput(Vector2.zero);
        UpdateCameraTarget();
    }

    public void ClearSoulwoodAvatarController(SoulwoodAvatarController avatarController)
    {
        if (soulwoodAvatarController == avatarController)
        {
            soulwoodAvatarController = null;
            SetMoveInput(Vector2.zero);
            UpdateCameraTarget();
        }
    }

    public void RestoreSurvivorControl()
    {
        soulwoodAvatarController = null;
        controlMode = PlayerControlMode.SurvivorControlled;
        UpdateCameraTarget();
        Debug.Log("LocalRoleController target restored to Survivor");
    }

    private void AutoFindReferences()
    {
        if (survivorMovement == null)
        {
            survivorMovement = FindFirstObjectByType<SurvivorMovement>();
        }

        survivorClassManager = survivorMovement != null
            ? survivorMovement.GetComponent<SurvivorClassManager>()
            : FindFirstObjectByType<SurvivorClassManager>();
        survivorAbilityController = survivorMovement != null
            ? survivorMovement.GetComponent<AbilityController>()
            : FindFirstObjectByType<AbilityController>();

        if (monsterMovement == null)
        {
            monsterMovement = FindFirstObjectByType<MonsterPlayerMovement>();
        }

        predatorClassManager = monsterMovement != null
            ? monsterMovement.GetComponent<PredatorClassManager>()
            : FindFirstObjectByType<PredatorClassManager>();
        predatorAbilityController = monsterMovement != null
            ? monsterMovement.GetComponent<AbilityController>()
            : FindFirstObjectByType<AbilityController>();

        if (monsterAI == null)
        {
            monsterAI = FindFirstObjectByType<MonsterAI>();
        }

        if (cameraFollow == null)
        {
            cameraFollow = FindFirstObjectByType<CameraFollow>();
        }
    }

    private void UpdateCameraTarget()
    {
        if (cameraFollow == null)
        {
            cameraFollow = FindFirstObjectByType<CameraFollow>();
        }

        if (cameraFollow == null)
        {
            Debug.LogWarning("[LocalRoleController] No CameraFollow found. Add CameraFollow to CameraRig, then assign CameraRig/CameraFollow to LocalRoleController.");
            return;
        }

        if (soulwoodAvatarController != null && soulwoodAvatarController.IsPlayerControlled)
        {
            cameraFollow.target = soulwoodAvatarController.transform;
            LogCameraTarget("Soulwood Avatar", cameraFollow.target);
            return;
        }

        if (controlMode == PlayerControlMode.MonsterControlled && monsterMovement != null)
        {
            cameraFollow.target = monsterMovement.transform;
            LogCameraTarget("Monster", cameraFollow.target);
            return;
        }

        if (survivorMovement != null)
        {
            cameraFollow.target = survivorMovement.transform;
            LogCameraTarget("Survivor", cameraFollow.target);
            return;
        }

        Debug.LogWarning("[LocalRoleController] Camera target not set. Missing SurvivorMovement or MonsterPlayerMovement reference.");
    }

    private void LogCameraTarget(string label, Transform target)
    {
        if (!logCameraTargetChanges)
        {
            return;
        }

        Debug.Log("[LocalRoleController] Camera now follows " + label + ": " + (target != null ? target.name : "none"));
    }

    private Vector2 ApplyDeadZone(Vector2 input)
    {
        if (input.magnitude < joystickDeadZone)
        {
            return Vector2.zero;
        }

        return Vector2.ClampMagnitude(input, 1f);
    }

    public void PressPredatorMeleeAttack()
    {
        UsePredatorAbilitySlot(1);
    }

    public void PressPredatorAbility1()
    {
        UsePredatorAbilitySlot(1);
    }

    public void PressPredatorAbility2()
    {
        UsePredatorAbilitySlot(2);
    }

    public void PressPredatorAbility3()
    {
        UsePredatorAbilitySlot(3);
    }

    public void PressPredatorUltimate()
    {
        UsePredatorAbilitySlot(4);
    }

    public void UsePredatorAbilitySlot(int slotNumber)
    {
        if (controlMode != PlayerControlMode.MonsterControlled)
        {
            Debug.LogWarning("[LocalRoleController] Ignored predator slot " + slotNumber + " because control mode is " + controlMode + ".");
            return;
        }

        Debug.Log("[LocalRoleController] Routing slot " + slotNumber + " to predator AbilityController");

        if (predatorAbilityController == null)
        {
            predatorAbilityController = monsterMovement != null
                ? monsterMovement.GetComponent<AbilityController>()
                : FindFirstObjectByType<AbilityController>();
        }

        if (predatorAbilityController != null)
        {
            predatorAbilityController.UseAbilitySlot(slotNumber);
            return;
        }

        Debug.LogWarning("[LocalRoleController] Missing predatorAbilityController reference on '" + gameObject.name + "'.");
    }
}
