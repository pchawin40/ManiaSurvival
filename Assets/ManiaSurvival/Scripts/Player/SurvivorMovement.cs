using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SurvivorMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 7.5f;
    public float rotationSpeed = 720f;
    public Transform cameraTransform;
    public bool useCameraRelativeMovement = true;

    [Header("Stamina")]
    public float maxStamina = 4f;
    public float staminaDrainPerSecond = 1f;
    public float staminaRegenPerSecond = 1.25f;
    public float minimumSprintStamina = 0.15f;

    [Header("Dodge Blink")]
    public float dodgeDistance = 4f;
    public float dodgeCooldown = 2f;
    public float dodgeStaminaCost = 1.25f;

    [Header("Keyboard Input")]
    public bool keyboardInputEnabled = true;
    public Key sprintKey = Key.LeftShift;
    public Key dodgeKey = Key.Space;

    [Header("Gravity")]
    public float gravity = -20f;

    public float CurrentStamina { get; private set; }
    public float StaminaPercent => maxStamina <= 0f ? 0f : CurrentStamina / maxStamina;
    public bool IsSprinting { get; private set; }

    private CharacterController characterController;
    private UnitHealth health;
    private Vector2 mobileMoveInput;
    private Vector3 lastMoveDirection = Vector3.forward;
    private float verticalVelocity;
    private float dodgeTimer;
    private bool mobileSprintHeld;

    private float speedBoostMultiplier = 1f;
    private Coroutine speedBoostRoutine;
    private float temporarySpeedMultiplier = 1f;
    private float carriedItemSpeedMultiplier = 1f;
    private float temporarySpeedEffectEndTime;
    private Coroutine temporarySpeedEffectRoutine;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        health = GetComponent<UnitHealth>();
        CurrentStamina = maxStamina;
    }

    private void Update()
    {
        if (gameObject == null || !gameObject.activeInHierarchy || !enabled || characterController == null || !characterController.enabled)
        {
            IsSprinting = false;
            return;
        }

        if (health != null && health.IsDead)
        {
            IsSprinting = false;
            return;
        }

        if (ManiaGameManager.Instance != null && !ManiaGameManager.Instance.IsPlaying)
        {
            IsSprinting = false;
            return;
        }

        dodgeTimer = Mathf.Max(0f, dodgeTimer - Time.deltaTime);

        if (keyboardInputEnabled && WasKeyPressedThisFrame(dodgeKey))
        {
            TryDodge();
        }

        Vector2 input = GetMoveInput();
        Vector3 moveDirection = BuildWorldDirection(input);

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            lastMoveDirection = moveDirection.normalized;
        }

        bool wantsSprint = mobileSprintHeld || (keyboardInputEnabled && IsKeyPressed(sprintKey));
        IsSprinting = wantsSprint && input.sqrMagnitude > 0.01f && CurrentStamina > minimumSprintStamina;

        float baseMoveSpeed = IsSprinting ? sprintSpeed : walkSpeed;
        float moveSpeed = baseMoveSpeed * speedBoostMultiplier * temporarySpeedMultiplier * carriedItemSpeedMultiplier;

        if (IsSprinting)
        {
            Debug.Log("Sprint held");
            CurrentStamina = Mathf.Max(0f, CurrentStamina - staminaDrainPerSecond * Time.deltaTime);
        }
        else
        {
            CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + staminaRegenPerSecond * Time.deltaTime);
        }

        ApplyGravity();
        characterController.Move((moveDirection * moveSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);
        RotateToward(moveDirection);
    }

    public void SetMoveInput(Vector2 input)
    {
        mobileMoveInput = Vector2.ClampMagnitude(input, 1f);
    }

    public void SetSprintHeld(bool isHeld)
    {
        mobileSprintHeld = isHeld;
    }

    public void TryDodge()
    {
        if (gameObject == null || !gameObject.activeInHierarchy || !enabled || characterController == null || !characterController.enabled)
        {
            Debug.Log("Dodge blocked: controller inactive");
            return;
        }

        if (dodgeTimer > 0f || CurrentStamina < dodgeStaminaCost)
        {
            Debug.Log("Dodge blocked: cooldown/stamina");
            return;
        }

        Debug.Log("Dodge pressed");
        CurrentStamina -= dodgeStaminaCost;
        dodgeTimer = dodgeCooldown;

        Vector3 dodgeDirection = lastMoveDirection.sqrMagnitude > 0.001f ? lastMoveDirection : transform.forward;
        characterController.Move(dodgeDirection.normalized * dodgeDistance);
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

    private bool WasKeyPressedThisFrame(Key key)
    {
        if (Keyboard.current == null || key == Key.None)
        {
            return false;
        }

        return Keyboard.current[key].wasPressedThisFrame;
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

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -1f;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void RotateToward(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    public void SetCarriedSpeedBoost(float multiplier)
    {
        carriedItemSpeedMultiplier = Mathf.Max(0.1f, multiplier);
    }

    public void ClearCarriedSpeedBoost()
    {
        carriedItemSpeedMultiplier = 1f;
    }

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (speedBoostRoutine != null)
        {
            StopCoroutine(speedBoostRoutine);
        }

        speedBoostRoutine = StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    public void ApplyTemporarySpeedMultiplier(float multiplier, float duration)
    {
        float clampedMultiplier = Mathf.Clamp(multiplier, 0.1f, 2f);
        float clampedDuration = Mathf.Max(0f, duration);

        if (clampedDuration <= 0f)
        {
            return;
        }

        bool alreadySlowed = Time.time < temporarySpeedEffectEndTime;
        if (!alreadySlowed)
        {
            temporarySpeedMultiplier = clampedMultiplier;
        }
        else
        {
            // Keep the stronger slow (smaller multiplier), never stack multiplicatively.
            temporarySpeedMultiplier = Mathf.Min(temporarySpeedMultiplier, clampedMultiplier);
        }

        temporarySpeedEffectEndTime = Mathf.Max(temporarySpeedEffectEndTime, Time.time + clampedDuration);

        if (temporarySpeedEffectRoutine != null)
        {
            StopCoroutine(temporarySpeedEffectRoutine);
        }

        temporarySpeedEffectRoutine = StartCoroutine(TemporarySpeedEffectRoutine());
    }

    private System.Collections.IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        speedBoostMultiplier = Mathf.Max(1f, multiplier);

        Debug.Log($"Speed boost applied: x{speedBoostMultiplier} for {duration} seconds");

        yield return new WaitForSeconds(duration);

        speedBoostMultiplier = 1f;
        speedBoostRoutine = null;

        Debug.Log("Speed boost ended");
    }

    private System.Collections.IEnumerator TemporarySpeedEffectRoutine()
    {
        while (Time.time < temporarySpeedEffectEndTime)
        {
            yield return null;
        }

        temporarySpeedMultiplier = 1f;
        temporarySpeedEffectRoutine = null;
        temporarySpeedEffectEndTime = 0f;

        Debug.Log("Temporary movement slow ended");
    }
}
