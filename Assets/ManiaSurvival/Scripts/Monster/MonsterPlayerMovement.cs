using System.Collections;
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
    public Key jumpKey = Key.Space;
    public Key pounceKey = Key.Q;
    public Key pounceModifierKey = Key.LeftShift;

    [Header("Predator Jump")]
    public bool enablePredatorJump = false;
    public float predatorJumpHeight = 1.4f;
    public int predatorMaxJumps = 1;
    public float groundedCheckDistance = 0.15f;
    public float jumpCooldown = 0.08f;

    [Header("Predator Pounce")]
    public bool enablePredatorPounce = true;
    public float pounceDistance = 6f;
    public float pounceDuration = 0.25f;
    public float pounceCooldown = 4f;
    public float pounceWindup = 0.15f;
    public float pounceLandingRadius = 2.5f;
    public float pounceLandingKnockback = 3.5f;
    public float pounceLandingDamage = 6f;
    public bool pounceCanCrossLowObstacles = true;
    public bool pounceCannotExitArena = true;

    [Header("Predator Vault")]
    public float predatorStepOffset = 0.45f;

    [Header("Landing Feel")]
    public float heavyFallVelocityThreshold = 14f;
    public float landingDustKnockback = 1.2f;
    public float landingDustRadius = 1.75f;

    [Header("Gravity")]
    public float gravity = -22f;

    [Header("Targets")]
    public string survivorTag = "Survivor";

    [Header("Debug")]
    public bool movementDebugLogs;

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
    private int jumpsUsed;
    private float lastJumpTime;
    private float nextPounceTime;
    private float defaultStepOffset;
    private bool hasStoredAim;
    private bool loggedInactiveController;
    private bool wasGroundedLastFrame;
    private bool isPouncing;
    private Coroutine pounceRoutine;
    private Animator animator;

    public bool IsPouncing => isPouncing;
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

    public void ApplyPredatorMovementProfile(PredatorClass predatorClass)
    {
        switch (predatorClass)
        {
            case PredatorClass.SwarmOverlord:
                pounceDistance = 4.5f;
                pounceCooldown = 5f;
                pounceLandingRadius = 2.5f;
                pounceLandingKnockback = 3.5f;
                break;
            case PredatorClass.Juggernaut:
                pounceDistance = 8f;
                pounceCooldown = 6f;
                pounceLandingRadius = 3.5f;
                pounceLandingKnockback = 5f;
                break;
            case PredatorClass.ShadowStalker:
                pounceDistance = 7f;
                pounceCooldown = 4.5f;
                pounceLandingRadius = 2.2f;
                pounceLandingKnockback = 3f;
                break;
            case PredatorClass.IronColossus:
                pounceDistance = 5f;
                pounceCooldown = 7f;
                pounceLandingRadius = 3f;
                pounceLandingKnockback = 4.5f;
                break;
            case PredatorClass.PlagueGardener:
                pounceDistance = 5.5f;
                pounceCooldown = 5.5f;
                pounceLandingRadius = 2.8f;
                pounceLandingKnockback = 3.2f;
                break;
            default:
                pounceDistance = 6f;
                pounceCooldown = 4f;
                pounceLandingRadius = 2.5f;
                pounceLandingKnockback = 3.5f;
                break;
        }
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        unitHealth = GetComponent<UnitHealth>();
        animator = GetComponent<Animator>();

        if (characterController != null)
        {
            defaultStepOffset = characterController.stepOffset;
            characterController.stepOffset = Mathf.Max(defaultStepOffset, predatorStepOffset);
        }

        PredatorClassManager classManager = GetComponent<PredatorClassManager>();
        if (classManager != null)
        {
            ApplyPredatorMovementProfile(classManager.GetCurrentPredatorClass());
        }
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

        if (isPouncing)
        {
            return;
        }

        if (keyboardInputEnabled)
        {
            if (WasPouncePressedThisFrame())
            {
                TryPounce();
            }
            else if (WasKeyPressedThisFrame(jumpKey))
            {
                TryJump();
            }
        }

        Vector2 input = GetMoveInput();
        UpdateAimFromInput(input);
        Vector3 moveDirection = BuildWorldDirection(input);
        float effectiveSpeed = moveSpeed * carriedItemSpeedMultiplier * abilitySpeedMultiplier;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            ApplyGravity();
            TryMove((moveDirection * effectiveSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);
            RotateToward(moveDirection);
            UpdateMoveAnimation(effectiveSpeed);
            TrackHeavyLanding();
            return;
        }

        UpdateMoveAnimation(0f);
        ApplyGravityOnly();
        TrackHeavyLanding();
    }

    private bool WasPouncePressedThisFrame()
    {
        if (!enablePredatorPounce)
        {
            return false;
        }

        if (WasKeyPressedThisFrame(pounceKey))
        {
            return true;
        }

        return IsKeyPressed(pounceModifierKey) && WasKeyPressedThisFrame(jumpKey);
    }

    public bool TryPounce()
    {
        if (!enablePredatorPounce || !CanUseCharacterController() || isPouncing)
        {
            return false;
        }

        if (Time.time < nextPounceTime)
        {
            if (movementDebugLogs)
            {
                Debug.Log("[Movement] Pounce blocked: cooldown");
            }

            return false;
        }

        Vector3 aim = GetGameplayAimDirection();
        Vector3 landingPoint = transform.position + aim * pounceDistance;
        landingPoint.y = transform.position.y;

        if (pounceCannotExitArena && !PlayableBoundsHelper.IsPositionInsidePlayableBounds(landingPoint))
        {
            if (movementDebugLogs)
            {
                Debug.Log("[Movement] Pounce blocked: would leave arena");
            }

            return false;
        }

        if (pounceRoutine != null)
        {
            StopCoroutine(pounceRoutine);
        }

        pounceRoutine = StartCoroutine(PounceRoutine(aim));
        return true;
    }

    private IEnumerator PounceRoutine(Vector3 aimDirection)
    {
        isPouncing = true;
        nextPounceTime = Time.time + Mathf.Max(0.1f, pounceCooldown);
        Vector3 landingPoint = transform.position + aimDirection * pounceDistance;
        landingPoint.y = transform.position.y;

        SpawnPounceWindupRing();
        PredatorAbilityFeelVfx.SpawnWarningCircle(
            landingPoint,
            pounceLandingRadius,
            Mathf.Max(0.1f, pounceWindup + pounceDuration * 0.5f),
            new Color(1f, 0.35f, 0.15f, 0.45f));

        float windupEnd = Time.time + Mathf.Max(0.05f, pounceWindup);
        while (Time.time < windupEnd)
        {
            yield return null;
        }

        float savedStepOffset = characterController != null ? characterController.stepOffset : predatorStepOffset;
        if (characterController != null && pounceCanCrossLowObstacles)
        {
            characterController.stepOffset = Mathf.Max(savedStepOffset, predatorStepOffset + 0.2f);
        }

        Vector3 start = transform.position;
        float duration = Mathf.Max(0.05f, pounceDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (!CanUseCharacterController())
            {
                break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 target = Vector3.Lerp(start, landingPoint, t);
            target.y = start.y + Mathf.Sin(t * Mathf.PI) * 0.35f;

            if (pounceCannotExitArena && !PlayableBoundsHelper.IsPositionInsidePlayableBounds(target))
            {
                if (movementDebugLogs)
                {
                    Debug.Log("[Movement] Pounce cancelled: left arena mid-lunge");
                }

                break;
            }

            Vector3 delta = target - transform.position;
            TryMove(delta);
            yield return null;
        }

        if (characterController != null)
        {
            characterController.stepOffset = savedStepOffset;
        }

        PlayableBoundsHelper.ClampUnitIfOutside(transform, characterController, "predator-pounce");
        ApplyPounceLandingShockwave(landingPoint);
        SpawnPounceLandingShockwave(landingPoint);

        isPouncing = false;
        pounceRoutine = null;

        if (movementDebugLogs)
        {
            Debug.Log("[Movement] Predator pounce landed at " + transform.position);
        }
    }

    private void ApplyPounceLandingShockwave(Vector3 center)
    {
        float radius = Mathf.Max(0.5f, pounceLandingRadius);
        int damage = Mathf.Max(1, Mathf.RoundToInt(pounceLandingDamage));
        UnitHealth[] units = FindObjectsByType<UnitHealth>(FindObjectsSortMode.None);

        for (int i = 0; i < units.Length; i++)
        {
            UnitHealth target = units[i];
            if (!IsValidPounceTarget(target))
            {
                continue;
            }

            Vector3 offset = target.transform.position - center;
            offset.y = 0f;
            if (offset.sqrMagnitude > radius * radius)
            {
                continue;
            }

            target.TakeDamage(damage, gameObject);
            ApplyLightKnockback(target, offset.normalized, pounceLandingKnockback);
        }
    }

    private void ApplyLightKnockback(UnitHealth target, Vector3 direction, float distance)
    {
        if (target == null || target.IsDead || direction.sqrMagnitude <= 0.001f || distance <= 0f)
        {
            return;
        }

        Vector3 destination = target.transform.position + direction.normalized * distance;
        destination.y = target.transform.position.y;
        destination = PlayableBoundsHelper.ClampToPlayableBounds(destination);

        CharacterController cc = target.GetComponent<CharacterController>();
        bool controllerWasEnabled = cc != null && cc.enabled;
        if (controllerWasEnabled)
        {
            cc.enabled = false;
        }

        target.transform.position = destination;

        if (cc != null && controllerWasEnabled && target.gameObject.activeInHierarchy)
        {
            cc.enabled = true;
        }

        SurvivorMovement survivorMove = target.GetComponent<SurvivorMovement>();
        if (survivorMove != null)
        {
            survivorMove.ApplyExternalMovementLock(0.2f);
        }
    }

    private bool IsValidPounceTarget(UnitHealth target)
    {
        if (target == null || target.IsDead || target.gameObject == gameObject)
        {
            return false;
        }

        return target.CompareTag(survivorTag);
    }

    private void SpawnPounceWindupRing()
    {
        TemporaryGroundEffect.Spawn(
            transform.position,
            new Color(0.95f, 0.55f, 0.15f, 0.5f),
            Mathf.Max(0.1f, pounceWindup),
            0.9f,
            null,
            movementDebugLogs);
    }

    private void SpawnPounceLandingShockwave(Vector3 center)
    {
        PredatorAbilityFeelVfx.SpawnShockwaveRing(
            center,
            pounceLandingRadius,
            new Color(1f, 0.4f, 0.1f, 0.55f),
            0.45f);

        TemporaryGroundEffect.Spawn(
            center,
            new Color(0.85f, 0.25f, 0.1f, 0.45f),
            0.5f,
            pounceLandingRadius,
            null,
            false);
    }

    private void TrackHeavyLanding()
    {
        bool grounded = IsGroundedForJump();
        if (grounded && !wasGroundedLastFrame && verticalVelocity < -heavyFallVelocityThreshold)
        {
            SpawnHeavyLandingDust();
        }

        wasGroundedLastFrame = grounded;
    }

    private void SpawnHeavyLandingDust()
    {
        Vector3 center = transform.position;
        PredatorAbilityFeelVfx.SpawnShockwaveRing(
            center,
            landingDustRadius,
            new Color(0.75f, 0.7f, 0.65f, 0.35f),
            0.35f);

        UnitHealth[] units = FindObjectsByType<UnitHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            UnitHealth target = units[i];
            if (!IsValidPounceTarget(target))
            {
                continue;
            }

            Vector3 offset = target.transform.position - center;
            offset.y = 0f;
            if (offset.sqrMagnitude > landingDustRadius * landingDustRadius)
            {
                continue;
            }

            ApplyLightKnockback(target, offset.normalized, landingDustKnockback);
        }
    }

    private void UpdateMoveAnimation(float speed)
    {
        UnitAnimationHelper.TrySetAnimatorFloat(animator, UnitAnimationHelper.Predator.MoveSpeed, speed);
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

        Vector3 start = transform.position;
        motion = PlayableBoundsHelper.ConstrainHorizontalMotion(start, motion);
        CollisionFlags flags = characterController.Move(motion);

        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }

        PlayableBoundsHelper.ClampUnitIfOutside(transform, characterController, "predator-move");
        return true;
    }

    public bool TryJump()
    {
        if (!enablePredatorJump || !CanUseCharacterController() || isPouncing)
        {
            return false;
        }

        if (Time.time - lastJumpTime < jumpCooldown)
        {
            return false;
        }

        int allowedJumps = Mathf.Max(1, predatorMaxJumps);

        bool grounded = IsGroundedForJump();
        if (grounded)
        {
            jumpsUsed = 0;
        }

        if (jumpsUsed >= allowedJumps)
        {
            if (movementDebugLogs)
            {
                Debug.Log("[Movement] Predator jump blocked: not enough jumps");
            }

            return false;
        }

        Vector3 predicted = transform.position + Vector3.up * predatorJumpHeight;
        if (!PlayableBoundsHelper.IsPositionInsidePlayableBounds(predicted))
        {
            if (movementDebugLogs)
            {
                Debug.Log("[Movement] Predator jump blocked: out of bounds");
            }

            return false;
        }

        jumpsUsed++;
        lastJumpTime = Time.time;
        verticalVelocity = Mathf.Sqrt(predatorJumpHeight * -2f * gravity);

        if (movementDebugLogs)
        {
            Debug.Log("[Movement] Predator jump " + jumpsUsed + "/" + allowedJumps);
        }

        return true;
    }

    private bool IsGroundedForJump()
    {
        if (characterController != null && characterController.isGrounded)
        {
            return true;
        }

        float radius = characterController != null ? characterController.radius * 0.95f : 0.35f;
        Vector3 origin = transform.position + Vector3.up * 0.05f;
        return Physics.SphereCast(origin, radius, Vector3.down, out _, groundedCheckDistance + 0.05f, ~0, QueryTriggerInteraction.Ignore);
    }

    private bool WasKeyPressedThisFrame(Key key)
    {
        if (Keyboard.current == null || key == Key.None)
        {
            return false;
        }

        return Keyboard.current[key].wasPressedThisFrame;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y > 0.5f && verticalVelocity < 0f)
        {
            jumpsUsed = 0;
        }

        if (hit.normal.y < -0.5f && verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }
    }

    private void ApplyGravity()
    {
        if (IsGroundedForJump() && verticalVelocity < 0f)
        {
            verticalVelocity = -1f;
            jumpsUsed = 0;
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

    private void OnDisable()
    {
        if (pounceRoutine != null)
        {
            StopCoroutine(pounceRoutine);
            pounceRoutine = null;
        }

        isPouncing = false;
    }
}
