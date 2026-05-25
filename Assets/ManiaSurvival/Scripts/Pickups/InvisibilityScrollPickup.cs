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

        SurvivorStealthStatus stealthStatus = other.GetComponentInParent<SurvivorStealthStatus>();
        SurvivorMovement survivorMovement = other.GetComponentInParent<SurvivorMovement>();

        if (stealthStatus == null)
        {
            return;
        }

        isCollected = true;

        MonoBehaviour coroutineHost = survivorMovement != null ? (MonoBehaviour)survivorMovement : stealthStatus;
        if (coroutineHost != null)
        {
            coroutineHost.StartCoroutine(ApplyInvisibility(stealthStatus, survivorMovement));
        }

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
        else
        {
            HidePickup();
        }
    }

    private IEnumerator ApplyInvisibility(SurvivorStealthStatus stealthStatus, SurvivorMovement survivorMovement)
    {
        Transform survivorRoot = stealthStatus.transform;

        stealthStatus.SetInvisible(true);

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

        stealthStatus.SetInvisible(false);

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
