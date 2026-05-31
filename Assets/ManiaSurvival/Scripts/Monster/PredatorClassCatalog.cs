using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static catalog of the six playable predator prototype classes for UI and selection.
/// </summary>
public static class PredatorClassCatalog
{
    private static readonly PredatorClass[] PlayableClasses =
    {
        PredatorClass.RelentlessHook,
        PredatorClass.SwarmOverlord,
        PredatorClass.Juggernaut,
        PredatorClass.ShadowStalker,
        PredatorClass.IronColossus,
        PredatorClass.PlagueGardener
    };

    private static readonly Dictionary<PredatorClass, PredatorClassDetail> DetailsByClass = BuildDetails();

    public static IReadOnlyList<PredatorClass> GetPlayableClasses()
    {
        return PlayableClasses;
    }

    public static bool IsPlayableClass(PredatorClass predatorClass)
    {
        return DetailsByClass.ContainsKey(predatorClass);
    }

    public static PredatorClassDetail GetDetail(PredatorClass predatorClass)
    {
        if (DetailsByClass.TryGetValue(predatorClass, out PredatorClassDetail detail))
        {
            return detail;
        }

        return CreateFallbackDetail(predatorClass);
    }

    public static int GetPlayableIndex(PredatorClass predatorClass)
    {
        for (int i = 0; i < PlayableClasses.Length; i++)
        {
            if (PlayableClasses[i] == predatorClass)
            {
                return i;
            }
        }

        return 0;
    }

    public static PredatorClass GetPlayableClassByIndex(int index)
    {
        index = Mathf.Clamp(index, 0, PlayableClasses.Length - 1);
        return PlayableClasses[index];
    }

    private static Dictionary<PredatorClass, PredatorClassDetail> BuildDetails()
    {
        var map = new Dictionary<PredatorClass, PredatorClassDetail>();
        map[PredatorClass.RelentlessHook] = new PredatorClassDetail
        {
            classId = PredatorClass.RelentlessHook,
            displayName = "Relentless Hook",
            shortRole = "Catch / Control",
            tagline = "Pull survivors out of position and punish bad spacing.",
            difficulty = "Medium",
            themeColor = new Color(0.95f, 0.45f, 0.18f, 1f),
            abilityNames = new[] { "Spray", "Hook", "Tonic", "Barrage" },
            abilityShortDescriptions = new[]
            {
                "Cone blast that damages and knocks survivors back.",
                "Skill-shot pull that drags a survivor toward you.",
                "Self-heal plus a slowing toxic cloud.",
                "Telegraphed cone bursts — move or get shredded."
            }
        };

        map[PredatorClass.SwarmOverlord] = new PredatorClassDetail
        {
            classId = PredatorClass.SwarmOverlord,
            displayName = "Swarm Overlord",
            shortRole = "Zone / Summon",
            tagline = "Spit, brood, infest, then call the hive.",
            difficulty = "Easy",
            themeColor = new Color(0.45f, 0.92f, 0.28f, 1f),
            abilityNames = new[] { "Spit", "Brood", "Infest", "Hive" },
            abilityShortDescriptions = new[]
            {
                "Acid cone that chips and slows survivors.",
                "Spawn broodlings that chase and harass.",
                "Drop a toxic zone that damages over time.",
                "Warn, pulse damage, then summon more brood."
            }
        };

        map[PredatorClass.Juggernaut] = new PredatorClassDetail
        {
            classId = PredatorClass.Juggernaut,
            displayName = "Dragon Juggernaut",
            shortRole = "Burst / AoE",
            tagline = "Flame breath, leap, roar, then meteor.",
            difficulty = "Medium",
            themeColor = new Color(1f, 0.42f, 0.12f, 1f),
            abilityNames = new[] { "Flame", "Leap", "Roar", "Meteor" },
            abilityShortDescriptions = new[]
            {
                "Cone fire that burns survivors in front of you.",
                "Leap forward and slam the landing zone.",
                "Knock survivors back and slow them.",
                "Warn, then crash down with fire aftermath."
            }
        };

        map[PredatorClass.ShadowStalker] = new PredatorClassDetail
        {
            classId = PredatorClass.ShadowStalker,
            displayName = "Shadow Stalker",
            shortRole = "Stealth / Pick",
            tagline = "Slash from the dark, vanish, mark prey, then bring night.",
            difficulty = "Hard",
            themeColor = new Color(0.52f, 0.28f, 0.82f, 1f),
            abilityNames = new[] { "Slash", "Vanish", "Mark", "Night." },
            abilityShortDescriptions = new[]
            {
                "Quick melee arc that cuts through a tight cone.",
                "Fade from sight and move faster for a short time.",
                "Curse a survivor with a slowing hunter's mark.",
                "Blanket an area in darkness — slow and shred."
            }
        };

        map[PredatorClass.IronColossus] = new PredatorClassDetail
        {
            classId = PredatorClass.IronColossus,
            displayName = "Iron Colossus",
            shortRole = "Tank / Frontline",
            tagline = "Crush, guard, quake, then fortify the arena.",
            difficulty = "Easy",
            themeColor = new Color(0.55f, 0.62f, 0.78f, 1f),
            abilityNames = new[] { "Crush", "Guard", "Quake", "Fort." },
            abilityShortDescriptions = new[]
            {
                "Heavy slam that damages survivors nearby.",
                "Brace yourself — brief damage reduction.",
                "Ground slam that knocks survivors away.",
                "Raise a ring of cover and punish intruders."
            }
        };

        map[PredatorClass.PlagueGardener] = new PredatorClassDetail
        {
            classId = PredatorClass.PlagueGardener,
            displayName = "Plague Gardener",
            shortRole = "Zone / Control",
            tagline = "Thorn, root, spore, then bloom the garden.",
            difficulty = "Medium",
            themeColor = new Color(0.38f, 0.78f, 0.32f, 1f),
            abilityNames = new[] { "Thorn", "Root", "Spore", "Bloom" },
            abilityShortDescriptions = new[]
            {
                "Fire thorns in a cone — poke and slow.",
                "Root survivors standing in a ground patch.",
                "Release spores that poison and slow.",
                "Overgrow the area with vines and burst damage."
            }
        };

        return map;
    }

    private static PredatorClassDetail CreateFallbackDetail(PredatorClass predatorClass)
    {
        return new PredatorClassDetail
        {
            classId = predatorClass,
            displayName = predatorClass.ToString(),
            shortRole = "Predator",
            tagline = predatorClass.ToString(),
            difficulty = "Unknown",
            themeColor = Color.gray,
            abilityNames = new[] { "Slot 1", "Slot 2", "Slot 3", "Slot 4" },
            abilityShortDescriptions = new[] { "", "", "", "" }
        };
    }
}
