using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class UnitManaSceneSetup
{
    private const string SampleScenePath = "Assets/ManiaSurvival/Scenes/SampleScene.unity";

    [MenuItem("Mania Survival/QA/Ensure Unit Mana On Players")]
    public static void EnsureUnitManaOnSampleScenePlayers()
    {
        EnsureUnitManaOnPrefabs();

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != SampleScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        }

        int added = 0;
        added += EnsureOnTag("Survivor", false);
        added += EnsureOnTag("Monster", true);

        SurvivorMovement[] survivors = Object.FindObjectsByType<SurvivorMovement>(FindObjectsSortMode.None);
        for (int i = 0; i < survivors.Length; i++)
        {
            if (survivors[i] != null)
            {
                added += EnsureOnObject(survivors[i].gameObject, false);
            }
        }

        MonsterPlayerMovement[] monsters = Object.FindObjectsByType<MonsterPlayerMovement>(FindObjectsSortMode.None);
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null)
            {
                added += EnsureOnObject(monsters[i].gameObject, true);
            }
        }

        if (added > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }

        Debug.Log("[MapSetup] Ensured UnitMana on SampleScene players. Added/updated " + added + " component(s).");
    }

    private static void EnsureUnitManaOnPrefabs()
    {
        EnsurePrefab("Assets/ManiaSurvival/Prefabs/Player/Survivor.prefab", false);
        EnsurePrefab("Assets/ManiaSurvival/Prefabs/Monster/Monster.prefab", true);
    }

    private static void EnsurePrefab(string path, bool isPredator)
    {
        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefabRoot == null)
        {
            return;
        }

        GameObject instance = PrefabUtility.LoadPrefabContents(path);
        if (instance.GetComponent<UnitMana>() == null)
        {
            UnitMana.EnsureOn(instance, isPredator);
            PrefabUtility.SaveAsPrefabAsset(instance, path);
        }

        PrefabUtility.UnloadPrefabContents(instance);
    }

    private static int EnsureOnTag(string tag, bool isPredator)
    {
        GameObject[] tagged = GameObject.FindGameObjectsWithTag(tag);
        int count = 0;
        for (int i = 0; i < tagged.Length; i++)
        {
            count += EnsureOnObject(tagged[i], isPredator);
        }

        return count;
    }

    private static int EnsureOnObject(GameObject host, bool isPredator)
    {
        if (host == null)
        {
            return 0;
        }

        UnitMana mana = host.GetComponent<UnitMana>();
        if (mana == null)
        {
            mana = UnitMana.EnsureOn(host, isPredator);
            return mana != null ? 1 : 0;
        }

        mana.manaRegenPerSecond = isPredator ? 6f : 3f;
        return 0;
    }
}
