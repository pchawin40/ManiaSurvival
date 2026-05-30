using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ensures enough AI survivor units exist for solo Monster mode hunts.
/// </summary>
[DisallowMultipleComponent]
public class SoloMonsterSurvivorSpawner : MonoBehaviour
{
    [Header("Count")]
    [Min(1)] public int targetSurvivorCount = 6;

    [Header("Spawn")]
    public GameObject survivorPrefab;
    public Transform spawnParent;
    public float spawnHeight = 1f;
    public float minDistanceBetweenSurvivors = 4f;

    [Header("Map Safety")]
    public Vector2 mapSize = new Vector2(34f, 34f);
    public float innerMargin = 3f;
    public float hellfireAvoidRadius = 8f;
    public float heavenPortalAvoidRadius = 5f;
    public float waterAvoidRadius = 4f;
    public int maxSpawnAttempts = 40;

    [Header("Debug")]
    public bool logSpawns = true;

    private readonly List<GameObject> spawnedSurvivors = new List<GameObject>();

    public void EnsureMonsterModeSurvivors()
    {
        CleanupSpawnList();

        int currentCount = CountActiveSurvivors();
        int toSpawn = Mathf.Max(0, targetSurvivorCount - currentCount);

        if (toSpawn <= 0)
        {
            if (logSpawns)
            {
                Debug.Log("[SoloMonsterSpawner] Already have " + currentCount + " survivors (target " + targetSurvivorCount + ").");
            }

            return;
        }

        GameObject template = ResolveSurvivorTemplate();
        if (template == null)
        {
            Debug.LogWarning("[SoloMonsterSpawner] No survivor prefab/template found. Assign survivorPrefab or place a Survivor in scene.");
            return;
        }

        Transform parent = ResolveSpawnParent();

        for (int i = 0; i < toSpawn; i++)
        {
            if (!TryFindSafeSpawnPosition(out Vector3 spawnPosition))
            {
                Debug.LogWarning("[SoloMonsterSpawner] Could not find safe spawn position for survivor " + (i + 1) + ".");
                continue;
            }

            GameObject instance = Instantiate(template, spawnPosition, Quaternion.identity, parent);
            instance.name = GetNextAiSurvivorName();
            EnsureSurvivorIdentity(instance);

            spawnedSurvivors.Add(instance);

            if (logSpawns)
            {
                Debug.Log("[SoloMonsterSpawner] Spawned " + instance.name + " at " + spawnPosition);
            }
        }

        ManiaGameManager manager = ManiaGameManager.Instance;
        if (manager != null)
        {
            manager.RefreshSurvivorList();
        }

        if (logSpawns)
        {
            Debug.Log("[SoloMonsterSpawner] Active survivors: " + CountActiveSurvivors() + " / " + targetSurvivorCount);
        }
    }

    public void ClearSpawnedSurvivors()
    {
        for (int i = spawnedSurvivors.Count - 1; i >= 0; i--)
        {
            GameObject spawned = spawnedSurvivors[i];
            if (spawned != null)
            {
                Destroy(spawned);
            }
        }

        spawnedSurvivors.Clear();
    }

    private void CleanupSpawnList()
    {
        for (int i = spawnedSurvivors.Count - 1; i >= 0; i--)
        {
            if (spawnedSurvivors[i] == null)
            {
                spawnedSurvivors.RemoveAt(i);
            }
        }
    }

