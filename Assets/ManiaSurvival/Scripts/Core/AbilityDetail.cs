using System;
using UnityEngine;

[System.Serializable]
public class AbilityDetail
{
    private const string PlaceholderName = "Ability";

    [Header("Identity")]
    public string displayName = "";
    [Tooltip("Short label shown on ability buttons.")]
    public string buttonLabel = "";
    [TextArea(1, 3)] public string shortDescription = "";
    [TextArea(1, 3)] public string flavorText = "";
    [Tooltip("Compact tag shown in tooltips, e.g. Heal / Peel, Ultimate / Burst.")]
    public string roleTag = "";
    public Sprite icon;
    public Color themeColor = new Color(0.55f, 0.85f, 1f, 1f);

    [Header("Optional Feel Hooks")]
    [Tooltip("Optional cast sound. Class managers may also have dedicated audio fields.")]
    public AudioClip castSound;
    public AudioClip hitSound;
    public GameObject castVfxPrefab;
    public GameObject hitVfxPrefab;

    public bool HasDisplayName => !string.IsNullOrWhiteSpace(displayName);

    public bool IsConfigured =>
        HasDisplayName && !IsPlaceholderName(displayName);

    public string GetButtonLabelOrDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(buttonLabel) && !IsPlaceholderName(buttonLabel))
        {
            return buttonLabel;
        }

        if (IsConfigured)
        {
            return displayName;
        }

        return string.Empty;
    }

    public static bool IsPlaceholderName(string value)
    {
        return string.Equals(value?.Trim(), PlaceholderName, StringComparison.OrdinalIgnoreCase);
    }

    public static AbilityDetail CreateFallback(int slotNumber, string classLabel = "Unknown")
    {
        return new AbilityDetail
        {
            displayName = classLabel + " Slot " + slotNumber,
            buttonLabel = "Slot " + slotNumber,
            shortDescription = "Ability slot " + slotNumber + ".",
            themeColor = Color.white
        };
    }
}
