using System.Collections;
using UnityEngine;

public class SpeedBootsPickup : MonoBehaviour
{
    [Header("Boost")]
    public float duration = 8f;
    public float speedMultiplier = 1.5f;

    [Header("Pickup")]
    public bool destroyOnCollect = true;

    private bool isCollected;

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected)
        {
            return;
        }

        SurvivorMovement survivorMovement = other.GetComponentInParent<SurvivorMovement>();
        if (survivorMovement == null)
        {
            return;
        }

        isCollected = true;
        survivorMovement.StartCoroutine(ApplySpeedBoost(survivorMovement));

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
        else
        {
            HidePickup();
        }
    }

    private IEnumerator ApplySpeedBoost(SurvivorMovement survivorMovement)
    {
        float originalWalkSpeed = survivorMovement.walkSpeed;
        float originalSprintSpeed = survivorMovement.sprintSpeed;

        survivorMovement.walkSpeed = originalWalkSpeed * speedMultiplier;
        survivorMovement.sprintSpeed = originalSprintSpeed * speedMultiplier;

        yield return new WaitForSeconds(duration);

        if (survivorMovement != null)
        {
            survivorMovement.walkSpeed = originalWalkSpeed;
            survivorMovement.sprintSpeed = originalSprintSpeed;
        }
    }

    private void HidePickup()
    {
        Collider pickupCollider = GetComponent<Collider>();
        if (pickupCollider != null)
        {
            pickupCollider.enabled = false;
        }

        Renderer[] pickupRenderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < pickupRenderers.Length; i++)
        {
            pickupRenderers[i].enabled = false;
        }
    }
}
