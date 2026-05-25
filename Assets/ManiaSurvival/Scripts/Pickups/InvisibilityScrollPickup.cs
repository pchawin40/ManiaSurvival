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

        SurvivorHealth survivorHealth = other.GetComponentInParent<SurvivorHealth>();
        SurvivorMovement survivorMovement = other.GetComponentInParent<SurvivorMovement>();

        if (survivorHealth == null && survivorMovement == null)
        {
            return;
        }

        isCollected = true;

        MonoBehaviour coroutineHost = survivorMovement != null ? (MonoBehaviour)survivorMovement : survivorHealth;
        if (coroutineHost != null)
        {
            coroutineHost.StartCoroutine(ApplyInvisibility(survivorHealth, survivorMovement));
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

    private IEnumerator ApplyInvisibility(SurvivorHealth survivorHealth, SurvivorMovement survivorMovement)
    {
        bool healthWasEnabled = survivorHealth != null && survivorHealth.enabled;
        Renderer[] renderers = survivorMovement != null
            ? survivorMovement.GetComponentsInChildren<Renderer>(true)
            : survivorHealth.GetComponentsInChildren<Renderer>(true);
        bool[] rendererStates = new bool[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            rendererStates[i] = renderers[i] != null && renderers[i].enabled;
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }

        if (survivorHealth != null)
        {
            survivorHealth.enabled = false;
        }

        yield return new WaitForSeconds(duration);

        if (survivorHealth != null)
        {
            survivorHealth.enabled = healthWasEnabled;
        }

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
