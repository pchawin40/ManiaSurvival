using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives Pick Up and Drop button visibility/interactability based on
/// what CarriedItemHolder is currently holding and what is nearby.
///
/// Wire button OnClick in the Inspector:
///   pickupButton OnClick -> holder.TryPickupNearbyItem()
///   dropButton   OnClick -> holder.DropCurrentItemFromButton()
/// </summary>
public class ItemActionButtonUI : MonoBehaviour
{
    [Header("Holder")]
    [Tooltip("The CarriedItemHolder to watch. Auto-found if left empty.")]
    public CarriedItemHolder holder;

    [Header("Pick Up Button")]
    public Button pickupButton;
    public TMP_Text pickupButtonText;
    [Tooltip("If true, the Pick Up button is hidden when no nearby item exists. If false, it stays visible but is greyed out.")]
    public bool hidePickupWhenUnavailable = true;

    [Header("Drop Button")]
    public Button dropButton;
    public TMP_Text dropButtonText;
    [Tooltip("If true, the Drop button is hidden when nothing is held. If false, it stays visible but is greyed out.")]
    public bool hideDropWhenUnavailable = false;

    private void Start()
    {
        if (holder == null)
        {
            holder = FindFirstObjectByType<CarriedItemHolder>();
        }

        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        RefreshPickupButton();
        RefreshDropButton();
    }

    private void RefreshPickupButton()
    {
        if (pickupButton == null)
        {
            return;
        }

        // Pickup is only available when nearby item exists AND holder is not already holding one.
        bool canPickup = holder != null && holder.HasNearbyItem && !holder.HasItem;

        if (hidePickupWhenUnavailable)
        {
            pickupButton.gameObject.SetActive(canPickup);
        }
        else
        {
            pickupButton.gameObject.SetActive(true);
            pickupButton.interactable = canPickup;
        }

        if (pickupButtonText != null)
        {
            string nearbyName = holder != null ? holder.NearbyItemName : "";
            pickupButtonText.text = !string.IsNullOrEmpty(nearbyName)
                ? "Pick Up: " + nearbyName
                : "Pick Up";
        }
    }

    private void RefreshDropButton()
    {
        if (dropButton == null)
        {
            return;
        }

        bool canDrop = holder != null && holder.HasItem;

        if (hideDropWhenUnavailable)
        {
            dropButton.gameObject.SetActive(canDrop);
        }
        else
        {
            dropButton.gameObject.SetActive(true);
            dropButton.interactable = canDrop;
        }

        if (dropButtonText != null)
        {
            string heldName = holder != null ? holder.CurrentItemName : "";
            dropButtonText.text = !string.IsNullOrEmpty(heldName)
                ? "Drop: " + heldName
                : "Drop";
        }
    }
}
