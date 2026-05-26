using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitHealth))]
public class SurvivorSoulwoodAvatarAbility : MonoBehaviour
{
    [Header("Soulwood Avatar")]
    public GameObject avatarPrefab;
    public float treeSearchRadius = 6f;
    public float avatarDuration = 14f;
    public float cooldown = 60f;
    public float ejectDistance = 2.25f;
    public float survivorGroundY = 0.15f;
    public float ejectCheckRadius = 0.45f;
    public AbilityCooldownButton cooldownButton;
    public CameraFollow cameraFollow;
    public Transform cameraRig;
    public SoulwoodAvatarUIBridge avatarUIBridge;
    public ManiaGameUI gameUI;

    [Header("Hide Survivor")]
    public GameObject[] survivorObjectsToHideDuringAvatar;
    public Canvas survivorHealthBarCanvas;
    public Behaviour[] survivorComponentsToDisable;
    public Renderer[] survivorRenderersToHide;
    public Collider[] survivorCollidersToDisable;

    private UnitHealth unitHealth;
    private CharacterController survivorCharacterController;
    private SurvivorMovement survivorMovement;
    private SurvivorVisibilityStatus survivorVisibilityStatus;
    private LocalRoleController localRoleController;
    private SoulwoodAvatarController activeAvatar;
    private SoulwoodAvatarController controlledAvatar;
    private Transform previousCameraTarget;
    private float nextCastTime;

    private readonly Dictionary<Behaviour, bool> behaviourStates = new Dictionary<Behaviour, bool>();
    private readonly Dictionary<Renderer, bool> rendererStates = new Dictionary<Renderer, bool>();
    private readonly Dictionary<Collider, bool> colliderStates = new Dictionary<Collider, bool>();
    private readonly Dictionary<CharacterController, bool> characterControllerStates = new Dictionary<CharacterController, bool>();
    private readonly Dictionary<GameObject, bool> gameObjectStates = new Dictionary<GameObject, bool>();
    private bool survivorMovementWasEnabled;

    private void Awake()
    {
        unitHealth = GetComponent<UnitHealth>();
        survivorCharacterController = GetComponent<CharacterController>();
        survivorMovement = GetComponent<SurvivorMovement>();
        survivorVisibilityStatus = GetComponent<SurvivorVisibilityStatus>();
        localRoleController = FindFirstObjectByType<LocalRoleController>();
        gameUI = FindFirstObjectByType<ManiaGameUI>();

        if (cooldownButton == null)
        {
            cooldownButton = FindFirstObjectByType<AbilityCooldownButton>();
        }

        ResolveCameraFollow();
        if (avatarUIBridge == null)
        {
            avatarUIBridge = FindFirstObjectByType<SoulwoodAvatarUIBridge>();
        }
    }

    public void CastSoulwoodAvatar()
    {
        if (cooldownButton != null && cooldownButton.IsCoolingDown)
        {
            Debug.Log("Soulwood Avatar on cooldown");
            return;
        }

        if (unitHealth == null)
        {
            unitHealth = GetComponent<UnitHealth>();
        }

        if (unitHealth == null || unitHealth.IsDead)
        {
            Debug.Log("Soulwood Avatar blocked");
            return;
        }

        if (activeAvatar != null)
        {
            Debug.Log("Soulwood Avatar blocked");
            return;
        }

        if (Time.time < nextCastTime)
        {
            Debug.Log("Soulwood Avatar on cooldown");
            return;
        }

        NeutralTree tree = FindNearestTree();
        if (tree == null)
        {
            Debug.Log("No nearby tree for Soulwood Avatar");
            return;
        }

        if (avatarPrefab == null)
        {
            Debug.Log("Soulwood Avatar blocked");
            return;
        }

        Vector3 spawnPosition = tree.transform.position;
        Quaternion spawnRotation = tree.transform.rotation;

        GameObject avatarObject = Instantiate(avatarPrefab, spawnPosition, spawnRotation);
        SoulwoodAvatarController avatarController = avatarObject.GetComponent<SoulwoodAvatarController>();
        if (avatarController == null)
        {
            Debug.Log("Soulwood Avatar blocked");
            Destroy(avatarObject);
            return;
        }

        if (avatarController.GetComponent<UnitHealth>() == null)
        {
            Debug.Log("Soulwood Avatar blocked");
            Destroy(avatarObject);
            return;
        }

        if (avatarController.GetComponent<CharacterController>() == null)
        {
            Debug.Log("Soulwood Avatar blocked");
            Destroy(avatarObject);
            return;
        }

        Destroy(tree.gameObject);

        activeAvatar = avatarController;
        avatarController.Initialize(transform, this, avatarDuration);
        avatarController.SetPlayerControlled(false);

        nextCastTime = Time.time + cooldown;
        if (cooldownButton != null)
        {
            cooldownButton.StartCooldown(cooldown);
        }
    }

