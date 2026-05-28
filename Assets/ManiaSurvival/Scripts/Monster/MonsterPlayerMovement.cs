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

    private CharacterController characterController;
    private MonsterAbilityController abilityController;
    private Vector2 mobileMoveInput;
    private float verticalVelocity;
    private float carriedItemSpeedMultiplier = 1f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        abilityController = GetComponent<MonsterAbilityController>();
    }

    private void Update()
    {
        if (ManiaGameManager.Instance != null && !ManiaGameManager.Instance.IsPlaying)
        {
            return;
        }

        // Leap coroutine drives movement directly — skip normal input while active.
        if (abilityController != null && abilityController.IsLeaping)
        {
            return;
        }

        Vector2 input = GetMoveInput();
        Vector3 moveDirection = BuildWorldDirection(input);

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            ApplyGravity();
            characterController.Move((moveDirection * moveSpeed * carriedItemSpeedMultiplier + Vector3.up * verticalVelocity) * Time.deltaTime);
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
        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
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

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
