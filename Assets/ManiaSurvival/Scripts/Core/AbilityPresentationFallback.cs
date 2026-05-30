using UnityEngine;

/// <summary>
/// Safe UI fallbacks when AbilityController or class detail data is not ready yet.
/// </summary>
public static class AbilityPresentationFallback
{
    public static AbilityDetail ResolveForUi(bool isPredator, int slotNumber, AbilityDetail liveDetail)
    {
        return ResolveForUi(isPredator, PredatorClass.RelentlessHook, slotNumber, liveDetail);
    }

    public static AbilityDetail ResolveForUi(
        bool isPredator,
        PredatorClass predatorClass,
        int slotNumber,
        AbilityDetail liveDetail)
    {
        if (liveDetail.IsConfigured)
        {
            return liveDetail;
        }

        if (!isPredator)
        {
            return GetMedicDetail(slotNumber);
        }

        switch (predatorClass)
        {
            case PredatorClass.SwarmOverlord:
                return GetSwarmOverlordDetail(slotNumber);
            case PredatorClass.Juggernaut:
                return GetJuggernautDetail(slotNumber);
            default:
                return GetRelentlessHookDetail(slotNumber);
        }
    }

    public static string GetShortLabel(bool isPredator, int slotNumber, AbilityDetail liveDetail)
    {
        return GetShortLabel(isPredator, PredatorClass.RelentlessHook, slotNumber, liveDetail);
    }

    public static string GetShortLabel(
        bool isPredator,
        PredatorClass predatorClass,
        int slotNumber,
        AbilityDetail liveDetail)
    {
        AbilityDetail detail = ResolveForUi(isPredator, predatorClass, slotNumber, liveDetail);
        string label = detail.GetButtonLabelOrDisplayName();
        if (!string.IsNullOrWhiteSpace(label))
        {
            return label;
        }

        return isPredator
            ? GetPredatorShortLabel(predatorClass, slotNumber)
            : GetMedicShortLabel(slotNumber);
    }

