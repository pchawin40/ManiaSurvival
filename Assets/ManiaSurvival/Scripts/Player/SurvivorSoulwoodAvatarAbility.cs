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
    public AbilityCooldownButton cooldownButton;
    public CameraFollow cameraFollow;
    public Transform cameraRig;
    public SoulwoodAvatarUIBridge avatarUIBridge;

    [Header("Hide Survivor")]
    public GameObject[] survivorObjectsToHideDuringAvatar;
    public Canvas survivorHealthBarCanvas;
    public Behaviour[] survivorComponentsToDisable;
    public Renderer[] survivorRenderersToHide;
    public Collider[] survivorCollidersToDisable;

    private UnitHealth unitHealth;
    private CharacterController survivorCharacterController;
    private SurvivorVisibilityStatus survivorVisibilityStatus;
    private LocalRoleController localRoleController;
    private SoulwoodAvatarController activeAvatar;
    private Transform previousCameraTarget;
    private float nextCastTime;

    private readonly Dictionary<Behaviour, bool> behaviourStates = new Dictionary<Behaviour, bool>();
    private readonly Dictionary<Renderer, bool> rendererStates = new Dictionary<Renderer, bool>();
    private readonly Dictionary<Collider, bool> colliderStates = new Dictionary<Collider, bool>();
    private readonly Dictionary<CharacterController, bool> characterControllerStates = new Dictionary<CharacterController, bool>();
    private readonly Dictionary<GameObject, bool> gameObjectStates = new Dictionary<GameObject, bool>();

    private void Awake()
    {
        unitHealth = GetComponent<UnitHealth>();
        survivorCharacterController = GetComponent<CharacterController>();
        survivorVisibilityStatus = GetComponent<SurvivorVisibilityStatus>();
        localRoleController = FindFirstObjectByType<LocalRoleController>();

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
        ApplySurvivorHiddenState(true);
        avatarController.Initialize(transform, this, avatarDuration);
        if (avatarUIBridge != null)
        {
            avatarUIBridge.SetActiveAvatar(avatarController);
            avatarUIBridge.gameUI?.ShowAvatarPanel();
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

        nextCastTime = Time.time + cooldown;
        if (cooldownButton != null)
        {
            cooldownButton.StartCooldown(cooldown);
        }
    }

    public void OnAvatarEnded(SoulwoodAvatarController avatarController, Vector3 returnPosition)
    {
        if (activeAvatar != avatarController)
        {
            return;
        }

        if (localRoleController != null)
        {
            localRoleController.ClearSoulwoodAvatarController(avatarController);
        }

        transform.position = returnPosition;
        RestoreCameraTarget();
        if (avatarUIBridge != null)
        {
            avatarUIBridge.ClearActiveAvatar(avatarController);
        }
        ManiaGameUI gameUI = FindFirstObjectByType<ManiaGameUI>();
        if (gameUI != null)
        {
            gameUI.ShowSurvivorPanel();
        }
        ApplySurvivorHiddenState(false);
        activeAvatar = null;
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

                behaviourStates[behaviour] = behaviour.enabled;
                behaviour.enabled = false;
            }
        }
        else
        {
            SurvivorMovement survivorMovement = GetComponent<SurvivorMovement>();
            if (survivorMovement != null)
            {
                behaviourStates[survivorMovement] = survivorMovement.enabled;
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
