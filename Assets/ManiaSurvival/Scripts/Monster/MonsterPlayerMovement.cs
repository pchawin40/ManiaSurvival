using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class MonsterPlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5.75f;
    public float rotationSpeed = 720f;
    public Transform cameraTransform;
    public bool useCameraRelativeMovement = true;

    [Header("Keyboard Input")]
    public bool keyboardInputEnabled = true;

    [Header("Gravity")]
    public float gravity = -20f;

    [Header("Aim")]
    [Tooltip("Gameplay aim used by predator abilities. Updated from move input, not lagging model rotation.")]
    public Vector3 lastAimDirection = Vector3.forward;
    public Vector3 lastMoveDirection = Vector3.forward;

    private CharacterController characterController;
    private UnitHealth unitHealth;
    private Vector2 mobileMoveInput;
    private float verticalVelocity;
    private float carriedItemSpeedMultiplier = 1f;
    private float abilitySpeedMultiplier = 1f;
    private bool hasStoredAim;
    private bool loggedInactiveController;

    public bool HasStoredAim => hasStoredAim;

    public Vector3 GetGameplayAimDirection()
    {
        Vector3 aim = hasStoredAim ? lastAimDirection : transform.forward;
        aim.y = 0f;
        if (aim.sqrMagnitude <= 0.001f)
        {
            aim = Vector3.forward;
        }

        return aim.normalized;
    }

    public void SetAbilitySpeedMultiplier(float multiplier)
    {
        abilitySpeedMultiplier = Mathf.Max(0.1f, multiplier);
    }

    public void ClearAbilitySpeedMultiplier()
    {
        abilitySpeedMultiplier = 1f;
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        unitHealth = GetComponent<UnitHealth>();
    }

    private void Update()
    {
        if (!enabled || !CanUseCharacterController())
        {
            return;
        }

        if (ManiaGameManager.Instance != null && !ManiaGameManager.Instance.IsPlaying)
        {
            return;
        }

        Vector2 input = GetMoveInput();
        UpdateAimFromInput(input);
        Vector3 moveDirection = BuildWorldDirection(input);

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            ApplyGravity();
            TryMove((moveDirection * moveSpeed * carriedItemSpeedMultiplier * abilitySpeedMultiplier + Vector3.up * verticalVelocity) * Time.deltaTime);
            RotateToward(moveDirection);
            return;
        }

        ApplyGravityOnly();
    }

    public void SetMoveInput(Vector2 input)
    {
        mobileMoveInput = Vector2.ClampMagnitude(input, 1f);
    }

    private Vector2 GetMoveInput()
    {
        Vector2 keyboardInput = Vector2.zero;

        if (keyboardInputEnabled)
        {
            if (IsKeyPressed(Key.A) || IsKeyPressed(Key.LeftArrow))
            {
                keyboardInput.x -= 1f;
            }

            if (IsKeyPressed(Key.D) || IsKeyPressed(Key.RightArrow))
            {
                keyboardInput.x += 1f;
            }

            if (IsKeyPressed(Key.S) || IsKeyPressed(Key.DownArrow))
            {
                keyboardInput.y -= 1f;
            }

            if (IsKeyPressed(Key.W) || IsKeyPressed(Key.UpArrow))
            {
                keyboardInput.y += 1f;
            }
        }

        Vector2 input = keyboardInput.sqrMagnitude > 0.01f ? keyboardInput : mobileMoveInput;
        return Vector2.ClampMagnitude(input, 1f);
    }

    private bool IsKeyPressed(Key key)
    {
        if (Keyboard.current == null || key == Key.None)
        {
            return false;
        }

        return Keyboard.current[key].isPressed;
    }

    private void UpdateAimFromInput(Vector2 input)
    {
        Vector3 direction = BuildWorldDirection(input);
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        lastAimDirection = direction;
        lastMoveDirection = direction;
        hasStoredAim = true;
    }

    private Vector3 BuildWorldDirection(Vector2 input)
    {
        if (input.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        if (!useCameraRelativeMovement || cameraTransform == null)
        {
            return new Vector3(input.x, 0f, input.y).normalized;
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        return (cameraForward.normalized * input.y + cameraRight.normalized * input.x).normalized;
    }

    private void ApplyGravityOnly()
    {
        ApplyGravity();
        TryMove(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    private bool CanUseCharacterController()
    {
        if (characterController == null || !characterController.enabled || !characterController.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (unitHealth != null && unitHealth.IsDead)
        {
            return false;
        }

        return true;
    }

    private bool TryMove(Vector3 motion)
    {
        if (!CanUseCharacterController())
        {
            if (!loggedInactiveController)
            {
                loggedInactiveController = true;
                Debug.LogWarning("[MonsterPlayerMovement] Skipping movement on inactive CharacterController on '" + gameObject.name + "'.");
            }

            return false;
        }

        characterController.Move(motion);
        return true;
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -1f;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    public void SetCarriedSpeedBoost(float multiplier)
    {
        carriedItemSpeedMultiplier = Mathf.Max(0.1f, multiplier);
    }

    public void ClearCarriedSpeedBoost()
    {
        carriedItemSpeedMultiplier = 1f;
    }

    private void RotateToward(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        lastAimDirection = moveDirection.normalized;
        lastMoveDirection = lastAimDirection;
        hasStoredAim = true;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
