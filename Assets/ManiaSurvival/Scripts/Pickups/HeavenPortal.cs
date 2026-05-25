using System.Collections;
using UnityEngine;

public class HeavenPortal : MonoBehaviour
{
    [Header("Portal")]
    public Transform safeZone;
    public Transform returnPoint;

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

        if (survivorController != null)
        {
            survivorController.enabled = false;
        }

        survivorRoot.position = safeZone.position;
        survivorRoot.rotation = safeZone.rotation;

        if (survivorController != null)
        {
            survivorController.enabled = true;
        }

        Debug.Log("[HeavenPortal] Survivor teleported to safe zone: " + survivorRoot.position);

        yield return new WaitForSeconds(safeDuration);

        Debug.Log("[HeavenPortal] Returning survivor to: " + returnPoint.position);

        if (survivorController != null)
        {
            survivorController.enabled = false;
        }

        survivorRoot.position = returnPoint.position;
        survivorRoot.rotation = returnPoint.rotation;

        if (survivorController != null)
        {
            survivorController.enabled = true;
        }

        Debug.Log("[HeavenPortal] Survivor returned from heaven: " + survivorRoot.position);
        visibilityStatus.SetHiddenFromMonster(false);

        nextUseTime = cooldownDuration > 0f ? Time.time + cooldownDuration : 0f;
        isBusy = false;
    }

    private void OnDisable()
    {
        isBusy = false;
        nextUseTime = 0f;
    }
}
