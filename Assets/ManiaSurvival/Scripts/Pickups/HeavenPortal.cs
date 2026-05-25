using System.Collections;
using UnityEngine;

public class HeavenPortal : MonoBehaviour
{
    [Header("Portal")]
    public Transform safeZone;
    public Transform returnPoint;

    [Header("Timing")]
    public float safeDuration = 15f;
    public float cooldownDuration = 10f;

    [Header("Safety")]
    public bool disableSurvivorHealth = true;

    private bool isBusy;
    private float nextUseTime;

    private void OnTriggerEnter(Collider other)
    {
        if (isBusy || Time.time < nextUseTime)
        {
            return;
        }

        SurvivorHealth survivorHealth = other.GetComponentInParent<SurvivorHealth>();
        SurvivorMovement survivorMovement = other.GetComponentInParent<SurvivorMovement>();

        if (survivorHealth == null && survivorMovement == null)
        {
            return;
        }

        Transform survivorRoot = survivorHealth != null ? survivorHealth.transform : survivorMovement.transform;
        if (survivorRoot == null || safeZone == null || returnPoint == null)
        {
            return;
        }

        Debug.Log("[HeavenPortal] Entered by " + survivorRoot.name);
        Debug.Log("[HeavenPortal] Safe zone position: " + safeZone.position);
        Debug.Log("[HeavenPortal] Survivor position before teleport: " + survivorRoot.position);

        isBusy = true;
        StartCoroutine(RunPortalSequence(survivorRoot, survivorHealth));
    }

    private IEnumerator RunPortalSequence(Transform survivorRoot, SurvivorHealth survivorHealth)
    {
        CharacterController survivorController = survivorRoot.GetComponent<CharacterController>();

        bool healthWasEnabled = survivorHealth != null && survivorHealth.enabled;
        if (disableSurvivorHealth && survivorHealth != null)
        {
            survivorHealth.enabled = false;
        }

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

        if (disableSurvivorHealth && survivorHealth != null)
        {
            survivorHealth.enabled = healthWasEnabled;
        }

        nextUseTime = Time.time + cooldownDuration;
        isBusy = false;
    }
}
