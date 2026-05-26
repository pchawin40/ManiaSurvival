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
        if (avatarController == null)
        {
            avatarController = GetComponentInParent<SoulwoodAvatarController>();
        }

        if (uiBridge == null)
        {
            uiBridge = FindFirstObjectByType<SoulwoodAvatarUIBridge>();
        }

        SphereCollider triggerCollider = GetComponent<SphereCollider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        NotifyNearbyAvatar(other);
    }

    private void OnTriggerStay(Collider other)
    {
        NotifyNearbyAvatar(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsSurvivorCollider(other))
        {
            return;
        }

        if (uiBridge != null && avatarController != null)
        {
            uiBridge.ClearNearbyAvatar(avatarController);
        }
    }

    private void OnDisable()
    {
        ClearNearbyAvatar();
    }

    private void OnDestroy()
    {
        ClearNearbyAvatar();
    }

    private void NotifyNearbyAvatar(Collider other)
    {
        if (!IsSurvivorCollider(other))
        {
            return;
        }

        if (avatarController == null || !avatarController.IsAlive)
        {
            ClearNearbyAvatar();
            return;
        }

        if (uiBridge != null)
        {
            uiBridge.SetNearbyAvatar(avatarController);
        }
    }

    private bool IsSurvivorCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        UnitHealth health = other.GetComponentInParent<UnitHealth>();
        if (health == null || !health.CompareTag("Survivor"))
        {
            return false;
        }

        return !health.IsDead;
    }

    private void ClearNearbyAvatar()
    {
        if (uiBridge != null && avatarController != null)
        {
            uiBridge.ClearNearbyAvatar(avatarController);
        }
    }
}
