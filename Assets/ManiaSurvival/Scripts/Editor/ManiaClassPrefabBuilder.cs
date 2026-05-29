using System.IO;
using UnityEditor;
using UnityEngine;

public class ManiaClassPrefabBuilder : EditorWindow
{
    private const string PrefabFolder = "Assets/ManiaSurvival/Prefabs/Abilities";

    [MenuItem("Mania Survival/Prefab Builder")]
    public static void Open()
    {
        GetWindow<ManiaClassPrefabBuilder>("Mania Class Prefab Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Mania Class Prefab Builder", EditorStyles.boldLabel);
        GUILayout.Space(6f);
        EditorGUILayout.HelpBox(
            "Builds missing greybox ability prefabs, assigns them to class managers, and runs a cleanup audit.",
            MessageType.Info);

        GUILayout.Space(8f);
        if (GUILayout.Button("Build / Assign / Audit", GUILayout.Height(34f)))
        {
            BuildAssignAndAudit();
        }

        if (GUILayout.Button("Only Generate Prefabs", GUILayout.Height(24f)))
        {
            GenerateAllPrefabs();
        }

        if (GUILayout.Button("Only Assign To Managers", GUILayout.Height(24f)))
        {
            AssignPrefabsToManagers();
        }

        if (GUILayout.Button("Only Assign To Prefab Assets", GUILayout.Height(24f)))
        {
            AssignPrefabsToPrefabAssets();
        }
    }

    private static void BuildAssignAndAudit()
    {
        GenerateAllPrefabs();
        AssignPrefabsToManagers();
        AssignPrefabsToPrefabAssets();
        RunCodeCleanupAudit();
        Debug.Log("[ManiaClassPrefabBuilder] Build/Assign/Audit complete.");
    }

