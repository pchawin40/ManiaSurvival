using System.Collections;
using UnityEngine;

public class InvisibilityScrollPickup : MonoBehaviour
{
    [Header("Invisibility")]
    public float duration = 8f;

    [Header("Pickup")]
    public bool destroyOnCollect = true;

    private bool isCollected;

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected)
        {
            return;
        }

        UnitHealth survivorHealth = other.GetComponentInParent<UnitHealth>();
        SurvivorMovement survivorMovement = other.GetComponentInParent<SurvivorMovement>();

        if (survivorHealth == null || !survivorHealth.CompareTag("Survivor"))
        {
            return;
        }

        SurvivorVisibilityStatus visibilityStatus = survivorHealth.GetComponent<SurvivorVisibilityStatus>();
        if (visibilityStatus == null)
        {
            visibilityStatus = survivorHealth.gameObject.AddComponent<SurvivorVisibilityStatus>();
        }

        isCollected = true;

        MonoBehaviour coroutineHost = survivorMovement != null ? (MonoBehaviour)survivorMovement : visibilityStatus;
        coroutineHost.StartCoroutine(ApplyInvisibility(visibilityStatus, survivorMovement, survivorHealth.transform));

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
        else
        {
            HidePickup();
        }
    }

    private IEnumerator ApplyInvisibility(SurvivorVisibilityStatus visibilityStatus, SurvivorMovement survivorMovement, Transform survivorRoot)
    {
        visibilityStatus.HideFromMonster(duration);

        Renderer[] renderers = survivorMovement != null
            ? survivorMovement.GetComponentsInChildren<Renderer>(true)
            : survivorRoot.GetComponentsInChildren<Renderer>(true);
        bool[] rendererStates = new bool[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            rendererStates[i] = renderers[i] != null && renderers[i].enabled;
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }

        yield return new WaitForSeconds(duration);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = rendererStates[i];
            }
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
