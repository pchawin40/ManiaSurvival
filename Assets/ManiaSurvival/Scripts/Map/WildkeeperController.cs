using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class WildkeeperController : MonoBehaviour
{
    private const float unitExclusionRadius = 2.5f;

    // Maximum seconds the NPC spends trying to reach a target before
    // giving up and picking a new one (prevents permanent wall-sticking).
    private const float maxMoveTimeout = 8f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float waitTimeMin = 1f;
    public float waitTimeMax = 4f;
    public Vector3 mapCenter;
    public Vector2 mapSize = new Vector2(40f, 40f);

    [Header("Trees")]
    public NeutralTree treePrefab;
    public Transform treeParent;
    public int maxTrees = 20;
    public float treeSpawnIntervalMin = 8f;
    public float treeSpawnIntervalMax = 15f;
    public float minTreeSpacing = 2f;
    public int maxSpawnAttempts = 20;

    [Header("Tree Patch")]
    [Tooltip("Minimum trees spawned per patch attempt.")]
    public int treePatchMinCount = 4;
    [Tooltip("Maximum trees spawned per patch attempt.")]
    public int treePatchMaxCount = 6;
    [Tooltip("Radius within which individual trees scatter from the patch centre.")]
    public float treePatchRadius = 3f;

    [Header("Spawn Validation")]
    [Tooltip("How far inward from the map edge spawn candidates must be.")]
    public float innerMapMargin = 3f;
    [Tooltip("Layers that count as valid ground. Leave empty to skip downward raycast.")]
    public LayerMask groundLayerMask;
    [Tooltip("How far above the candidate the downward raycast starts.")]
    public float groundRaycastStartHeight = 15f;
    [Tooltip("Layers that are hazards (WaterZone, HellPit). Trees will not spawn here.")]
    public LayerMask hazardLayerMask;
    [Tooltip("Layers for solid blockers (walls). Trees will not spawn inside them.")]
    public LayerMask spawnBlockerMask;
    [Tooltip("Radius checked for NoSpawnZone components around each candidate. Set 0 to disable.")]
    public float noSpawnZoneCheckRadius = 1.5f;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    [Header("Game Manager")]
    public ManiaGameManager gameManager;

    private enum WildkeeperState
    {
        Waiting,
        Moving
    }

    private CharacterController _cc;
    private float _verticalVelocity;

    private WildkeeperState state = WildkeeperState.Waiting;
    private Vector3 moveTarget;
    private float stateEndTime;
    private float nextTreeSpawnTime;
    private float moveStartTime;

    private void Start()
    {
        _cc = GetComponent<CharacterController>();

        if (gameManager == null)
        {
            gameManager = ManiaGameManager.Instance;
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<ManiaGameManager>();
        }

        StartWaiting();
        ScheduleNextTreeSpawn();
    }

    private void Update()
    {
        if (!IsGameplayActive())
        {
            return;
        }

        UpdateMovement();
        UpdateTreeSpawning();
    }

    private bool IsGameplayActive()
    {
        if (gameManager == null)
        {
            gameManager = ManiaGameManager.Instance;
        }

        return gameManager != null && gameManager.IsPlaying;
    }

    private void UpdateMovement()
    {
        if (state == WildkeeperState.Waiting)
        {
            if (Time.time >= stateEndTime)
            {
                StartMovingToRandomPoint();
            }

            // Still apply gravity while waiting so the NPC stays on the ground.
            ApplyGravityOnly();
            return;
        }

        // Timeout: if the NPC has been trying to reach this target too long
        // (e.g. blocked by a wall on all sides), give up and pick a new destination.
        if (Time.time - moveStartTime > maxMoveTimeout)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[Wildkeeper] Move timed out — picking new target.");
            }

            StartWaiting();
            return;
        }

        Vector3 toTarget = new Vector3(
            moveTarget.x - transform.position.x,
            0f,
            moveTarget.z - transform.position.z);

        if (toTarget.sqrMagnitude <= 0.04f)
        {
            StartWaiting();
            return;
        }

        // Gravity: stay grounded; apply downward acceleration when airborne.
        if (_cc.isGrounded)
        {
            _verticalVelocity = -0.5f; // small constant push keeps isGrounded reliable
        }
        else
        {
            _verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }

        Vector3 horizontal = toTarget.normalized * moveSpeed;
        Vector3 motion = new Vector3(horizontal.x, _verticalVelocity, horizontal.z) * Time.deltaTime;
        _cc.Move(motion);

        if (toTarget.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(toTarget.normalized);
        }
    }

    private void ApplyGravityOnly()
    {
        if (_cc == null)
        {
            return;
        }

        if (_cc.isGrounded)
        {
            _verticalVelocity = -0.5f;
        }
        else
        {
            _verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }

        _cc.Move(new Vector3(0f, _verticalVelocity * Time.deltaTime, 0f));
    }

    private void UpdateTreeSpawning()
    {
        if (Time.time < nextTreeSpawnTime)
        {
            return;
        }

        if (enableDebugLogs)
        {
            Debug.Log("tree spawn attempt begins");
        }

        SpawnTree();
        ScheduleNextTreeSpawn();
    }

    private void StartWaiting()
    {
        state = WildkeeperState.Waiting;
        float minWait = Mathf.Min(waitTimeMin, waitTimeMax);
        float maxWait = Mathf.Max(waitTimeMin, waitTimeMax);
        stateEndTime = Time.time + Random.Range(minWait, maxWait);

        if (enableDebugLogs)
        {
            Debug.Log("Wildkeeper waiting");
        }
    }

    private void StartMovingToRandomPoint()
    {
        state = WildkeeperState.Moving;
        moveTarget = GetRandomPointInMap();
        moveStartTime = Time.time;

        if (enableDebugLogs)
        {
            Debug.Log("Wildkeeper moving");
        }
    }

    private void ScheduleNextTreeSpawn()
    {
        float minInterval = Mathf.Min(treeSpawnIntervalMin, treeSpawnIntervalMax);
        float maxInterval = Mathf.Max(treeSpawnIntervalMin, treeSpawnIntervalMax);
        nextTreeSpawnTime = Time.time + Random.Range(minInterval, maxInterval);

        if (enableDebugLogs)
        {
            Debug.Log("next tree spawn scheduled at " + nextTreeSpawnTime.ToString("0.00"));
        }
    }

    private void SpawnTree()
    {
        if (treePrefab == null)
        {
            LogTreeSkip("no tree prefab assigned");
            return;
        }

        NeutralTree[] existingTrees = FindObjectsByType<NeutralTree>(FindObjectsSortMode.None);
        if (existingTrees.Length >= maxTrees)
        {
            LogTreeSkip("max trees reached (" + existingTrees.Length + "/" + maxTrees + ")");
            return;
        }

        MapSpawnSettings settings = BuildSpawnSettings();

        // Find a valid patch centre well inside the map.
        if (!MapSpawnUtility.TryGetValidPosition(settings, out Vector3 patchCenter))
        {
            LogTreeSkip("no valid patch centre found — check groundLayerMask and innerMapMargin");
            return;
        }

        int targetCount = Random.Range(
            Mathf.Max(1, treePatchMinCount),
            Mathf.Max(1, treePatchMaxCount) + 1);

        int spawned = 0;
        // Track positions we already placed this batch so trees don't stack.
        Vector3[] batchPositions = new Vector3[targetCount];

        for (int i = 0; i < targetCount; i++)
        {
            if (existingTrees.Length + spawned >= maxTrees)
            {
                break;
            }

            if (!MapSpawnUtility.TryGetValidPositionNear(patchCenter, treePatchRadius, settings, out Vector3 treePos))
            {
                if (enableDebugLogs)
                {
                    Debug.Log("[Wildkeeper] patch tree " + (i + 1) + ": no valid position found, skipping.");
                }

                continue;
            }

            // Reject if too close to an already-existing tree or one placed this batch.
            if (IsTooCloseToTrees(treePos, existingTrees) || IsTooCloseToBatch(treePos, batchPositions, spawned))
            {
                if (enableDebugLogs)
                {
                    Debug.Log("[Wildkeeper] patch tree " + (i + 1) + ": too close to neighbouring tree, skipping.");
                }

                continue;
            }

            NeutralTree spawnedTree = Instantiate(treePrefab, treePos, Quaternion.identity, treeParent);
            spawnedTree.name = "NeutralTree_Spawned";
            spawnedTree.SetBlocksMovement(true);
            batchPositions[spawned] = treePos;
            spawned++;
        }

        if (enableDebugLogs)
        {
            Debug.Log("[Wildkeeper] Tree patch: spawned " + spawned + "/" + targetCount +
                      " trees near " + patchCenter.ToString("F2"));
        }
    }

    private Vector3 GetRandomPointInMap()
    {
        float halfWidth = Mathf.Max(0f, mapSize.x * 0.5f);
        float halfLength = Mathf.Max(0f, mapSize.y * 0.5f);
        float x = Random.Range(mapCenter.x - halfWidth, mapCenter.x + halfWidth);
        float z = Random.Range(mapCenter.z - halfLength, mapCenter.z + halfLength);
        return new Vector3(x, transform.position.y, z);
    }

    /// <summary>
    /// Builds a MapSpawnSettings from this component's Inspector fields.
    /// The patch centre must land inside the map minus innerMapMargin on every side.
    /// </summary>
    private MapSpawnSettings BuildSpawnSettings()
    {
        return new MapSpawnSettings
        {
            mapCenter = mapCenter,
            mapSize = mapSize,
            innerMargin = innerMapMargin,
            groundLayerMask = groundLayerMask,
            groundRaycastStartHeight = groundRaycastStartHeight,
            hazardLayerMask = hazardLayerMask,
            blockerLayerMask = spawnBlockerMask,
            overlapCheckRadius = Mathf.Max(0.25f, minTreeSpacing * 0.5f),
            minDistanceFromUnits = unitExclusionRadius,
            maxAttempts = Mathf.Max(1, maxSpawnAttempts),
            noSpawnZoneCheckRadius = noSpawnZoneCheckRadius,
            noSpawnFilter = NoSpawnFilter.Trees,
        };
    }

    private bool IsTooCloseToTrees(Vector3 pos, NeutralTree[] trees)
    {
        for (int i = 0; i < trees.Length; i++)
        {
            if (trees[i] == null)
            {
                continue;
            }

            if (Vector3.Distance(trees[i].transform.position, pos) < minTreeSpacing)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsTooCloseToBatch(Vector3 pos, Vector3[] batch, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (Vector3.Distance(batch[i], pos) < minTreeSpacing)
            {
                return true;
            }
        }

        return false;
    }

    private Vector3 ClampToMap(Vector3 position)
    {
        float halfWidth = Mathf.Max(0f, mapSize.x * 0.5f);
        float halfLength = Mathf.Max(0f, mapSize.y * 0.5f);
        float x = Mathf.Clamp(position.x, mapCenter.x - halfWidth, mapCenter.x + halfWidth);
        float z = Mathf.Clamp(position.z, mapCenter.z - halfLength, mapCenter.z + halfLength);
        return new Vector3(x, position.y, z);
    }

    private void LogTreeSkip(string reason)
    {
        if (enableDebugLogs)
        {
            Debug.Log("tree spawn skipped: " + reason);
        }
    }
}