    public void OnAvatarEnded(SoulwoodAvatarController avatarController, Vector3 returnPosition)
    {
        OnAvatarEndedCompletely(avatarController, returnPosition);
    }

    public void EnterAvatar(SoulwoodAvatarController avatarController)
    {
        if (avatarController == null || activeAvatar != avatarController || !avatarController.IsAlive)
        {
            return;
        }

        if (controlledAvatar != null)
        {
            return;
        }

        controlledAvatar = avatarController;
        Debug.Log("UI Enter Avatar Mode");

        ApplySurvivorHiddenState(true);
        avatarController.SetPlayerControlled(true);

        if (avatarUIBridge != null)
        {
            avatarUIBridge.SetActiveAvatar(avatarController);
        }

        if (gameUI == null)
        {
            gameUI = FindFirstObjectByType<ManiaGameUI>();
        }

        if (gameUI != null)
        {
            gameUI.ShowAvatarPanel();
            gameUI.HideEnterSoulwoodButton();
        }

        SetCameraTarget(avatarController.transform);

        if (localRoleController == null)
        {
            localRoleController = FindFirstObjectByType<LocalRoleController>();
        }

        if (localRoleController != null)
        {
            localRoleController.SetSoulwoodAvatarController(avatarController);
        }
    }

    public void ExitAvatarButKeepAvatarAlive(SoulwoodAvatarController avatarController)
    {
        if (avatarController == null || controlledAvatar != avatarController)
        {
            return;
        }

        Debug.Log("Soulwood eject requested");
        Debug.Log("Soulwood Avatar eject: keeping avatar alive");

        if (localRoleController == null)
        {
            localRoleController = FindFirstObjectByType<LocalRoleController>();
        }

        Debug.Log("Restoring Survivor control");
        if (localRoleController != null)
        {
            localRoleController.ClearSoulwoodAvatarController(avatarController);
        }

        Vector3 avatarPosition = avatarController.transform.position;
        Debug.Log("avatar position: " + avatarPosition);

        Vector3 returnPosition = GetSafeEjectPosition(avatarPosition, avatarController);
        Debug.Log("chosen eject position: " + returnPosition);

        RestoreSurvivorAfterEject(returnPosition);

        if (avatarUIBridge != null)
        {
            avatarUIBridge.ClearActiveAvatar(avatarController);
        }

        if (gameUI == null)
        {
            gameUI = FindFirstObjectByType<ManiaGameUI>();
        }

        if (gameUI != null)
        {
            gameUI.ShowSurvivorPanel();
        }

        Debug.Log("UI Exit Avatar Mode");

        avatarController.SetPlayerControlled(false);
        Debug.Log("Avatar switched to idle guard mode");

        controlledAvatar = null;
        Debug.Log("Soulwood eject complete");
    }

    public void OnAvatarEndedCompletely(SoulwoodAvatarController avatarController, Vector3 returnPosition)
    {
        if (avatarController == null || activeAvatar != avatarController)
        {
            return;
        }

        bool wasPossessed = controlledAvatar == avatarController
            || (localRoleController != null && localRoleController.soulwoodAvatarController == avatarController);

        if (wasPossessed)
        {
            if (localRoleController == null)
            {
                localRoleController = FindFirstObjectByType<LocalRoleController>();
            }

            if (localRoleController != null)
            {
                localRoleController.ClearSoulwoodAvatarController(avatarController);
            }

            Vector3 survivorReturnPosition = GetSafeEjectPosition(returnPosition, avatarController);
            RestoreSurvivorAfterEject(survivorReturnPosition);
        }

        if (avatarUIBridge != null)
        {
            avatarUIBridge.ClearNearbyAvatar(avatarController);
            avatarUIBridge.ClearActiveAvatar(avatarController);
        }

        if (gameUI == null)
        {
            gameUI = FindFirstObjectByType<ManiaGameUI>();
        }

        if (gameUI != null)
        {
            gameUI.ShowSurvivorPanel();
        }

        if (wasPossessed)
        {
            Debug.Log("UI Exit Avatar Mode");
        }

        activeAvatar = null;
        controlledAvatar = null;
    }

