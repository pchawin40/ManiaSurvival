using System.Collections;
using UnityEngine;

public class HeavenPortal : MonoBehaviour
{
    [Header("Portal")]
    public Transform safeZone;
    public Transform returnPoint;

    [Header("Teleport")]
    [Tooltip("How far above the floor the survivor is placed after teleport.")]
    public float teleportVerticalOffset = 1.1f;

    [Header("Timing")]
    public float safeDuration = 15f;
    public float cooldownDuration = 0f;

    private bool isBusy;
    private float nextUseTime;

    private void OnTriggerEnter(Collider other)
    {
        if (isBusy || (cooldownDuration > 0f && Time.time < nextUseTime))
        {
            return;
        }

        UnitHealth survivorHealth = other.GetComponentInParent<UnitHealth>();

        if (survivorHealth == null || !survivorHealth.CompareTag("Survivor"))
        {
            return;
        }

        SurvivorVisibilityStatus visibilityStatus = survivorHealth.GetComponent<SurvivorVisibilityStatus>();
        if (visibilityStatus == null)
        {
            visibilityStatus = survivorHealth.gameObject.AddComponent<SurvivorVisibilityStatus>();
        }

        Transform survivorRoot = survivorHealth.transform;
        if (survivorRoot == null || safeZone == null || returnPoint == null)
        {
            return;
        }

        Debug.Log("[HeavenPortal] Entered by " + survivorRoot.name);
        Debug.Log("[HeavenPortal] Safe zone position: " + safeZone.position);
        Debug.Log("[HeavenPortal] Survivor position before teleport: " + survivorRoot.position);

        isBusy = true;
        StartCoroutine(RunPortalSequence(survivorRoot, visibilityStatus));
    }

    private IEnumerator RunPortalSequence(Transform survivorRoot, SurvivorVisibilityStatus visibilityStatus)
    {
        CharacterController survivorController = survivorRoot.GetComponent<CharacterController>();
        visibilityStatus.SetHiddenFromMonster(true);

        EnsureHeavenFloorExists();
        TeleportSafely(survivorRoot, safeZone, survivorController, isHeavenDestination: true);

        yield return new WaitForSeconds(safeDuration);

        Debug.Log("[HeavenPortal] Returning survivor to: " + returnPoint.position);
        TeleportSafely(survivorRoot, returnPoint, survivorController, isHeavenDestination: false);

        visibilityStatus.SetHiddenFromMonster(false);

        nextUseTime = cooldownDuration > 0f ? Time.time + cooldownDuration : 0f;
        isBusy = false;
    }

    private void EnsureHeavenFloorExists()
    {
        HeavenFloorCollider floor = FindFirstObjectByType<HeavenFloorCollider>();
        if (floor != null)
        {
            floor.EnsureWalkableFloor();
            return;
        }

        GameObject floorHost = new GameObject("HeavenFloor_Auto");
        floorHost.AddComponent<HeavenFloorCollider>().EnsureWalkableFloor();
    }

    private void TeleportSafely(Transform survivorRoot, Transform destination, CharacterController controller, bool isHeavenDestination)
    {
        Vector3 targetPosition = HeavenFloorCollider.GetSafeStandPosition(destination.position, teleportVerticalOffset);

        if (controller != null)
        {
            controller.enabled = false;
        }

        survivorRoot.SetPositionAndRotation(targetPosition, destination.rotation);

        if (controller != null)
        {
            controller.enabled = true;
        }

        if (isHeavenDestination)
        {
            Debug.Log("[Teleport] Moved player to Heaven at position " + targetPosition);
        }
        else
        {
            Debug.Log("[HeavenPortal] Survivor returned from heaven: " + targetPosition);
        }

        ManiaGameUI gameUi = FindFirstObjectByType<ManiaGameUI>();
        if (gameUi != null)
        {
            gameUi.RefreshActionButtonLayout();
        }
    }

    private void OnDisable()
    {
        isBusy = false;
        nextUseTime = 0f;
    }
}
