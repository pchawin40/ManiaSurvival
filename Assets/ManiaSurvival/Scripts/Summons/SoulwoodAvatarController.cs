using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(UnitHealth))]
[RequireComponent(typeof(CharacterController))]
public class SoulwoodAvatarController : MonoBehaviour
{
    public enum AvatarControlMode
    {
        IdleGuard,
        PlayerControlled
    }

    [Header("Avatar")]
    public float moveSpeed = 2f;
    public float attackRange = 1.8f;
    public int attackDamage = 3;
    public float attackCooldown = 2f;
    public float lifetime = 14f;

    [Header("Input")]
    public bool keyboardInputEnabled = true;
    public Transform cameraTransform;
    public bool useCameraRelativeMovement = true;

    public AvatarControlMode controlMode = AvatarControlMode.IdleGuard;

    private UnitHealth unitHealth;
    private CharacterController characterController;
    private SurvivorSoulwoodAvatarAbility ownerAbility;
    private Transform ownerSurvivor;
    private Vector2 mobileMoveInput;
    private UnitHealth target;
    private float nextAttackTime;
    private float lifeTimer;
    private bool hasFinished;

    public bool IsAlive => unitHealth != null && !unitHealth.IsDead;
    public bool IsPlayerControlled => controlMode == AvatarControlMode.PlayerControlled;

    private void Awake()
    {
        unitHealth = GetComponent<UnitHealth>();
        characterController = GetComponent<CharacterController>();
        lifeTimer = lifetime;
    }

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (hasFinished)
        {
            return;
        }

        if (unitHealth == null)
        {
            unitHealth = GetComponent<UnitHealth>();
        }

        if (unitHealth == null || unitHealth.IsDead)
        {
            FinishAvatarCompletely();
            return;
        }

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            FinishAvatarCompletely();
            return;
        }

        if (!IsValidTarget(target))
        {
            target = FindMonsterTarget(false);
        }

        if (IsPlayerControlled)
        {
            Vector2 input = GetMoveInput();
            Vector3 moveDirection = BuildWorldDirection(input);
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                characterController.Move(moveDirection.normalized * moveSpeed * Time.deltaTime);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(moveDirection.normalized), 720f * Time.deltaTime);
            }
        }
        else
        {
            AttemptAutoAttack();
        }
    }

    public void Initialize(Transform survivorOwner, SurvivorSoulwoodAvatarAbility ability, float avatarLifetime)
    {
        ownerSurvivor = survivorOwner;
        ownerAbility = ability;
        lifetime = avatarLifetime;
        lifeTimer = lifetime;
        SetPlayerControlled(false);
        target = null;
    }

    public void SetMoveInput(Vector2 input)
    {
        mobileMoveInput = Vector2.ClampMagnitude(input, 1f);
    }

    public void SetPlayerControlled(bool isControlled)
    {
        controlMode = isControlled ? AvatarControlMode.PlayerControlled : AvatarControlMode.IdleGuard;

        if (!isControlled)
        {
            mobileMoveInput = Vector2.zero;
        }
    }

    public void TryAttack()
    {
        AttemptAttack(true);
    }

    public void EjectPlayerOnly()
    {
        if (hasFinished)
        {
            return;
        }

        SetPlayerControlled(false);

        if (ownerAbility != null)
        {
            ownerAbility.ExitAvatarButKeepAvatarAlive(this);
        }

    }

    public void Eject()
    {
        EjectPlayerOnly();
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

    private void AttemptAutoAttack()
    {
        AttemptAttack(false);
    }

    private void AttemptAttack(bool logWhenNoTarget)
    {
        if (hasFinished)
        {
            return;
        }

        if (unitHealth == null || unitHealth.IsDead)
        {
            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        if (!IsValidTarget(target))
        {
            target = FindMonsterTarget(logWhenNoTarget);
        }

        if (target == null)
        {
            if (logWhenNoTarget)
            {
                Debug.Log("no valid target found");
            }

            return;
        }

        float targetDistance = GetTargetDistance(target);
        if (targetDistance > attackRange)
        {
            return;
        }

        int healthBefore = target.currentHealth;
        nextAttackTime = Time.time + attackCooldown;
        target.TakeDamage(attackDamage, gameObject);
        Debug.Log("attacking target: hp " + healthBefore + " -> " + target.currentHealth);
    }

    private UnitHealth FindMonsterTarget(bool logWhenNoTarget)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 25f, ~0, QueryTriggerInteraction.Ignore);
        UnitHealth nearest = null;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth health = hits[i].GetComponentInParent<UnitHealth>();
            if (health == null)
            {
                if (logWhenNoTarget)
                {
                    Debug.Log("target missing UnitHealth");
                }
                continue;
            }

            if (health.IsDead || !IsMonsterTarget(health))
            {
                continue;
            }

            if (ownerSurvivor != null && health.transform == ownerSurvivor)
            {
                continue;
            }

            float distanceSqr = (health.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr < nearestDistanceSqr)
            {
                nearest = health;
                nearestDistanceSqr = distanceSqr;
            }
        }

        if (nearest != null && logWhenNoTarget)
        {
            Debug.Log("target found: " + nearest.name);
        }
        else if (logWhenNoTarget)
        {
            Debug.Log("no valid target found");
        }

        return nearest;
    }

    private bool IsValidTarget(UnitHealth health)
    {
        return health != null && !health.IsDead && IsMonsterTarget(health);
    }

    private bool IsMonsterTarget(UnitHealth health)
    {
        return health != null && (health.CompareTag("Monster") || health.CompareTag("Predator"));
    }

    private float GetTargetDistance(UnitHealth health)
    {
        if (health == null)
        {
            return float.MaxValue;
        }

        Collider targetCollider = health.GetComponentInChildren<Collider>();
        if (targetCollider == null)
        {
            targetCollider = health.GetComponent<Collider>();
        }

        Vector3 avatarPosition = transform.position;
        avatarPosition.y = 0f;

        if (targetCollider != null)
        {
            Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);
            closestPoint.y = 0f;
            return Vector3.Distance(avatarPosition, closestPoint);
        }

        Vector3 targetPosition = health.transform.position;
        targetPosition.y = 0f;
        return Vector3.Distance(avatarPosition, targetPosition);
    }

    private void FinishAvatarCompletely()
    {
        if (hasFinished)
        {
            return;
        }

        hasFinished = true;

        ownerAbility?.OnAvatarEndedCompletely(this, transform.position);

        Destroy(gameObject);
    }
}