    private int CountActiveSurvivors()
    {
        UnitHealth[] all = FindObjectsByType<UnitHealth>(FindObjectsSortMode.None);
        int count = 0;

        for (int i = 0; i < all.Length; i++)
        {
            UnitHealth health = all[i];
            if (health == null || health.IsDead || !health.CompareTag("Survivor"))
            {
                continue;
            }

            if (health.GetComponent<SurvivorMovement>() == null && health.GetComponent<OfflineSurvivorBotAI>() == null)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private GameObject ResolveSurvivorTemplate()
    {
        if (survivorPrefab != null)
        {
            return survivorPrefab;
        }

        SurvivorMovement localSurvivor = null;
        LocalRoleController roleController = FindFirstObjectByType<LocalRoleController>();
        if (roleController != null)
        {
            localSurvivor = roleController.survivorMovement;
        }

        SurvivorMovement[] survivors = FindObjectsByType<SurvivorMovement>(FindObjectsSortMode.None);
        for (int i = 0; i < survivors.Length; i++)
        {
            SurvivorMovement candidate = survivors[i];
            if (candidate != null && candidate != localSurvivor)
            {
                return candidate.gameObject;
            }
        }

        if (localSurvivor != null)
        {
            return localSurvivor.gameObject;
        }

        return null;
    }

    private Transform ResolveSpawnParent()
    {
        if (spawnParent != null)
        {
            return spawnParent;
        }

        GameObject players = GameObject.Find("Players");
        if (players != null)
        {
            return players.transform;
        }

        return null;
    }

    private string GetNextAiSurvivorName()
    {
        int index = 1;
        while (GameObject.Find("AISurvivor_" + index.ToString("00")) != null)
        {
            index++;
        }

        return "AISurvivor_" + index.ToString("00");
    }

    private void EnsureSurvivorIdentity(GameObject instance)
    {
        instance.tag = "Survivor";

        if (instance.GetComponent<UnitHealth>() == null)
        {
            instance.AddComponent<UnitHealth>();
        }

        if (instance.GetComponent<SurvivorMovement>() == null)
        {
            instance.AddComponent<SurvivorMovement>();
        }

        if (instance.GetComponent<SurvivorClassManager>() == null)
        {
            instance.AddComponent<SurvivorClassManager>();
        }

        if (instance.GetComponent<AbilityController>() == null)
        {
            instance.AddComponent<AbilityController>();
        }

        if (instance.GetComponent<CharacterController>() == null)
        {
            CharacterController controller = instance.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0f, 1f, 0f);
        }

        if (instance.GetComponent<OfflineSurvivorBotAI>() == null)
        {
            instance.AddComponent<OfflineSurvivorBotAI>();
        }
    }

    private bool TryFindSafeSpawnPosition(out Vector3 result)
    {
        Vector3 center = Vector3.zero;
        float halfWidth = mapSize.x * 0.5f - innerMargin;
        float halfDepth = mapSize.y * 0.5f - innerMargin;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            float x = Random.Range(center.x - halfWidth, center.x + halfWidth);
            float z = Random.Range(center.z - halfDepth, center.z + halfDepth);
            Vector3 candidate = new Vector3(x, spawnHeight, z);

            if (!IsSpawnPositionSafe(candidate))
            {
                continue;
            }

            result = candidate;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    private bool IsSpawnPositionSafe(Vector3 position)
    {
        if (ArenaBounds.Instance != null && !ArenaBounds.Instance.IsInside(position))
        {
            return false;
        }

        if (IsInsideHellfire(position) || IsNearHeavenPortal(position) || IsNearWater(position))
        {
            return false;
        }

        if (Physics.CheckSphere(position, 0.75f, ~0, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        UnitHealth[] survivors = FindObjectsByType<UnitHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < survivors.Length; i++)
        {
            UnitHealth survivor = survivors[i];
            if (survivor == null || !survivor.CompareTag("Survivor"))
            {
                continue;
            }

            Vector3 offset = survivor.transform.position - position;
            offset.y = 0f;
            if (offset.sqrMagnitude < minDistanceBetweenSurvivors * minDistanceBetweenSurvivors)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsInsideHellfire(Vector3 worldPos)
    {
        HellfirePitDamageZone[] pits = FindObjectsByType<HellfirePitDamageZone>(FindObjectsSortMode.None);
        for (int i = 0; i < pits.Length; i++)
        {
            HellfirePitDamageZone pit = pits[i];
            if (pit == null)
            {
                continue;
            }

            Vector3 local = pit.transform.InverseTransformPoint(worldPos);
            Vector3 half = Vector3.Scale(pit.localDamageSize, pit.transform.lossyScale) * 0.5f;
            half.x += hellfireAvoidRadius;
            half.z += hellfireAvoidRadius;

            if (Mathf.Abs(local.x - pit.localDamageCenter.x) <= half.x
                && Mathf.Abs(local.z - pit.localDamageCenter.z) <= half.z)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsNearHeavenPortal(Vector3 worldPos)
    {
        HeavenPortal[] portals = FindObjectsByType<HeavenPortal>(FindObjectsSortMode.None);
        for (int i = 0; i < portals.Length; i++)
        {
            HeavenPortal portal = portals[i];
            if (portal == null)
            {
                continue;
            }

            Vector3 portalPos = portal.transform.position;
            if (Vector3.Distance(new Vector3(worldPos.x, 0f, worldPos.z), new Vector3(portalPos.x, 0f, portalPos.z)) <= heavenPortalAvoidRadius)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsNearWater(Vector3 worldPos)
    {
        GameObject water = GameObject.Find("Water");
        if (water == null)
        {
            return false;
        }

        Vector3 waterPos = water.transform.position;
        return Vector3.Distance(new Vector3(worldPos.x, 0f, worldPos.z), new Vector3(waterPos.x, 0f, waterPos.z)) <= waterAvoidRadius;
    }
}
