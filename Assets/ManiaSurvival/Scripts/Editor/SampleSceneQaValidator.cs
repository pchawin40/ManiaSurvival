using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SampleSceneQaValidator
{
    private const string SampleScenePath = "Assets/ManiaSurvival/Scenes/SampleScene.unity";

    [MenuItem("Mania Survival/QA/Prepare SampleScene (Mana + Waterfall)")]
    public static void PrepareSampleScene()
    {
        WaterfallManaZoneSetup.SetupWaterfallManaRegenZone();
        UnitManaSceneSetup.EnsureUnitManaOnSampleScenePlayers();
        ValidateSampleSceneSetup();
    }

    [MenuItem("Mania Survival/QA/Validate SampleScene Setup")]
    public static void ValidateSampleSceneSetup()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene previousScene = SceneManager.GetActiveScene();
        string previousPath = previousScene.path;

        try
        {
            Scene sampleScene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            RunValidation(sampleScene);
        }
        finally
        {
            if (!string.IsNullOrEmpty(previousPath) && previousPath != SampleScenePath)
            {
                EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
            }
        }
    }

    private static void RunValidation(Scene scene)
    {
        int passCount = 0;
        int warnCount = 0;
        int failCount = 0;

        void Pass(string message)
        {
            passCount++;
            Debug.Log("[QA] PASS: " + message);
        }

        void Warn(string message)
        {
            warnCount++;
            Debug.LogWarning("[QA] WARN: " + message);
        }

        void Fail(string message)
        {
            failCount++;
            Debug.LogError("[QA] FAIL: " + message);
        }

        GameObject[] roots = scene.GetRootGameObjects();
        List<GameObject> allObjects = new List<GameObject>();
        for (int i = 0; i < roots.Length; i++)
        {
            CollectHierarchy(roots[i], allObjects);
        }

        int missingScripts = CountMissingScripts(allObjects);
        if (missingScripts == 0)
        {
            Pass("No missing script components in SampleScene");
        }
        else
        {
            Fail("Found " + missingScripts + " missing script component(s) in SampleScene");
        }

        ManiaGameManager gameManager = Object.FindFirstObjectByType<ManiaGameManager>();
        if (gameManager != null)
        {
            Pass("ManiaGameManager exists");
        }
        else
        {
            Fail("ManiaGameManager missing");
        }

        LocalRoleController roleController = Object.FindFirstObjectByType<LocalRoleController>();
        if (roleController != null)
        {
            Pass("LocalRoleController exists");
            ValidateRoleControllerReferences(roleController, Pass, Warn, Fail);
        }
        else
        {
            Fail("LocalRoleController missing");
        }

        ManiaGameUI gameUi = Object.FindFirstObjectByType<ManiaGameUI>();
        if (gameUi != null)
        {
            Pass("ManiaGameUI exists");
            ValidateGameUiReferences(gameUi, Pass, Warn, Fail);
        }
        else
        {
            Fail("ManiaGameUI missing");
        }

        GameObject monster = FindTaggedObject("Monster");
        if (monster != null)
        {
            Pass("Monster exists");
            ValidateUnitComponents(monster, true, Pass, Fail);
        }
        else
        {
            Fail("Monster missing (tag Monster)");
        }

        GameObject survivor = FindMainSurvivor();
        if (survivor != null)
        {
            Pass("Main Survivor exists");
            ValidateUnitComponents(survivor, false, Pass, Fail);
        }
        else
        {
            Fail("Main Survivor missing");
        }

        ValidateAbilityButtonLabels(gameUi, monster, Pass, Warn, Fail);
        ValidatePredatorPrototypeClasses(monster, Pass, Warn, Fail);

        if (Object.FindFirstObjectByType<ArenaBounds>() != null || GameObject.Find("ArenaBounds") != null)
        {
            Pass("ArenaBounds exists");
        }
        else
        {
            Warn("ArenaBounds not in scene (may bootstrap at runtime)");
        }

        ValidateHellfirePit(Pass, Fail);
        ValidateHeavenFloor(Pass, Warn, Fail);
        ValidateWaterfallManaZone(Pass, Fail);
        ValidateBlockerColliders(allObjects, Pass, Fail);
        ValidateGeneratedProps(allObjects, Pass, Warn, Fail);

        Debug.Log("[QA] SampleScene validation complete: " + passCount + " pass, "
            + warnCount + " warnings, " + failCount + " fails");
    }

    private static void CollectHierarchy(GameObject root, List<GameObject> output)
    {
        if (root == null)
        {
            return;
        }

        output.Add(root);
        Transform transform = root.transform;
        for (int i = 0; i < transform.childCount; i++)
        {
            CollectHierarchy(transform.GetChild(i).gameObject, output);
        }
    }

    private static int CountMissingScripts(List<GameObject> objects)
    {
        int count = 0;
        for (int i = 0; i < objects.Count; i++)
        {
            Component[] components = objects[i].GetComponents<Component>();
            for (int c = 0; c < components.Length; c++)
            {
                if (components[c] == null)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static GameObject FindTaggedObject(string tag)
    {
        GameObject[] tagged = GameObject.FindGameObjectsWithTag(tag);
        return tagged.Length > 0 ? tagged[0] : null;
    }

    private static GameObject FindMainSurvivor()
    {
        LocalRoleController roleController = Object.FindFirstObjectByType<LocalRoleController>();
        if (roleController != null && roleController.survivorMovement != null)
        {
            return roleController.survivorMovement.gameObject;
        }

        SurvivorMovement[] survivors = Object.FindObjectsByType<SurvivorMovement>(FindObjectsSortMode.None);
        for (int i = 0; i < survivors.Length; i++)
        {
            SurvivorMovement movement = survivors[i];
            if (movement != null && movement.enabled)
            {
                return movement.gameObject;
            }
        }

        return FindTaggedObject("Survivor");
    }

    private static void ValidateRoleControllerReferences(
        LocalRoleController roleController,
        System.Action<string> pass,
        System.Action<string> warn,
        System.Action<string> fail)
    {
        bool allAssigned = roleController.survivorMovement != null
            && roleController.monsterMovement != null;

        if (allAssigned)
        {
            pass("LocalRoleController core references assigned");
        }
        else
        {
            StringBuilder missing = new StringBuilder();
            if (roleController.survivorMovement == null)
            {
                missing.Append("survivorMovement ");
            }

            if (roleController.monsterMovement == null)
            {
                missing.Append("monsterMovement ");
            }

            fail("LocalRoleController missing references: " + missing.ToString().Trim());
        }

        if (roleController.cameraFollow == null)
        {
            warn("LocalRoleController.cameraFollow is not assigned");
        }
    }

    private static void ValidateGameUiReferences(
        ManiaGameUI gameUi,
        System.Action<string> pass,
        System.Action<string> warn,
        System.Action<string> fail)
    {
        bool coreAssigned = gameUi.gameManager != null
            && gameUi.localRoleController != null
            && gameUi.survivorAbilityController != null
            && gameUi.predatorAbilityController != null;

        if (coreAssigned)
        {
            pass("ManiaGameUI core references assigned");
        }
        else
        {
            fail("ManiaGameUI missing one or more core references (gameManager, localRoleController, ability controllers)");
        }

        if (gameUi.abilityTooltipPanel == null)
        {
            warn("ManiaGameUI.abilityTooltipPanel is not assigned");
        }

        if (gameUi.timerText == null)
        {
            warn("ManiaGameUI.timerText is not assigned");
        }
    }

    private static void ValidateUnitComponents(
        GameObject unit,
        bool isPredator,
        System.Action<string> pass,
        System.Action<string> fail)
    {
        RequireComponent<UnitHealth>(unit, pass, fail);
        RequireComponent<CharacterController>(unit, pass, fail);
        RequireComponent<AbilityController>(unit, pass, fail);
        RequireComponent<UnitMana>(unit, pass, fail);

        if (isPredator)
        {
            RequireComponent<MonsterPlayerMovement>(unit, pass, fail);
            RequireComponent<PredatorClassManager>(unit, pass, fail);
        }
        else
        {
            RequireComponent<SurvivorMovement>(unit, pass, fail);
            RequireComponent<SurvivorClassManager>(unit, pass, fail);
        }
    }

    private static void RequireComponent<T>(
        GameObject host,
        System.Action<string> pass,
        System.Action<string> fail) where T : Component
    {
        if (host.GetComponent<T>() != null)
        {
            pass(host.name + " has " + typeof(T).Name);
        }
        else
        {
            fail(host.name + " missing " + typeof(T).Name);
        }
    }

    private static void ValidateAbilityButtonLabels(
        ManiaGameUI gameUi,
        GameObject monster,
        System.Action<string> pass,
        System.Action<string> warn,
        System.Action<string> fail)
    {
        if (gameUi == null)
        {
            fail("Cannot validate ability buttons — ManiaGameUI missing");
            return;
        }

        PredatorClassManager predatorClassManager = monster != null
            ? monster.GetComponent<PredatorClassManager>()
            : null;
        PredatorClass predatorClass = predatorClassManager != null
            ? predatorClassManager.GetCurrentPredatorClass()
            : PredatorClass.RelentlessHook;

        string[] predatorLabels = GetExpectedPredatorLabels(predatorClass);
        ValidateButtonLabel(gameUi.predatorMeleeButton, predatorLabels[0], pass, fail);
        ValidateButtonLabel(gameUi.predatorAbility2Button, predatorLabels[1], pass, fail);
        ValidateButtonLabel(gameUi.predatorAbility3Button, predatorLabels[2], pass, fail);
        ValidateButtonLabel(gameUi.predatorUltimateButton, predatorLabels[3], pass, fail);

        ValidateButtonLabel(gameUi.survivorPrimaryButton, "Biotic", pass, fail);
        ValidateButtonLabel(gameUi.survivorAbility2Button, "Pulse", pass, fail);
        ValidateButtonLabel(gameUi.survivorAbility3Button, "Tether", pass, fail);
        ValidateButtonLabel(gameUi.survivorUltimateButton, "Sanct.", pass, fail);

        ValidatePredatorButtonsAvoidGenericLabels(gameUi, warn, fail);
    }

    private static string[] GetExpectedPredatorLabels(PredatorClass predatorClass)
    {
        switch (predatorClass)
        {
            case PredatorClass.SwarmOverlord:
                return new[] { "Spit", "Brood", "Infest", "Hive" };
            case PredatorClass.Juggernaut:
                return new[] { "Flame", "Leap", "Roar", "Meteor" };
            default:
                return new[] { "Spray", "Hook", "Tonic", "Barrage" };
        }
    }

    private static void ValidatePredatorButtonsAvoidGenericLabels(
        ManiaGameUI gameUi,
        System.Action<string> warn,
        System.Action<string> fail)
    {
        Button[] buttons =
        {
            gameUi.predatorMeleeButton,
            gameUi.predatorAbility2Button,
            gameUi.predatorAbility3Button,
            gameUi.predatorUltimateButton
        };

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
            {
                continue;
            }

            string label = ReadButtonLabel(buttons[i]);
            if (string.Equals(label.Trim(), "Ability", System.StringComparison.OrdinalIgnoreCase))
            {
                fail("Predator ability button '" + buttons[i].name + "' still uses generic label 'Ability'");
            }
        }

        pass("Predator ability buttons avoid generic 'Ability' label");
    }

    private static void ValidatePredatorPrototypeClasses(
        GameObject monster,
        System.Action<string> pass,
        System.Action<string> warn,
        System.Action<string> fail)
    {
        if (monster == null)
        {
            fail("Cannot validate predator prototype classes — Monster missing");
            return;
        }

        PredatorClassManager manager = monster.GetComponent<PredatorClassManager>();
        if (manager == null)
        {
            fail("Monster missing PredatorClassManager for prototype validation");
            return;
        }

        if (!PredatorClassManager.IsPlayablePrototypeClass(manager.GetCurrentPredatorClass()))
        {
            warn("Monster active predator class is not one of the three prototype classes: "
                + manager.GetCurrentPredatorClass());
        }
        else
        {
            pass("Monster predator class is playable prototype: " + manager.GetClassDisplayName());
        }

        for (int slot = 1; slot <= 4; slot++)
        {
            AbilityDetail detail = manager.GetPredatorAbilityDetail(slot);
            string label = detail.GetButtonLabelOrDisplayName();
            if (string.IsNullOrWhiteSpace(label) || string.Equals(label, "Ability", System.StringComparison.OrdinalIgnoreCase))
            {
                fail("Predator slot " + slot + " has missing or generic ability label");
                continue;
            }

            pass("Predator slot " + slot + " label configured: " + label);
        }

        PredatorClassSwitcher switcher = monster.GetComponent<PredatorClassSwitcher>();
        if (switcher == null)
        {
            warn("Monster missing PredatorClassSwitcher (F1/F2/F3 dev switching)");
        }
        else
        {
            pass("Monster has PredatorClassSwitcher for dev class hotkeys");
        }
    }

    private static void ValidateButtonLabel(
        Button button,
        string expectedLabel,
        System.Action<string> pass,
        System.Action<string> fail)
    {
        if (button == null)
        {
            fail("Ability button for '" + expectedLabel + "' is missing");
            return;
        }

        string label = ReadButtonLabel(button);
        if (string.IsNullOrEmpty(label))
        {
            fail("Ability button '" + button.name + "' has no label text");
            return;
        }

        if (label.Contains(expectedLabel))
        {
            pass("Ability button labeled '" + expectedLabel + "'");
        }
        else
        {
            fail("Ability button '" + button.name + "' label '" + label + "' does not contain '" + expectedLabel + "'");
        }
    }

    private static string ReadButtonLabel(Button button)
    {
        AbilityCooldownButton cooldownButton = button.GetComponent<AbilityCooldownButton>();
        if (cooldownButton != null && cooldownButton.abilityNameText != null)
        {
            return cooldownButton.abilityNameText.text;
        }

        TMPro.TMP_Text[] texts = button.GetComponentsInChildren<TMPro.TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (!texts[i].gameObject.name.ToLowerInvariant().Contains("cooldown"))
            {
                return texts[i].text;
            }
        }

        return string.Empty;
    }

    private static void ValidateHellfirePit(System.Action<string> pass, System.Action<string> fail)
    {
        HellfirePitWalkableFloor floor = Object.FindFirstObjectByType<HellfirePitWalkableFloor>();
        HellfirePitDamageZone damage = Object.FindFirstObjectByType<HellfirePitDamageZone>();

        if (floor != null && damage != null)
        {
            pass("Hellfire pit has walkable solid floor + trigger damage zone");
        }
        else
        {
            if (floor == null)
            {
                fail("Hellfire pit missing HellfirePitWalkableFloor");
            }

            if (damage == null)
            {
                fail("Hellfire pit missing HellfirePitDamageZone");
            }
        }
    }

    private static void ValidateHeavenFloor(
        System.Action<string> pass,
        System.Action<string> warn,
        System.Action<string> fail)
    {
        GameObject platform = GameObject.Find("HeavenSafeZonePlatform");
        if (platform == null)
        {
            warn("HeavenSafeZonePlatform not found");
            return;
        }

        Collider[] colliders = platform.GetComponentsInChildren<Collider>(true);
        bool hasSolid = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col != null && !col.isTrigger && col.enabled)
            {
                hasSolid = true;
                break;
            }
        }

        if (hasSolid)
        {
            pass("Heaven floor has solid walkable floor");
        }
        else
        {
            fail("Heaven floor has no solid walkable collider");
        }
    }

    private static void ValidateWaterfallManaZone(System.Action<string> pass, System.Action<string> fail)
    {
        GameObject zoneObject = GameObject.Find("WaterfallManaRegenZone");
        if (zoneObject == null)
        {
            fail("WaterfallManaRegenZone missing — run Mania Survival > Map > Setup Waterfall Mana Regen Zone");
            return;
        }

        ManaRegenZone zone = zoneObject.GetComponent<ManaRegenZone>();
        Collider trigger = zoneObject.GetComponent<Collider>();
        if (zone != null && trigger != null && trigger.isTrigger)
        {
            pass("Waterfall mana regen zone exists with trigger collider");
        }
        else
        {
            fail("WaterfallManaRegenZone missing ManaRegenZone or trigger collider");
        }
    }

    private static void ValidateBlockerColliders(
        List<GameObject> allObjects,
        System.Action<string> pass,
        System.Action<string> fail)
    {
        bool blockerTriggerFound = false;
        bool criticalFloorTriggerOnly = false;

        for (int i = 0; i < allObjects.Count; i++)
        {
            GameObject obj = allObjects[i];
            string lowerName = obj.name.ToLowerInvariant();
            Collider[] colliders = obj.GetComponents<Collider>();
            for (int c = 0; c < colliders.Length; c++)
            {
                Collider col = colliders[c];
                if (col == null || !col.enabled)
                {
                    continue;
                }

                if (lowerName.Contains("blocker") && lowerName.Contains("wall") && col.isTrigger)
                {
                    blockerTriggerFound = true;
                }

                bool looksLikeFloor = lowerName.Contains("floor") || lowerName.Contains("platform") || lowerName.Contains("walkable");
                if (looksLikeFloor && col.isTrigger && !lowerName.Contains("trigger") && !lowerName.Contains("damage") && !lowerName.Contains("zone"))
                {
                    criticalFloorTriggerOnly = true;
                }
            }
        }

        if (!blockerTriggerFound)
        {
            pass("No blocker wall collider is trigger");
        }
        else
        {
            fail("A Blocker_Wall collider is set to trigger");
        }

        if (!criticalFloorTriggerOnly)
        {
            pass("No critical floor collider is trigger-only");
        }
        else
        {
            fail("A critical floor/platform collider is trigger-only");
        }
    }

    private static void ValidateGeneratedProps(
        List<GameObject> allObjects,
        System.Action<string> pass,
        System.Action<string> warn,
        System.Action<string> fail)
    {
        Transform parent = GameObject.Find(PrototypeMapPropsGenerator.ParentObjectName)?.transform;
        if (parent == null)
        {
            warn("PrototypeMapProps_Auto not found — skipping generated props script check");
            return;
        }

        int missing = 0;
        for (int i = 0; i < parent.childCount; i++)
        {
            Component[] components = parent.GetChild(i).GetComponents<Component>();
            for (int c = 0; c < components.Length; c++)
            {
                if (components[c] == null)
                {
                    missing++;
                }
            }
        }

        if (missing == 0)
        {
            pass("Generated props have no missing scripts");
        }
        else
        {
            fail("Generated props have " + missing + " missing script(s)");
        }
    }
}