    private void RestoreSurvivorAfterEject(Vector3 returnPosition)
    {
        Debug.Log("Restoring Survivor before controller enable");
        Debug.Log("Restoring survivor GameObject active: " + gameObject.activeSelf);

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (survivorMovement != null)
        {
            survivorMovement.enabled = false;
        }

        if (survivorCharacterController != null)
        {
            survivorCharacterController.enabled = false;
        }

        transform.position = returnPosition;

        ApplySurvivorHiddenState(false);

        if (localRoleController == null)
        {
            localRoleController = FindFirstObjectByType<LocalRoleController>();
        }

        if (localRoleController != null)
        {
            localRoleController.RestoreSurvivorControl();
        }

        if (survivorCharacterController != null)
        {
            survivorCharacterController.enabled = true;
            Debug.Log("Survivor CharacterController enabled: " + survivorCharacterController.enabled);
        }
        else
        {
            Debug.Log("Survivor CharacterController enabled: false");
        }

        if (survivorMovement != null)
        {
            survivorMovement.enabled = true;
            survivorMovement.SetMoveInput(Vector2.zero);
            survivorMovement.SetSprintHeld(false);
            Debug.Log("SurvivorMovement enabled: " + survivorMovement.enabled);
        }
        else
        {
            Debug.Log("SurvivorMovement enabled: false");
        }

        if (localRoleController != null)
        {
            localRoleController.ApplyControlMode();
        }

        RestoreCameraTarget();
        Debug.Log("Camera restored to Survivor");
        Debug.Log("survivor final position: " + transform.position);
    }

    private Vector3 GetSafeEjectPosition(Vector3 avatarPosition, SoulwoodAvatarController avatarController)
    {
        Vector3 flatForward = Vector3.forward;
        Vector3 flatRight = Vector3.right;

        if (avatarController != null)
        {
            flatForward = avatarController.transform.forward;
            flatRight = avatarController.transform.right;
        }

        flatForward.y = 0f;
        flatRight.y = 0f;

        if (flatForward.sqrMagnitude <= 0.001f)
        {
            flatForward = Vector3.forward;
        }

        if (flatRight.sqrMagnitude <= 0.001f)
        {
            flatRight = Vector3.right;
        }

        Vector3[] directions =
        {
            flatRight,
            -flatRight,
            flatForward,
            -flatForward,
            (flatForward + flatRight).normalized,
            (flatForward - flatRight).normalized,
            (-flatForward + flatRight).normalized,
            (-flatForward - flatRight).normalized
        };

        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 direction = directions[i];
            if (direction.sqrMagnitude <= 0.001f)
            {
                continue;
            }

            Vector3 candidate = avatarPosition + direction.normalized * ejectDistance;
            candidate.y = survivorGroundY;

            if (IsEjectSpotClear(candidate))
            {
                return candidate;
            }
        }