    public static string GetMedicShortLabel(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1: return "Biotic";
            case 2: return "Pulse";
            case 3: return "Tether";
            default: return "Sanct.";
        }
    }

    public static string GetPredatorShortLabel(int slotNumber)
    {
        return GetPredatorShortLabel(PredatorClass.RelentlessHook, slotNumber);
    }

    public static string GetPredatorShortLabel(PredatorClass predatorClass, int slotNumber)
    {
        switch (predatorClass)
        {
            case PredatorClass.SwarmOverlord:
                switch (slotNumber)
                {
                    case 1: return "Spit";
                    case 2: return "Brood";
                    case 3: return "Infest";
                    default: return "Hive";
                }
            case PredatorClass.Juggernaut:
                switch (slotNumber)
                {
                    case 1: return "Flame";
                    case 2: return "Leap";
                    case 3: return "Roar";
                    default: return "Meteor";
                }
            default:
                switch (slotNumber)
                {
                    case 1: return "Spray";
                    case 2: return "Hook";
                    case 3: return "Tonic";
                    default: return "Barrage";
                }
        }
    }

    public static AbilityDetail GetMedicDetail(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1:
                return new AbilityDetail
                {
                    displayName = "Biotic Dart",
                    buttonLabel = "Biotic",
                    shortDescription = "Heal an ally or knock the predator back.",
                    roleTag = "Heal / Peel",
                    themeColor = new Color(0.35f, 0.95f, 0.55f, 1f)
                };
            case 2:
                return new AbilityDetail
                {
                    displayName = "Heal Pulse",
                    buttonLabel = "Pulse",
                    shortDescription = "Heal nearby wounded allies.",
                    roleTag = "Area Heal",
                    themeColor = new Color(0.45f, 0.85f, 1f, 1f)
                };
            case 3:
                return new AbilityDetail
                {
                    displayName = "Tether / Blink",
                    buttonLabel = "Tether",
                    shortDescription = "Dash to an ally. No ally: blink forward.",
                    roleTag = "Mobility",
                    themeColor = new Color(0.72f, 0.62f, 1f, 1f)
                };
            default:
                return new AbilityDetail
                {
                    displayName = "Sanctuary",
                    buttonLabel = "Sanct.",
                    shortDescription = "Create a healing safe zone.",
                    roleTag = "Ultimate / Zone",
                    themeColor = new Color(1f, 0.82f, 0.35f, 1f)
                };
        }
    }

    public static AbilityDetail GetSwarmOverlordDetail(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1:
                return new AbilityDetail
                {
                    displayName = "Acid Spit",
                    buttonLabel = "Spit",
                    shortDescription = "Cone spit that damages and can slow survivors.",
                    roleTag = "Cone / Poke",
                    themeColor = new Color(0.45f, 0.95f, 0.3f, 1f)
                };
            case 2:
                return new AbilityDetail
                {
                    displayName = "Brood Spawn",
                    buttonLabel = "Brood",
                    shortDescription = "Spawn broodlings that chase and chip survivors.",
                    roleTag = "Summon",
                    themeColor = new Color(0.55f, 0.85f, 0.35f, 1f)
                };
            case 3:
                return new AbilityDetail
                {
                    displayName = "Infest",
                    buttonLabel = "Infest",
                    shortDescription = "Drop a toxic zone that damages and slows.",
                    roleTag = "Zone / Control",
                    themeColor = new Color(0.35f, 0.8f, 0.25f, 1f)
                };
            default:
                return new AbilityDetail
                {
                    displayName = "Hive Call",
                    buttonLabel = "Hive",
                    shortDescription = "Warn, pulse damage, then summon broodlings.",
                    roleTag = "Ultimate / Zone",
                    themeColor = new Color(0.65f, 1f, 0.25f, 1f)
                };
        }
    }

    public static AbilityDetail GetJuggernautDetail(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1:
                return new AbilityDetail
                {
                    displayName = "Flame Breath",
                    buttonLabel = "Flame",
                    shortDescription = "Cone fire that burns survivors in front of you.",
                    roleTag = "Cone / Damage",
                    themeColor = new Color(1f, 0.45f, 0.15f, 1f)
                };
            case 2:
                return new AbilityDetail
                {
                    displayName = "Dragon Leap",
                    buttonLabel = "Leap",
                    shortDescription = "Leap forward and slam the landing zone.",
                    roleTag = "Mobility / Burst",
                    themeColor = new Color(1f, 0.55f, 0.2f, 1f)
                };
            case 3:
                return new AbilityDetail
                {
                    displayName = "Dragon Roar",
                    buttonLabel = "Roar",
                    shortDescription = "Knock survivors back and slow them.",
                    roleTag = "AoE / Control",
                    themeColor = new Color(1f, 0.65f, 0.25f, 1f)
                };
            default:
                return new AbilityDetail
                {
                    displayName = "Meteor",
                    buttonLabel = "Meteor",
                    shortDescription = "Warn, then crash down with fire aftermath.",
                    roleTag = "Ultimate / Zone",
                    themeColor = new Color(1f, 0.35f, 0.1f, 1f)
                };
        }
    }

    public static AbilityDetail GetRelentlessHookDetail(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1:
                return new AbilityDetail
                {
                    displayName = "Razor Spray",
                    buttonLabel = "Spray",
                    shortDescription = "Fire a short cone blast that damages and knocks survivors back.",
                    roleTag = "Cone / Poke",
                    themeColor = new Color(1f, 0.45f, 0.25f, 1f)
                };
            case 2:
                return new AbilityDetail
                {
                    displayName = "Harpoon Hook",
                    buttonLabel = "Hook",
                    shortDescription = "Pull a survivor out of position.",
                    roleTag = "Catch / Control",
                    themeColor = new Color(0.85f, 0.55f, 0.25f, 1f)
                };
            case 3:
                return new AbilityDetail
                {
                    displayName = "Toxic Tonic",
                    buttonLabel = "Tonic",
                    shortDescription = "Heal yourself and release a slowing danger cloud.",
                    roleTag = "Self Heal / Zone",
                    themeColor = new Color(0.35f, 0.95f, 0.45f, 1f)
                };
            default:
                return new AbilityDetail
                {
                    displayName = "Barrage",
                    buttonLabel = "Barrage",
                    shortDescription = "Warn, then fire repeated cone blasts in front of you.",
                    roleTag = "Ultimate / Burst",
                    themeColor = new Color(1f, 0.3f, 0.2f, 1f)
                };
        }
    }
}
