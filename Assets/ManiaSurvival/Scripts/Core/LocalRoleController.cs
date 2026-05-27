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
    public MonsterPlayerMovement monsterMovement;
    public MonsterAI monsterAI;
    public MonsterAttack monsterAttack;
    public MonsterRoarAbility monsterRoar;
    public MonsterStompAbility monsterStomp;
    public SoulwoodAvatarController soulwoodAvatarController;

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
    }

    public void SetMoveInput(Vector2 input)
    {
        Vector2 filteredInput = ApplyDeadZone(input);

        if (soulwoodAvatarController != null)
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

    public void SetSoulwoodAvatarController(SoulwoodAvatarController avatarController)
    {
        soulwoodAvatarController = avatarController;
        SetMoveInput(Vector2.zero);
    }

    public void ClearSoulwoodAvatarController(SoulwoodAvatarController avatarController)
    {
        if (soulwoodAvatarController == avatarController)
        {
            soulwoodAvatarController = null;
            SetMoveInput(Vector2.zero);
        }
    }

    public void RestoreSurvivorControl()
    {
        soulwoodAvatarController = null;
        controlMode = PlayerControlMode.SurvivorControlled;
        Debug.Log("LocalRoleController target restored to Survivor");
    }

    private void AutoFindReferences()
    {
        if (survivorMovement == null)
        {
            survivorMovement = FindFirstObjectByType<SurvivorMovement>();
        }

        if (monsterMovement == null)
        {
            monsterMovement = FindFirstObjectByType<MonsterPlayerMovement>();
        }

        if (monsterAI == null)
        {
            monsterAI = FindFirstObjectByType<MonsterAI>();
        }

        if (monsterAttack == null)
        {
            monsterAttack = FindFirstObjectByType<MonsterAttack>();
        }

        if (monsterRoar == null)
        {
            monsterRoar = FindFirstObjectByType<MonsterRoarAbility>();
        }

        if (monsterStomp == null)
        {
            monsterStomp = FindFirstObjectByType<MonsterStompAbility>();
        }
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

    public void PressMonsterRoar()
    {
        if (controlMode != PlayerControlMode.MonsterControlled)
        {
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
