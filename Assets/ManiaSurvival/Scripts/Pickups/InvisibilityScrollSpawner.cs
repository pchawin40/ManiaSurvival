using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns InvisibilityScrollPickup prefabs at random valid map positions
/// a few times per match, on a pre-generated randomised schedule.
/// Attach to any persistent scene object (e.g. GameManager or a dedicated Spawner GO).
/// </summary>
[DisallowMultipleComponent]
public class InvisibilityScrollSpawner : MonoBehaviour
{
    [Header("Spawn Schedule")]
    [Tooltip("Prefab that has an InvisibilityScrollPickup component.")]
    public GameObject scrollPrefab;

    [Tooltip("Number of scroll spawns per match.")]
    public int totalSpawnsPerMatch = 3;

    [Tooltip("Earliest time (seconds after match start) that the first scroll can spawn.")]
    public float earliestFirstSpawn = 20f;

    [Tooltip("Latest time (seconds after match start) that the last scroll can spawn.")]
    public float latestLastSpawn = 160f;

    [Header("Active Cap")]
    [Tooltip("Maximum number of scrolls on the map at the same time. Spawn is skipped if this is reached.")]
    public int maxActiveScrolls = 1;

    [Header("Spawn Settings")]
    public MapSpawnSettings spawnSettings;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private ManiaGameManager gameManager;
    private ManiaGameState lastState;

    private readonly List<float> spawnTimes = new List<float>();
    private int nextSpawnIndex;

    private readonly List<GameObject> activeScrolls = new List<GameObject>();

    private void Awake()
    {
        gameManager = ManiaGameManager.Instance;
    }

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<ManiaGameManager>();
        }

        lastState = gameManager != null ? gameManager.State : ManiaGameState.WaitingToStart;
    }

    private void Update()
    {
        if (gameManager == null)
        {
            return;
        }

        // Detect when a new round begins and regenerate the schedule.
        if (gameManager.State == ManiaGameState.Playing && lastState != ManiaGameState.Playing)
        {
            GenerateSpawnSchedule();
        }

        lastState = gameManager.State;

        if (!gameManager.IsPlaying)
        {
            return;
        }

        if (nextSpawnIndex >= spawnTimes.Count)
        {
            return;
        }

        // Elapsed = total round time minus what remains.
        float elapsed = gameManager.roundDuration - gameManager.TimeRemaining;

        if (elapsed < spawnTimes[nextSpawnIndex])
        {
            return;
        }

        TrySpawnScroll();
        nextSpawnIndex++;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void GenerateSpawnSchedule()
    {
        spawnTimes.Clear();
        nextSpawnIndex = 0;

        float window = Mathf.Max(0f, latestLastSpawn - earliestFirstSpawn);

        for (int i = 0; i < Mathf.Max(1, totalSpawnsPerMatch); i++)
        {
            spawnTimes.Add(earliestFirstSpawn + Random.Range(0f, window));
        }

        spawnTimes.Sort();

        if (enableDebugLogs)
        {
            for (int i = 0; i < spawnTimes.Count; i++)
            {
                Debug.Log("[ScrollSpawner] Scroll #" + (i + 1) +
                          " scheduled at +" + spawnTimes[i].ToString("0.0") + "s");
            }
        }
    }

    private void TrySpawnScroll()
    {
        if (scrollPrefab == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[ScrollSpawner] Scroll prefab not assigned — assign it in the Inspector.");
            }

            return;
        }

        // Purge destroyed references before checking the cap.
        activeScrolls.RemoveAll(s => s == null);

        if (activeScrolls.Count >= maxActiveScrolls)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[ScrollSpawner] Spawn skipped: " + activeScrolls.Count +
                          "/" + maxActiveScrolls + " active scrolls already on map.");
            }

            return;
        }

        if (!MapSpawnUtility.TryGetValidPosition(spawnSettings, out Vector3 pos))
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[ScrollSpawner] Spawn skipped: no valid position found. Check spawnSettings.");
            }

            return;
        }

        GameObject scroll = Instantiate(scrollPrefab, pos, Quaternion.identity);
        activeScrolls.Add(scroll);

        if (enableDebugLogs)
        {
            Debug.Log("[ScrollSpawner] Invisibility scroll spawned at " + pos.ToString("F2") +
                      " (" + activeScrolls.Count + "/" + maxActiveScrolls + " active)");
        }
    }
}