        Vector3 fallback = avatarPosition + flatRight.normalized * ejectDistance;
        fallback.y = survivorGroundY;
        return fallback;
    }

    private bool IsEjectSpotClear(Vector3 candidatePosition)
    {
        Vector3 checkCenter = candidatePosition + Vector3.up * ejectCheckRadius;
        Collider[] hits = Physics.OverlapSphere(checkCenter, ejectCheckRadius, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private NeutralTree FindNearestTree()
    {
        NeutralTree[] trees = FindObjectsByType<NeutralTree>(FindObjectsSortMode.None);
        NeutralTree nearest = null;
        float nearestDistanceSqr = treeSearchRadius * treeSearchRadius;

        for (int i = 0; i < trees.Length; i++)
        {
            NeutralTree tree = trees[i];
            if (tree == null)
            {
                continue;
            }

            float distanceSqr = (tree.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr > nearestDistanceSqr)
            {
                continue;
            }

            nearest = tree;
            nearestDistanceSqr = distanceSqr;
        }

        return nearest;
    }

    private void ApplySurvivorHiddenState(bool hide)
    {
        if (survivorVisibilityStatus != null)
        {
            survivorVisibilityStatus.SetHiddenFromMonster(hide);
        }

        SetSurvivorObjectsActive(!hide);

        if (hide)
        {
            CacheAndDisableComponents();
        }
        else
        {
            RestoreComponents();
        }
    }

    private void CacheAndDisableComponents()
    {
        behaviourStates.Clear();
        rendererStates.Clear();
        colliderStates.Clear();
        characterControllerStates.Clear();
        gameObjectStates.Clear();

        if (survivorComponentsToDisable != null && survivorComponentsToDisable.Length > 0)
        {
            for (int i = 0; i < survivorComponentsToDisable.Length; i++)
            {
                Behaviour behaviour = survivorComponentsToDisable[i];
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour is SurvivorMovement movement)
                {
                    survivorMovement = movement;
                    survivorMovementWasEnabled = movement.enabled;
                    movement.enabled = false;
                    continue;
                }

                behaviourStates[behaviour] = behaviour.enabled;
                behaviour.enabled = false;
            }
        }
        else
        {
            if (survivorMovement != null)
            {
                survivorMovementWasEnabled = survivorMovement.enabled;
                survivorMovement.enabled = false;
            }
        }

        if (survivorRenderersToHide != null && survivorRenderersToHide.Length > 0)
        {
            for (int i = 0; i < survivorRenderersToHide.Length; i++)
            {
                Renderer renderer = survivorRenderersToHide[i];
                if (renderer == null)
                {
                    continue;
                }

                rendererStates[renderer] = renderer.enabled;
                renderer.enabled = false;
            }
        }
        else
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                rendererStates[renderer] = renderer.enabled;
                renderer.enabled = false;
            }
        }

        if (survivorCollidersToDisable != null && survivorCollidersToDisable.Length > 0)
        {
            for (int i = 0; i < survivorCollidersToDisable.Length; i++)
            {
                Collider collider = survivorCollidersToDisable[i];
                if (collider == null)
                {
                    continue;
                }

                colliderStates[collider] = collider.enabled;
                collider.enabled = false;
            }
        }
        else
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                {
                    continue;
                }

                colliderStates[collider] = collider.enabled;
                collider.enabled = false;
            }
        }

        if (survivorCharacterController != null)
        {
            characterControllerStates[survivorCharacterController] = survivorCharacterController.enabled;
            survivorCharacterController.enabled = false;
        }
    }

    private void RestoreComponents()
    {
        foreach (KeyValuePair<Behaviour, bool> pair in behaviourStates)
        {
            if (pair.Key != null)
            {
                pair.Key.enabled = pair.Value;
            }
        }

        foreach (KeyValuePair<Renderer, bool> pair in rendererStates)
        {
            if (pair.Key != null)
            {
                pair.Key.enabled = pair.Value;
            }
        }

        foreach (KeyValuePair<Collider, bool> pair in colliderStates)
        {
            if (pair.Key != null)
            {
                pair.Key.enabled = pair.Value;
            }
        }

        behaviourStates.Clear();
        rendererStates.Clear();
        colliderStates.Clear();

        if (survivorCharacterController != null)
        {
            if (characterControllerStates.TryGetValue(survivorCharacterController, out bool wasEnabled))
            {
                survivorCharacterController.enabled = wasEnabled;
            }
            else
            {
                survivorCharacterController.enabled = true;
            }

            if (survivorCharacterController.enabled)
            {
                Debug.Log("Survivor CharacterController enabled");
            }
        }

        if (survivorMovement != null)
        {
            survivorMovement.enabled = survivorMovementWasEnabled;
            if (survivorMovement.enabled)
            {
                survivorMovement.SetMoveInput(Vector2.zero);
                survivorMovement.SetSprintHeld(false);
                Debug.Log("SurvivorMovement enabled");
            }
        }

        characterControllerStates.Clear();
        gameObjectStates.Clear();
    }

    private void SetSurvivorObjectsActive(bool active)
    {
        if (survivorObjectsToHideDuringAvatar != null)
        {
            for (int i = 0; i < survivorObjectsToHideDuringAvatar.Length; i++)
            {
                GameObject survivorObject = survivorObjectsToHideDuringAvatar[i];
                if (survivorObject != null)
                {
                    if (!gameObjectStates.ContainsKey(survivorObject))
                    {
                        gameObjectStates[survivorObject] = survivorObject.activeSelf;
                    }

                    bool targetState = active ? gameObjectStates[survivorObject] : false;
                    survivorObject.SetActive(targetState);
                }
            }
        }

        if (survivorHealthBarCanvas != null)
        {
            GameObject canvasObject = survivorHealthBarCanvas.gameObject;
            if (!gameObjectStates.ContainsKey(canvasObject))
            {
                gameObjectStates[canvasObject] = canvasObject.activeSelf;
            }

            bool targetState = active ? gameObjectStates[canvasObject] : false;
            canvasObject.SetActive(targetState);
        }
    }

    private void ResolveCameraFollow()
    {
        if (cameraFollow == null && cameraRig != null)
        {
            cameraFollow = cameraRig.GetComponent<CameraFollow>();
        }

        if (cameraFollow == null)
        {
            cameraFollow = FindFirstObjectByType<CameraFollow>();
        }
    }

    private void SetCameraTarget(Transform target)
    {
        ResolveCameraFollow();

        if (cameraFollow == null)
        {
            return;
        }

        if (previousCameraTarget == null)
        {
            previousCameraTarget = cameraFollow.target;
        }

        cameraFollow.target = target;
    }

    private void RestoreCameraTarget()
    {
        ResolveCameraFollow();

        if (cameraFollow == null)
        {
            return;
        }

        if (previousCameraTarget != null)
        {
            cameraFollow.target = previousCameraTarget;
        }
        else if (transform != null)
        {
            cameraFollow.target = transform;
        }

        previousCameraTarget = null;
    }
}
