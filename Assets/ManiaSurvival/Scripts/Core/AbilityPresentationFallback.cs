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
            case PredatorClass.ShadowStalker:
                return GetShadowStalkerDetail(slotNumber);
            case PredatorClass.IronColossus:
                return GetIronColossusDetail(slotNumber);
            case PredatorClass.PlagueGardener:
                return GetPlagueGardenerDetail(slotNumber);
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
            case PredatorClass.ShadowStalker:
                switch (slotNumber)
                {
                    case 1: return "Slash";
                    case 2: return "Vanish";
                    case 3: return "Mark";
                    default: return "Night.";
                }
            case PredatorClass.IronColossus:
                switch (slotNumber)
                {
                    case 1: return "Crush";
                    case 2: return "Guard";
                    case 3: return "Quake";
                    default: return "Fort.";
                }
            case PredatorClass.PlagueGardener:
                switch (slotNumber)
                {
                    case 1: return "Thorn";
                    case 2: return "Root";
                    case 3: return "Spore";
                    default: return "Bloom";
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
                    shortDescription = "Heal ally. Blast enemies in front. Strong vs broodlings.",
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
                    displayName = "Infection Spit",
                    buttonLabel = "Spit",
                    shortDescription = "Lob a bio-glob — splat, slow, and leave a corruption puddle.",
                    roleTag = "Lob / Poke",
                    themeColor = new Color(0.45f, 0.95f, 0.3f, 1f)
                };
            case 2:
                return new AbilityDetail
                {
                    displayName = "Brood Hatch",
                    buttonLabel = "Brood",
                    shortDescription = "Hatch four weak broodlings (45 mana, cap 8). Survivors can kill them.",
                    roleTag = "Summon",
                    themeColor = new Color(0.55f, 0.85f, 0.35f, 1f)
                };
            case 3:
                return new AbilityDetail
                {
                    displayName = "Infest",
                    buttonLabel = "Infest",
                    shortDescription = "Plant a seed marker, then bloom a corruption DOT zone.",
                    roleTag = "Zone / Attrition",
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
                    shortDescription = "Roar, mark landing, then leap — heavy but telegraphed.",
                    roleTag = "Mobility / Commit",
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

    public static AbilityDetail GetShadowStalkerDetail(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1:
                return new AbilityDetail
                {
                    displayName = "Shadow Slash",
                    buttonLabel = "Slash",
                    shortDescription = "Quick melee arc that cuts through a tight cone.",
                    roleTag = "Melee / Poke",
                    themeColor = new Color(0.52f, 0.28f, 0.82f, 1f)
                };
            case 2:
                return new AbilityDetail
                {
                    displayName = "Vanish",
                    buttonLabel = "Vanish",
                    shortDescription = "Fade from sight and move faster briefly.",
                    roleTag = "Stealth / Mobility",
                    themeColor = new Color(0.42f, 0.22f, 0.72f, 1f)
                };
            case 3:
                return new AbilityDetail
                {
                    displayName = "Hunter's Mark",
                    buttonLabel = "Mark",
                    shortDescription = "Curse a survivor with a slowing mark.",
                    roleTag = "Pick / Control",
                    themeColor = new Color(0.62f, 0.32f, 0.92f, 1f)
                };
            default:
                return new AbilityDetail
                {
                    displayName = "Nightfall",
                    buttonLabel = "Night.",
                    shortDescription = "Blanket an area in darkness — slow and shred.",
                    roleTag = "Ultimate / Zone",
                    themeColor = new Color(0.35f, 0.15f, 0.55f, 1f)
                };
        }
    }

    public static AbilityDetail GetIronColossusDetail(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1:
                return new AbilityDetail
                {
                    displayName = "Iron Crush",
                    buttonLabel = "Crush",
                    shortDescription = "Heavy slam that damages survivors nearby.",
                    roleTag = "Melee / AoE",
                    themeColor = new Color(0.55f, 0.62f, 0.78f, 1f)
                };
            case 2:
                return new AbilityDetail
                {
                    displayName = "Iron Guard",
                    buttonLabel = "Guard",
                    shortDescription = "Brace yourself with brief damage reduction.",
                    roleTag = "Self / Defense",
                    themeColor = new Color(0.48f, 0.55f, 0.72f, 1f)
                };
            case 3:
                return new AbilityDetail
                {
                    displayName = "Seismic Quake",
                    buttonLabel = "Quake",
                    shortDescription = "Ground slam that knocks survivors away.",
                    roleTag = "AoE / Control",
                    themeColor = new Color(0.58f, 0.65f, 0.82f, 1f)
                };
            default:
                return new AbilityDetail
                {
                    displayName = "Iron Fortress",
                    buttonLabel = "Fort.",
                    shortDescription = "Raise cover and punish intruders.",
                    roleTag = "Ultimate / Zone",
                    themeColor = new Color(0.45f, 0.52f, 0.68f, 1f)
                };
        }
    }

    public static AbilityDetail GetPlagueGardenerDetail(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1:
                return new AbilityDetail
                {
                    displayName = "Thorn Volley",
                    buttonLabel = "Thorn",
                    shortDescription = "Fire thorns in a cone — poke and slow.",
                    roleTag = "Cone / Poke",
                    themeColor = new Color(0.38f, 0.78f, 0.32f, 1f)
                };
            case 2:
                return new AbilityDetail
                {
                    displayName = "Root Patch",
                    buttonLabel = "Root",
                    shortDescription = "Root survivors standing in a ground patch.",
                    roleTag = "Zone / Control",
                    themeColor = new Color(0.32f, 0.72f, 0.28f, 1f)
                };
            case 3:
                return new AbilityDetail
                {
                    displayName = "Toxic Spore",
                    buttonLabel = "Spore",
                    shortDescription = "Release spores that poison and slow.",
                    roleTag = "Zone / DoT",
                    themeColor = new Color(0.42f, 0.85f, 0.35f, 1f)
                };
            default:
                return new AbilityDetail
                {
                    displayName = "Plague Bloom",
                    buttonLabel = "Bloom",
                    shortDescription = "Overgrow the area with vines and burst damage.",
                    roleTag = "Ultimate / Zone",
                    themeColor = new Color(0.55f, 0.95f, 0.38f, 1f)
                };
        }
    }
}