    private static void GenerateAllPrefabs()
    {
        EnsureFolder(PrefabFolder);

        // Medic
        CreateOrReplacePrefab("VFX_BioticDart_Projectile", PrimitiveType.Capsule, new Vector3(0.18f, 0.45f, 0.18f), true, Vector3.zero);
        CreateOrReplacePrefab("VFX_ImmortalityField_Zone", PrimitiveType.Sphere, new Vector3(5f, 5f, 5f), true, Vector3.zero);

        // Weaver
        CreateOrReplacePrefab("VFX_BlossomMend_Projectile", PrimitiveType.Sphere, new Vector3(0.3f, 0.3f, 0.3f), false, Vector3.zero);
        CreateOrReplacePrefab("VFX_LifeGrip_Bubble", PrimitiveType.Sphere, new Vector3(1.3f, 1.3f, 1.3f), true, Vector3.zero);
        CreateOrReplacePrefab("VFX_PurifyingTalisman_Splash", PrimitiveType.Cube, new Vector3(0.45f, 0.06f, 0.35f), false, Vector3.zero);

        // Warden
        CreateOrReplacePrefab("VFX_EchoingShield_Zone", PrimitiveType.Sphere, new Vector3(8f, 8f, 8f), true, Vector3.zero);

        // SwarmOverlord
        CreateOrReplacePrefab("Structure_SentryTurret_Basic", PrimitiveType.Cylinder, new Vector3(0.8f, 1f, 0.8f), false, Vector3.zero);
        CreateOrReplacePrefab("Unit_TrackingMinion_Basic", PrimitiveType.Sphere, new Vector3(0.55f, 0.55f, 0.55f), false, Vector3.zero);
        CreateOrReplacePrefab("Hazard_MoltenCore_Pool", PrimitiveType.Cylinder, new Vector3(2.2f, 0.08f, 2.2f), true, Vector3.zero);

        // Stalker
        CreateOrReplacePrefab("VFX_TremorShockwave_Wave", PrimitiveType.Cube, new Vector3(6.5f, 0.2f, 1.2f), false, Vector3.zero);

        // Colossus
        CreateOrReplacePrefab("Structure_ArenaDesolation_Ring", PrimitiveType.Cylinder, new Vector3(9f, 1.2f, 9f), false, Vector3.zero);

        // CyberNinja
        CreateOrReplacePrefab("VFX_RazorBlade_Fan", PrimitiveType.Cube, new Vector3(0.8f, 0.05f, 0.25f), false, new Vector3(0f, 0f, 25f));

        // Vanguard
        CreateOrReplacePrefab("VFX_SearingWave_Line", PrimitiveType.Capsule, new Vector3(0.7f, 2.4f, 0.7f), true, new Vector3(0f, 0f, 90f));

        // RelentlessHook
        CreateOrReplacePrefab("VFX_HarpoonTether_Hook", PrimitiveType.Cylinder, new Vector3(0.25f, 0.8f, 0.25f), false, new Vector3(90f, 0f, 0f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void AssignPrefabsToManagers()
    {
        GameObject bioticDart = LoadPrefab("VFX_BioticDart_Projectile");
        GameObject immortalityField = LoadPrefab("VFX_ImmortalityField_Zone");
        GameObject blossom = LoadPrefab("VFX_BlossomMend_Projectile");
        GameObject lifeGripBubble = LoadPrefab("VFX_LifeGrip_Bubble");
        GameObject talisman = LoadPrefab("VFX_PurifyingTalisman_Splash");
        GameObject echoShield = LoadPrefab("VFX_EchoingShield_Zone");
        GameObject sentry = LoadPrefab("Structure_SentryTurret_Basic");
        GameObject minion = LoadPrefab("Unit_TrackingMinion_Basic");
        GameObject molten = LoadPrefab("Hazard_MoltenCore_Pool");
        GameObject tremor = LoadPrefab("VFX_TremorShockwave_Wave");
        GameObject ring = LoadPrefab("Structure_ArenaDesolation_Ring");
        GameObject blade = LoadPrefab("VFX_RazorBlade_Fan");
        GameObject searing = LoadPrefab("VFX_SearingWave_Line");
        GameObject hook = LoadPrefab("VFX_HarpoonTether_Hook");

        SurvivorClassManager[] survivors = Object.FindObjectsByType<SurvivorClassManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < survivors.Length; i++)
        {
            SurvivorClassManager manager = survivors[i];
            if (manager == null)
            {
                continue;
            }

            Undo.RecordObject(manager, "Assign Survivor ability prefabs");
            manager.bioticDartProjectilePrefab = bioticDart;
            manager.immortalityFieldPrefab = immortalityField;
            manager.healingBlossomProjectilePrefab = blossom;
            manager.lifeGripBubblePrefab = lifeGripBubble;
            manager.suzuProjectilePrefab = talisman;
            manager.echoingShieldZonePrefab = echoShield;
            EditorUtility.SetDirty(manager);
        }

        PredatorClassManager[] predators = Object.FindObjectsByType<PredatorClassManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < predators.Length; i++)
        {
            PredatorClassManager manager = predators[i];
            if (manager == null)
            {
                continue;
            }

            Undo.RecordObject(manager, "Assign Predator ability prefabs");
            manager.sentryTurretPrefab = sentry;
            manager.minionPrefab = minion;
            manager.moltenHazardPrefab = molten;
            manager.tremorShockwavePrefab = tremor;
            manager.ringBarrierPrefab = ring;
            manager.shurikenProjectilePrefab = blade;
            manager.fireStrikeProjectilePrefab = searing;
            manager.hookProjectilePrefab = hook;
            EditorUtility.SetDirty(manager);
        }

        Debug.Log("[ManiaClassPrefabBuilder] Assigned generated prefabs to SurvivorClassManager and PredatorClassManager instances in open scenes.");
    }

    private static void AssignPrefabsToPrefabAssets()
    {
        GameObject bioticDart = LoadPrefab("VFX_BioticDart_Projectile");
        GameObject immortalityField = LoadPrefab("VFX_ImmortalityField_Zone");
        GameObject blossom = LoadPrefab("VFX_BlossomMend_Projectile");
        GameObject lifeGripBubble = LoadPrefab("VFX_LifeGrip_Bubble");
        GameObject talisman = LoadPrefab("VFX_PurifyingTalisman_Splash");
        GameObject echoShield = LoadPrefab("VFX_EchoingShield_Zone");
        GameObject sentry = LoadPrefab("Structure_SentryTurret_Basic");
        GameObject minion = LoadPrefab("Unit_TrackingMinion_Basic");
        GameObject molten = LoadPrefab("Hazard_MoltenCore_Pool");
        GameObject tremor = LoadPrefab("VFX_TremorShockwave_Wave");
        GameObject ring = LoadPrefab("Structure_ArenaDesolation_Ring");
        GameObject blade = LoadPrefab("VFX_RazorBlade_Fan");
        GameObject searing = LoadPrefab("VFX_SearingWave_Line");
        GameObject hook = LoadPrefab("VFX_HarpoonTether_Hook");

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/ManiaSurvival/Prefabs" });
        int survivorPrefabCount = 0;
        int predatorPrefabCount = 0;
        int survivorAiAttachedCount = 0;
        bool medicPrefabFound = false;
        bool wardenPrefabFound = false;
        bool weaverPrefabFound = false;

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            if (string.IsNullOrEmpty(assetPath))
            {
                continue;
            }

            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(assetPath);
                bool modified = false;

                SurvivorClassManager survivor = root.GetComponentInChildren<SurvivorClassManager>(true);
                if (survivor != null)
                {
                    survivor.bioticDartProjectilePrefab = bioticDart;
                    survivor.immortalityFieldPrefab = immortalityField;
                    survivor.healingBlossomProjectilePrefab = blossom;
                    survivor.lifeGripBubblePrefab = lifeGripBubble;
                    survivor.suzuProjectilePrefab = talisman;
                    survivor.echoingShieldZonePrefab = echoShield;
                    EditorUtility.SetDirty(survivor);
                    modified = true;
                    survivorPrefabCount++;

                    if (IsMasterSurvivorPrefab(assetPath))
                    {
                        string lowerPath = assetPath.ToLowerInvariant();
                        medicPrefabFound |= lowerPath.Contains("medic");
                        wardenPrefabFound |= lowerPath.Contains("warden");
                        weaverPrefabFound |= lowerPath.Contains("weaver");

                        OfflineSurvivorBotAI ai = survivor.GetComponent<OfflineSurvivorBotAI>();
                        if (ai == null)
                        {
                            ai = survivor.gameObject.AddComponent<OfflineSurvivorBotAI>();
                            EditorUtility.SetDirty(ai);
                            EditorUtility.SetDirty(survivor.gameObject);
                            modified = true;
                            survivorAiAttachedCount++;
                            Debug.Log("[ManiaClassPrefabBuilder] Added OfflineSurvivorBotAI to prefab: " + assetPath);
                        }
                    }
                }

                PredatorClassManager predator = root.GetComponentInChildren<PredatorClassManager>(true);
                if (predator != null)
                {
                    predator.sentryTurretPrefab = sentry;
                    predator.minionPrefab = minion;
                    predator.moltenHazardPrefab = molten;
                    predator.tremorShockwavePrefab = tremor;
                    predator.ringBarrierPrefab = ring;
                    predator.shurikenProjectilePrefab = blade;
                    predator.fireStrikeProjectilePrefab = searing;
                    predator.hookProjectilePrefab = hook;
                    EditorUtility.SetDirty(predator);
                    modified = true;
                    predatorPrefabCount++;
                }

                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                }
            }
            finally
            {
                if (root != null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        Debug.Log("[ManiaClassPrefabBuilder] Assigned generated prefabs into prefab assets. " +
                  "Survivor prefabs updated: " + survivorPrefabCount +
                  ", Predator prefabs updated: " + predatorPrefabCount +
                  ", OfflineSurvivorBotAI attached: " + survivorAiAttachedCount + ".");

        if (!medicPrefabFound || !wardenPrefabFound || !weaverPrefabFound)
        {
            Debug.LogWarning("[ManiaClassPrefabBuilder] Could not confirm all master survivor prefabs (Medic/Warden/Weaver) by name during prefab asset scan. " +
                             "Detected -> Medic: " + medicPrefabFound + ", Warden: " + wardenPrefabFound + ", Weaver: " + weaverPrefabFound + ".");
        }
    }

    private static bool IsMasterSurvivorPrefab(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return false;
        }

        string lowerPath = assetPath.ToLowerInvariant();
        return lowerPath.Contains("medic")
            || lowerPath.Contains("warden")
            || lowerPath.Contains("weaver");
    }

