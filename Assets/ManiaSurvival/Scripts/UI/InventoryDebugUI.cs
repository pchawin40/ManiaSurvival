using TMPro;
using UnityEngine;

/// <summary>
/// Simple inventory display. Assign a TMP_Text and a CarriedItemHolder.
/// Works without any references assigned — the game still runs normally.
/// </summary>
public class InventoryDebugUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The text element that shows inventory state.")]
    public TMP_Text inventoryText;
    [Tooltip("Optional separate text for the pickup prompt. If empty, prompt is appended to inventoryText.")]
    public TMP_Text pickupPromptText;
    [Tooltip("The holder to watch. Usually the local Survivor.")]
    public CarriedItemHolder holder;

    [Header("Labels")]
    public string emptyLabel = "Inventory: Empty";
    public string bootLabel = "Inventory: Speed Boots";
    public string nearbyPrompt = "Pick Up: Speed Boots  [E]";

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
        if (holder == null)
        {
            SetInventoryText(emptyLabel);
            SetPromptText("");
            return;
        }

        SetInventoryText(holder.HasItem ? bootLabel : emptyLabel);

        bool showPrompt = !holder.HasItem && holder.HasNearbyItem;

        if (pickupPromptText != null)
        {
            // Separate prompt field: show prompt there, inventory field stays clean.
            SetPromptText(showPrompt ? nearbyPrompt : "");
        }
        else if (inventoryText != null)
        {
            // No separate prompt field: append prompt to inventory line.
            string inv = holder.HasItem ? bootLabel : emptyLabel;
            inventoryText.text = showPrompt ? inv + "\n" + nearbyPrompt : inv;
        }
    }

    private void SetInventoryText(string text)
    {
        if (inventoryText == null)
        {
            return;
        }

        inventoryText.text = text;
    }

    private void SetPromptText(string text)
    {
        if (pickupPromptText == null)
        {
            return;
        }

        pickupPromptText.text = text;
    }
}
