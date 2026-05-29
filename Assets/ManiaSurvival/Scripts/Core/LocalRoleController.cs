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
    public MonsterPlayerMovement monsterMovement;
    public MonsterAI monsterAI;
    public MonsterAttack monsterAttack;
    public PredatorClassManager predatorClassManager;
    public MonsterAbilityController monsterAbilityController;
    public MonsterRoarAbility monsterRoar;
    public MonsterStompAbility monsterStomp;
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

        if (survivorMovement != null)
        {
            survivorMovement.enabled = controlMode == PlayerControlMode.SurvivorControlled;
            survivorMovement.SetMoveInput(Vector2.zero);
            survivorMovement.SetSprintHeld(false);
        }

        if (monsterMovement != null)
        {
            monsterMovement.enabled = controlMode == PlayerControlMode.MonsterControlled;
            monsterMovement.SetMoveInput(Vector2.zero);
        }

        if (monsterAI != null)
        {
            monsterAI.enabled = controlMode != PlayerControlMode.MonsterControlled;
        }

        if (monsterAttack != null)
        {
            monsterAttack.autoAttack = controlMode != PlayerControlMode.MonsterControlled;
        }

        UpdateCameraTarget();
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
        if (controlMode != PlayerControlMode.SurvivorControlled)
        {
            return;
        }

        if (survivorClassManager == null)
        {
            survivorClassManager = FindFirstObjectByType<SurvivorClassManager>();
        }

        if (survivorClassManager != null)
        {
            survivorClassManager.ExecutePrimary();
        }
    }

    public void PressSurvivorAbility2()
    {
        if (controlMode != PlayerControlMode.SurvivorControlled)
        {
            return;
        }

        if (survivorClassManager == null)
        {
            survivorClassManager = FindFirstObjectByType<SurvivorClassManager>();
        }

        if (survivorClassManager != null)
        {
            survivorClassManager.ExecuteAbility2();
        }
    }

    public void PressSurvivorAbility3()
    {
        if (controlMode != PlayerControlMode.SurvivorControlled)
        {
            return;
        }

        if (survivorClassManager == null)
        {
            survivorClassManager = FindFirstObjectByType<SurvivorClassManager>();
        }

        if (survivorClassManager != null)
        {
            survivorClassManager.ExecuteAbility3();
        }
    }

    public void PressSurvivorUltimate()
    {
        if (controlMode != PlayerControlMode.SurvivorControlled)
        {
            return;
        }

        if (survivorClassManager == null)
        {
            survivorClassManager = FindFirstObjectByType<SurvivorClassManager>();
        }

        if (survivorClassManager != null)
        {
            survivorClassManager.ExecuteUltimate();
        }
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

        if (monsterMovement == null)
        {
            monsterMovement = FindFirstObjectByType<MonsterPlayerMovement>();
        }

        predatorClassManager = monsterMovement != null
            ? monsterMovement.GetComponent<PredatorClassManager>()
            : FindFirstObjectByType<PredatorClassManager>();

        if (monsterAI == null)
        {
            monsterAI = FindFirstObjectByType<MonsterAI>();
        }

        if (monsterAttack == null)
        {
            monsterAttack = FindFirstObjectByType<MonsterAttack>();
        }

        if (monsterAbilityController == null)
        {
            monsterAbilityController = FindFirstObjectByType<MonsterAbilityController>();
        }

        if (monsterRoar == null)
        {
            monsterRoar = FindFirstObjectByType<MonsterRoarAbility>();
        }

        if (monsterStomp == null)
        {
            monsterStomp = FindFirstObjectByType<MonsterStompAbility>();
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

    public void PressMonsterAttack()
    {
        if (controlMode == PlayerControlMode.MonsterControlled && monsterAttack != null)
        {
            monsterAttack.TryAttack();
        }
    }

    public void PressPredatorMeleeAttack()
    {
        if (controlMode != PlayerControlMode.MonsterControlled)
        {
            return;
        }

        if (predatorClassManager == null)
        {
            predatorClassManager = FindFirstObjectByType<PredatorClassManager>();
        }

        if (predatorClassManager != null)
        {
            predatorClassManager.CastMeleeAttack();
        }
    }

    public void PressPredatorAbility1()
    {
        if (controlMode != PlayerControlMode.MonsterControlled)
        {
            return;
        }

        if (predatorClassManager == null)
        {
            predatorClassManager = FindFirstObjectByType<PredatorClassManager>();
        }

        if (predatorClassManager != null)
        {
            predatorClassManager.CastAbility1();
        }
    }

    public void PressPredatorAbility2()
    {
        if (controlMode != PlayerControlMode.MonsterControlled)
        {
            return;
        }

        if (predatorClassManager == null)
        {
            predatorClassManager = FindFirstObjectByType<PredatorClassManager>();
        }

        if (predatorClassManager != null)
        {
            predatorClassManager.CastAbility2();
        }
    }

    public void PressPredatorAbility3()
    {
        if (controlMode != PlayerControlMode.MonsterControlled)
        {
            return;
        }

        if (predatorClassManager == null)
        {
            predatorClassManager = FindFirstObjectByType<PredatorClassManager>();
        }

        if (predatorClassManager != null)
        {
            predatorClassManager.CastAbility3();
        }
    }

    public void PressPredatorUltimate()
    {
        if (controlMode != PlayerControlMode.MonsterControlled)
        {
            return;
        }

        if (predatorClassManager == null)
        {
            predatorClassManager = FindFirstObjectByType<PredatorClassManager>();
        }

        if (predatorClassManager != null)
        {
            predatorClassManager.CastUltimate();
        }
    }

    public void PressMonsterRoar()
    {
        if (controlMode != PlayerControlMode.MonsterControlled)
        {
            return;
        }

        if (monsterAbilityController == null)
        {
            monsterAbilityController = FindFirstObjectByType<MonsterAbilityController>();
        }

        if (monsterAbilityController != null)
        {
            monsterAbilityController.TryUseRoar();
            return;
        }

        if (monsterRoar == null)
        {
            monsterRoar = FindFirstObjectByType<MonsterRoarAbility>();
        }

        if (monsterRoar != null)
        {
            monsterRoar.CastRoar();
        }
    }

    public void PressMonsterStomp()
    {
        if (controlMode != PlayerControlMode.MonsterControlled)
        {
            return;
        }

        if (monsterStomp == null)
        {
            monsterStomp = FindFirstObjectByType<MonsterStompAbility>();
        }

        if (monsterStomp != null)
        {
            monsterStomp.CastStomp();
        }
    }
}
