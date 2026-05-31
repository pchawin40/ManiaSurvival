using System;
using UnityEngine;

[Serializable]
public class PredatorClassDetail
{
    public PredatorClass classId = PredatorClass.RelentlessHook;
    public string displayName = "Predator";
    public string shortRole = "Hunter";
    [TextArea(1, 2)] public string tagline = "";
    public string difficulty = "Normal";
    public Color themeColor = new Color(0.85f, 0.35f, 0.2f, 1f);
    public string[] abilityNames = new string[4];
    public string[] abilityShortDescriptions = new string[4];
    public Sprite portraitIcon;

    public bool HasPortrait => portraitIcon != null;

    public string GetAbilityName(int slotIndex)
    {
        int index = Mathf.Clamp(slotIndex, 0, 3);
        if (abilityNames != null && index < abilityNames.Length && !string.IsNullOrWhiteSpace(abilityNames[index]))
        {
            return abilityNames[index];
        }

        return "Slot " + (index + 1);
    }

    public string GetAbilityShortDescription(int slotIndex)
    {
        int index = Mathf.Clamp(slotIndex, 0, 3);
        if (abilityShortDescriptions != null && index < abilityShortDescriptions.Length)
        {
            return abilityShortDescriptions[index] ?? string.Empty;
        }

        return string.Empty;
    }
}