    private static void RunCodeCleanupAudit()
    {
        string localRolePath = "Assets/ManiaSurvival/Scripts/Core/LocalRoleController.cs";
        string survivorPath = "Assets/ManiaSurvival/Scripts/Player/SurvivorClassManager.cs";
        string predatorPath = "Assets/ManiaSurvival/Scripts/Monster/PredatorClassManager.cs";

        AuditFileExists(localRolePath);
        AuditFileExists(survivorPath);
        AuditFileExists(predatorPath);

        // Non-destructive audit for deprecated single-key tester logic.
        string localRoleText = File.ReadAllText(localRolePath);
        bool hasDirectKeyboardTester = localRoleText.Contains("Keyboard.current") && localRoleText.Contains("PressPredatorAbility");
        if (hasDirectKeyboardTester)
        {
            Debug.LogWarning("[ManiaClassPrefabBuilder] LocalRoleController appears to contain direct keyboard tester logic. Review manually to keep UI as exclusive authority.");
        }
        else
        {
            Debug.Log("[ManiaClassPrefabBuilder] LocalRoleController audit passed: no direct tester-key override block detected for predator class ability routing.");
        }

        // Animation hash audit.
        string predatorText = File.ReadAllText(predatorPath);
        bool predatorHasHashCache =
            predatorText.Contains("Animator.StringToHash(\"MeleeAttack\")") &&
            predatorText.Contains("Animator.StringToHash(\"Ability1\")") &&
            predatorText.Contains("Animator.StringToHash(\"Ability2\")") &&
            predatorText.Contains("Animator.StringToHash(\"Ability3\")") &&
            predatorText.Contains("Animator.StringToHash(\"Ultimate\")");

        string survivorText = File.ReadAllText(survivorPath);
        bool survivorHasHashCache =
            survivorText.Contains("Animator.StringToHash(\"Attack\")") &&
            survivorText.Contains("Animator.StringToHash(\"Ability2\")") &&
            survivorText.Contains("Animator.StringToHash(\"Ability3\")") &&
            survivorText.Contains("Animator.StringToHash(\"Ultimate\")");

        if (!predatorHasHashCache || !survivorHasHashCache)
        {
            Debug.LogWarning("[ManiaClassPrefabBuilder] Animator hash cache audit flagged missing entries. Verify class managers are using cached hashes for trigger routing.");
        }
        else
        {
            Debug.Log("[ManiaClassPrefabBuilder] Animator hash routing audit passed.");
        }
    }

    private static void CreateOrReplacePrefab(
        string prefabName,
        PrimitiveType primitiveType,
        Vector3 localScale,
        bool colliderIsTrigger,
        Vector3 localEuler)
    {
        GameObject temp = GameObject.CreatePrimitive(primitiveType);
        temp.name = prefabName;
        temp.transform.localScale = localScale;
        temp.transform.localEulerAngles = localEuler;

        Collider col = temp.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = colliderIsTrigger;
        }

        string assetPath = $"{PrefabFolder}/{prefabName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(temp, assetPath);

        // Remove temporary scene object immediately.
        Object.DestroyImmediate(temp);
    }

    private static GameObject LoadPrefab(string prefabName)
    {
        string path = $"{PrefabFolder}/{prefabName}.prefab";
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    private static void AuditFileExists(string assetPath)
    {
        if (!File.Exists(assetPath))
        {
            Debug.LogWarning("[ManiaClassPrefabBuilder] Audit target missing: " + assetPath);
        }
    }
}
