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
        int infoCount = 0;
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

        void Info(string message)
        {
            infoCount++;
            Debug.Log("[QA] INFO: " + message);
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
        ValidatePredatorPrototypeClasses(monster, Pass, Warn, Fail, Info);

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
        ValidateGeneratedProps(allObjects, Pass, Info, Fail);
        ValidateMovementTraversal(survivor, monster, gameUi, Pass, Warn, Info);

        Debug.Log("[QA] SampleScene validation complete: " + passCount + " pass, "
            + infoCount + " info, " + warnCount + " warnings, " + failCount + " fails");
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

        gameUi.RefreshAbilityLabels(force: true);
        gameUi.RefreshAbilityInfo(force: true);

        GameObject survivor = FindMainSurvivor();
        SurvivorClassManager survivorClassManager = survivor != null
            ? survivor.GetComponent<SurvivorClassManager>()
            : null;
        PredatorClassManager predatorClassManager = monster != null
            ? monster.GetComponent<PredatorClassManager>()
            : null;
        PredatorClass predatorClass = predatorClassManager != null
            ? predatorClassManager.GetCurrentPredatorClass()
            : PredatorClass.RelentlessHook;

        ValidateConfiguredAbilityLabels("Survivor", survivorClassManager, 4, slot =>
        {
            AbilityDetail detail = survivorClassManager != null
                ? survivorClassManager.GetAbilityDetail(slot)
                : default;
            if (!detail.IsConfigured)
            {
                detail = AbilityPresentationFallback.GetMedicDetail(slot);
            }

            return detail.GetButtonLabelOrDisplayName();
        }, new[] { "Biotic", "Pulse", "Tether", "Sanct." }, pass, fail);

        string[] predatorLabels = GetExpectedPredatorLabels(predatorClass);
        ValidateConfiguredAbilityLabels("Predator", predatorClassManager, 4, slot =>
        {
            AbilityDetail detail = predatorClassManager != null
                ? predatorClassManager.GetPredatorAbilityDetail(slot)
                : default;
            if (!detail.IsConfigured)
            {
                detail = AbilityPresentationFallback.ResolveForUi(true, predatorClass, slot, detail);
            }

            return detail.GetButtonLabelOrDisplayName();
        }, predatorLabels, pass, fail);

        ValidateButtonLabel(gameUi.survivorPrimaryButton, "Biotic", pass, fail);
        ValidateButtonLabel(gameUi.survivorAbility2Button, "Pulse", pass, fail);
        ValidateButtonLabel(gameUi.survivorAbility3Button, "Tether", pass, fail);
        ValidateButtonLabel(gameUi.survivorUltimateButton, "Sanct.", pass, fail);

        ValidateButtonLabel(gameUi.predatorMeleeButton, predatorLabels[0], pass, fail);
        ValidateButtonLabel(gameUi.predatorAbility2Button, predatorLabels[1], pass, fail);
        ValidateButtonLabel(gameUi.predatorAbility3Button, predatorLabels[2], pass, fail);
        ValidateButtonLabel(gameUi.predatorUltimateButton, predatorLabels[3], pass, fail);

        ValidatePredatorButtonsAvoidGenericLabels(gameUi, pass, warn, fail);
    }

    private static void ValidateConfiguredAbilityLabels(
        string roleLabel,
        Component classManager,
        int slotCount,
        System.Func<int, string> readLabel,
        string[] expectedContains,
        System.Action<string> pass,
        System.Action<string> fail)
    {
        if (classManager == null)
        {
            fail(roleLabel + " class manager missing for ability label validation");
            return;
        }

        for (int slot = 1; slot <= slotCount; slot++)
        {
            string label = readLabel(slot);
            string expected = expectedContains[slot - 1];
            if (string.IsNullOrWhiteSpace(label)
                || string.Equals(label.Trim(), "Ability", System.StringComparison.OrdinalIgnoreCase)
                || label.Contains("Use Ability", System.StringComparison.OrdinalIgnoreCase)
                || label.Contains("Use Ultimate", System.StringComparison.OrdinalIgnoreCase))
            {
                fail(roleLabel + " slot " + slot + " configured label is generic or missing ('" + label + "')");
                continue;
            }

            if (label.Contains(expected))
            {
                pass(roleLabel + " slot " + slot + " configured label contains '" + expected + "'");
            }
            else
            {
                fail(roleLabel + " slot " + slot + " configured label '" + label + "' does not contain '" + expected + "'");
            }
        }
    }

    private static string[] GetExpectedPredatorLabels(PredatorClass predatorClass)
    {
        switch (predatorClass)
        {
            case PredatorClass.SwarmOverlord:
                return new[] { "Spit", "Brood", "Infest", "Hive" };
            case PredatorClass.Juggernaut:
                return new[] { "Flame", "Leap", "Roar", "Meteor" };
            case PredatorClass.ShadowStalker:
                return new[] { "Slash", "Vanish", "Mark", "Night." };
            case PredatorClass.IronColossus:
                return new[] { "Crush", "Guard", "Quake", "Fort." };
            case PredatorClass.PlagueGardener:
                return new[] { "Thorn", "Root", "Spore", "Bloom" };
            default:
                return new[] { "Spray", "Hook", "Tonic", "Barrage" };
        }
    }

    private static void ValidatePredatorButtonsAvoidGenericLabels(
        ManiaGameUI gameUi,
        System.Action<string> pass,
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
        System.Action<string> fail,
        System.Action<string> info)
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
            warn("Monster active predator class is not one of the six playable classes: "
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

        ValidatePredatorClassCatalogPortraits(info, warn);
        ValidatePredatorClassTabLabels(pass, warn);
        ValidateSwarmBroodTuning(manager, pass, warn, fail);
        ValidateBioticAntiSummon(FindMainSurvivor(), pass, warn, fail);
        ValidatePredatorClassSelectUi(pass, info);

        PredatorClassSwitcher switcher = monster.GetComponent<PredatorClassSwitcher>();
        if (switcher == null)
        {
            warn("Monster missing PredatorClassSwitcher (F1-F6 dev switching)");
        }
        else
        {
            pass("Monster has PredatorClassSwitcher for dev class hotkeys");
        }

        PredatorClassSelectPanel selectPanel = Object.FindFirstObjectByType<PredatorClassSelectPanel>();
        if (selectPanel == null)
        {
            info("PredatorClassSelectPanel not in scene — ManiaGameUI will build one at runtime under startScreen");
        }
        else
        {
            pass("PredatorClassSelectPanel exists in scene");
        }
    }

    private static void ValidatePredatorClassCatalogPortraits(
        System.Action<string> info,
        System.Action<string> warn)
    {
        IReadOnlyList<PredatorClass> playable = PredatorClassCatalog.GetPlayableClasses();
        int missingPortraitCount = 0;

        for (int i = 0; i < playable.Count; i++)
        {
            PredatorClassDetail detail = PredatorClassCatalog.GetDetail(playable[i]);
            if (detail.HasPortrait)
            {
                info("Predator class portrait assigned: " + detail.displayName);
            }
            else
            {
                missingPortraitCount++;
                warn("Predator class portrait missing (optional): " + detail.displayName);
            }
        }

        if (missingPortraitCount == playable.Count)
        {
            info("No predator class portraits assigned yet — UI uses theme colors instead");
        }
    }

    private static void ValidatePredatorClassTabLabels(
        System.Action<string> pass,
        System.Action<string> warn)
    {
        IReadOnlyList<PredatorClass> playable = PredatorClassCatalog.GetPlayableClasses();
        for (int i = 0; i < playable.Count; i++)
        {
            PredatorClassDetail detail = PredatorClassCatalog.GetDetail(playable[i]);
            if (string.IsNullOrWhiteSpace(detail.tabShortName))
            {
                warn("Predator class tab label missing: " + detail.displayName);
                continue;
            }

            pass("Predator class tab label configured: " + detail.tabShortName);
        }
    }

    private static void ValidateSwarmBroodTuning(
        PredatorClassManager manager,
        System.Action<string> pass,
        System.Action<string> warn,
        System.Action<string> fail)
    {
        if (manager.broodlingDamage > 2)
        {
            warn("Broodling damage is high for chip-pressure tuning: " + manager.broodlingDamage);
        }
        else
        {
            pass("Broodling damage tuned low: " + manager.broodlingDamage);
        }

        if (manager.broodlingLifetime <= 0f)
        {
            fail("Broodling lifetime must be > 0 so summons despawn");
        }
        else
        {
            pass("Broodling lifetime configured: " + manager.broodlingLifetime.ToString("0.0") + "s");
        }

        if (manager.broodlingContactInterval < 0.9f)
        {
            warn("Broodling bite interval is fast: " + manager.broodlingContactInterval.ToString("0.0") + "s");
        }
        else
        {
            pass("Broodling bite interval configured: " + manager.broodlingContactInterval.ToString("0.0") + "s");
        }

        if (manager.swarmBroodManaCost < 40f || manager.swarmBroodManaCost > 50f)
        {
            warn("Swarm Brood mana cost outside expected ~45 range: " + manager.swarmBroodManaCost.ToString("0"));
        }
        else
        {
            pass("Swarm Brood mana cost configured: " + manager.swarmBroodManaCost.ToString("0"));
        }

        if (manager.maxActiveBroodlings > 8)
        {
            warn("Active brood cap is high: " + manager.maxActiveBroodlings);
        }
        else
        {
            pass("Active brood cap configured: " + manager.maxActiveBroodlings);
        }

        if (manager.swarmBroodSpawnCount > 4)
        {
            warn("Brood spawn count is high: " + manager.swarmBroodSpawnCount);
        }
        else
        {
            pass("Brood spawn count configured: " + manager.swarmBroodSpawnCount);
        }

        if (manager.swarmHiveBroodSpawnCount > 2)
        {
            warn("Hive brood spawn count is high: " + manager.swarmHiveBroodSpawnCount);
        }
        else
        {
            pass("Hive brood spawn count configured: " + manager.swarmHiveBroodSpawnCount);
        }

        if (manager.broodlingMaxHealth <= 0)
        {
            fail("Broodling max health must be > 0 so survivors can kill summons");
        }
        else
        {
            pass("Broodling max health configured: " + manager.broodlingMaxHealth);
        }

        if (typeof(MiniWorldHealthBarBuilder).GetMethod("Attach") != null)
        {
            pass("Broodling health bar builder available for runtime summons");
        }
        else
        {
            warn("Broodling health bar builder missing");
        }
    }

    private static void ValidateBioticAntiSummon(
        GameObject survivor,
        System.Action<string> pass,
        System.Action<string> warn,
        System.Action<string> fail)
    {
        if (survivor == null)
        {
            warn("Main survivor missing for Biotic anti-summon validation");
            return;
        }

        SurvivorClassManager manager = survivor.GetComponent<SurvivorClassManager>();
        if (manager == null)
        {
            fail("Survivor missing SurvivorClassManager for Biotic validation");
            return;
        }

        if (manager.bioticDartSummonDamage <= 0)
        {
            fail("Biotic anti-broodling damage must be > 0");
        }
        else
        {
            pass("Biotic anti-broodling damage configured: " + manager.bioticDartSummonDamage);
        }

        if (manager.bioticDartMaxEnemyHits < 2)
        {
            warn("Biotic max enemy hits is low: " + manager.bioticDartMaxEnemyHits);
        }
        else
        {
            pass("Biotic max enemy hits configured: " + manager.bioticDartMaxEnemyHits);
        }
    }

    private static void ValidatePredatorClassSelectUi(
        System.Action<string> pass,
        System.Action<string> info)
    {
        PredatorClassSelectPanel selectPanel = Object.FindFirstObjectByType<PredatorClassSelectPanel>();
        if (selectPanel == null)
        {
            info("PredatorClassSelectPanel builds at runtime on Hunt — tab labels validated via catalog");
            return;
        }

        if (selectPanel.HasReadableTabLabels())
        {
            pass("Predator class select panel has readable tab TMP labels");
        }
        else
        {
            info("PredatorClassSelectPanel exists but tab labels are not built until Play Mode Show()");
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

        HeavenFloorCollider floorBootstrap = platform.GetComponent<HeavenFloorCollider>();
        if (floorBootstrap != null)
        {
            floorBootstrap.EnsureWalkableFloor();
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
            pass("Heaven floor has solid walkable collider");
            return;
        }

        if (floorBootstrap != null)
        {
            fail("Heaven floor has no solid walkable collider (HeavenFloorCollider present but walkable child missing)");
            return;
        }

        fail("Heaven floor has no solid walkable collider");
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

    private static void ValidateMovementTraversal(
        GameObject survivor,
        GameObject monster,
        ManiaGameUI gameUi,
        System.Action<string> pass,
        System.Action<string> warn,
        System.Action<string> info)
    {
        if (survivor != null)
        {
            SurvivorMovement survivorMove = survivor.GetComponent<SurvivorMovement>();
            if (survivorMove != null)
            {
                if (survivorMove.enableJump)
                {
                    pass("Survivor jump enabled (Space / optional UI button)");
                }
                else
                {
                    info("Survivor jump disabled (enableJump=false)");
                }

                info("Survivor jump defaults: height " + survivorMove.jumpHeight.ToString("0.0")
                    + ", double " + survivorMove.doubleJumpHeight.ToString("0.0")
                    + ", gravity " + survivorMove.gravity.ToString("0.0"));
            }

            if (survivor.GetComponent<SurvivorBlinkAbility>() != null)
            {
                pass("Survivor blink ability present (bounds checked at landing)");
            }
        }

        if (monster != null)
        {
            MonsterPlayerMovement predatorMove = monster.GetComponent<MonsterPlayerMovement>();
            if (predatorMove != null)
            {
                if (predatorMove.enablePredatorJump)
                {
                    pass("Predator heavy jump enabled");
                }
                else
                {
                    info("Predator jump disabled (enablePredatorJump=false)");
                }

                if (predatorMove.enablePredatorPounce)
                {
                    pass("Predator pounce enabled (Q or Shift+Space / optional UI)");
                }
                else
                {
                    warn("MonsterPlayerMovement.enablePredatorPounce is false");
                }

                info("Predator pounce tuning: distance " + predatorMove.pounceDistance.ToString("0.0")
                    + ", cooldown " + predatorMove.pounceCooldown.ToString("0.0")
                    + ", landing radius " + predatorMove.pounceLandingRadius.ToString("0.0"));
            }
            else
            {
                warn("Monster missing MonsterPlayerMovement for traversal checks");
            }
        }

        if (Object.FindFirstObjectByType<ArenaBounds>() != null || GameObject.Find("ArenaBounds") != null)
        {
            pass("Playable bounds source available for jump/pounce/blink/pads");
        }
        else
        {
            info("ArenaBounds may bootstrap at runtime — PlayableBoundsHelper fallback still applies");
        }

        if (gameUi != null)
        {
            if (!gameUi.showJumpButton || gameUi.survivorJumpButton == null)
            {
                info("Survivor jump UI button optional — keyboard Space still works");
            }

            if (!gameUi.showPredatorPounceButton || gameUi.predatorPounceButton == null)
            {
                info("Predator pounce UI button optional — keyboard Q / Shift+Space still works");
            }
        }
    }

    private static void ValidateGeneratedProps(
        List<GameObject> allObjects,
        System.Action<string> pass,
        System.Action<string> info,
        System.Action<string> fail)
    {
        Transform parent = GameObject.Find(PrototypeMapPropsGenerator.ParentObjectName)?.transform;
        if (parent == null)
        {
            parent = GameObject.Find(CreatePrototypeArenaProps.ParentObjectName)?.transform;
        }

        if (parent == null)
        {
            info("PrototypeMapProps_Auto not found — generated props not present, skipping optional check");
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
