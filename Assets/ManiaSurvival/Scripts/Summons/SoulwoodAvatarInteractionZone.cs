using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
public class SoulwoodAvatarInteractionZone : MonoBehaviour
{
    [Header("References")]
    public SoulwoodAvatarController avatarController;
    public SoulwoodAvatarUIBridge uiBridge;

    private void Awake()
    {
        Debug.Log("Soulwood interaction zone awake");

        if (avatarController == null)
        {
            avatarController = GetComponentInParent<SoulwoodAvatarController>();
        }

        if (avatarController == null)
        {
            Debug.LogError("SoulwoodAvatarInteractionZone: missing parent SoulwoodAvatarController");
        }

        if (uiBridge == null)
        {
            uiBridge = FindFirstObjectByType<SoulwoodAvatarUIBridge>();
        }

        if (uiBridge == null)
        {
            Debug.LogError("SoulwoodAvatarInteractionZone: missing SoulwoodAvatarUIBridge in scene");
        }

        SphereCollider triggerCollider = GetComponent<SphereCollider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Soulwood enter trigger hit: " + (other != null ? other.name : "null") + ", tag: " + (other != null ? other.tag : "null"));

        if (!IsSurvivor(other))
        {
            return;
        }

        if (avatarController == null)
        {
            avatarController = GetComponentInParent<SoulwoodAvatarController>();
        }

        if (avatarController == null)
        {
            Debug.LogError("SoulwoodAvatarInteractionZone: no avatarController found on enter");
            return;
        }

        if (uiBridge == null)
        {
            uiBridge = FindFirstObjectByType<SoulwoodAvatarUIBridge>();
        }

        if (uiBridge == null)
        {
            Debug.LogError("SoulwoodAvatarInteractionZone: no uiBridge found on enter");
            return;
        }

        Debug.Log("Survivor near Soulwood Avatar");
        uiBridge.SetNearbyAvatar(avatarController);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Soulwood exit trigger hit: " + (other != null ? other.name : "null") + ", tag: " + (other != null ? other.tag : "null"));

        if (!IsSurvivor(other))
        {
            return;
        }

        if (avatarController == null)
        {
            avatarController = GetComponentInParent<SoulwoodAvatarController>();
        }

        if (avatarController == null)
        {
            Debug.LogError("SoulwoodAvatarInteractionZone: no avatarController found on exit");
            return;
        }

        if (uiBridge == null)
        {
            uiBridge = FindFirstObjectByType<SoulwoodAvatarUIBridge>();
        }

        if (uiBridge == null)
        {
            Debug.LogError("SoulwoodAvatarInteractionZone: no uiBridge found on exit");
            return;
        }

        Debug.Log("Survivor left Soulwood Avatar");
        uiBridge.ClearNearbyAvatar(avatarController);
    }

    private bool IsSurvivor(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.CompareTag("Survivor"))
        {
            return true;
        }

        if (other.GetComponentInParent<SurvivorSoulwoodAvatarAbility>() != null)
        {
            return true;
        }

        UnitHealth health = other.GetComponentInParent<UnitHealth>();
        if (health != null && health.CompareTag("Survivor"))
        {
            return true;
        }

        return false;
    }
}
