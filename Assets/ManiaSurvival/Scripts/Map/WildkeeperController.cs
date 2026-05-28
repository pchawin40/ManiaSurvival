using UnityEngine;

[DisallowMultipleComponent]
public class WildkeeperController : MonoBehaviour
{
    private const float unitExclusionRadius = 2.5f;

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
    public float treeSpawnRadius = 8f;
    public float minTreeSpacing = 2f;
    [Tooltip("Fallback Y used only when groundLayerMask is not assigned.")]
    public float treeSpawnHeight = 0f;
    public int maxSpawnAttempts = 20;
    public LayerMask spawnBlockerMask;

    [Header("Ground Raycast")]
    [Tooltip("Layers that count as valid ground. Assign your terrain/ground layer here. If empty, falls back to treeSpawnHeight.")]
    public LayerMask groundLayerMask;
    [Tooltip("How far above the candidate XZ point the downward raycast starts.")]
    public float groundRaycastStartHeight = 15f;
    [Tooltip("Layers that are hazards (WaterZone, HellPit, etc). Trees will not spawn here.")]
    public LayerMask hazardLayerMask;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    [Header("Game Manager")]
    public ManiaGameManager gameManager;

    private enum WildkeeperState
    {
        Waiting,
        Moving
    }

    private WildkeeperState state = WildkeeperState.Waiting;
    private Vector3 moveTarget;
    private float stateEndTime;
    private float nextTreeSpawnTime;

    private void Start()
    {
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

            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = new Vector3(moveTarget.x, currentPosition.y, moveTarget.z);
        Vector3 toTarget = targetPosition - currentPosition;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= 0.04f)
        {
            StartWaiting();
            return;
        }

        Vector3 moveStep = toTarget.normalized * moveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(currentPosition, targetPosition, moveStep.magnitude);

        if (toTarget.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(toTarget.normalized);
        }
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
            LogTreeSkip("max trees reached");
            return;
        }

        for (int attempt = 0; attempt < Mathf.Max(1, maxSpawnAttempts); attempt++)
        {
            Vector3 candidate = GetRandomTreeSpawnPoint();

            if (enableDebugLogs)
            {
                Debug.Log("[Wildkeeper] tree spawn candidate XZ: " + candidate);
            }

            if (!TryFindGroundPosition(candidate, out Vector3 groundPosition))
            {
                if (enableDebugLogs)
                {
                    Debug.Log("[Wildkeeper] tree spawn rejected: no ground found below candidate");
                }

                continue;
            }

            if (!IsSpawnPointValid(groundPosition, out string failReason))
            {
                if (enableDebugLogs)
                {
                    Debug.Log("[Wildkeeper] tree spawn rejected: " + failReason);
                }

                continue;
            }

            if (enableDebugLogs)
            {
                Debug.Log("[Wildkeeper] tree spawned at: " + groundPosition);
            }

            NeutralTree spawnedTree = Instantiate(treePrefab, groundPosition, Quaternion.identity, treeParent);
            spawnedTree.name = "NeutralTree_Spawned";
            spawnedTree.SetBlocksMovement(true);
            return;
        }

        LogTreeSkip("no valid tree position found after " + Mathf.Max(1, maxSpawnAttempts) + " attempts");
    }

    private Vector3 GetRandomPointInMap()
    {
        float halfWidth = Mathf.Max(0f, mapSize.x * 0.5f);
        float halfLength = Mathf.Max(0f, mapSize.y * 0.5f);
        float x = Random.Range(mapCenter.x - halfWidth, mapCenter.x + halfWidth);
        float z = Random.Range(mapCenter.z - halfLength, mapCenter.z + halfLength);
        return new Vector3(x, transform.position.y, z);
    }

    private Vector3 GetRandomTreeSpawnPoint()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(0f, Mathf.Max(0f, treeSpawnRadius));
        Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        Vector3 candidate = transform.position + offset;
        candidate = ClampToMap(candidate);
        // Y is intentionally left as-is; TryFindGroundPosition will resolve the correct ground Y.
        return candidate;
    }

    /// <summary>
    /// Shoots a downward ray from above <paramref name="candidate"/> to locate the ground.
    /// When <see cref="groundLayerMask"/> is assigned the ray must hit that layer.
    /// Falls back to <see cref="treeSpawnHeight"/> when no mask is configured.
    /// Returns false (no spawn) when a mask is set but no hit is found.
    /// </summary>
    private bool TryFindGroundPosition(Vector3 candidate, out Vector3 groundPosition)
    {
        groundPosition = candidate;

        if (groundLayerMask.value == 0)
        {
            // No ground mask configured — use the fixed treeSpawnHeight as fallback.
            groundPosition.y = treeSpawnHeight;
            return true;
        }

        float rayStart = groundRaycastStartHeight > 0f ? groundRaycastStartHeight : 15f;
        Vector3 origin = new Vector3(candidate.x, candidate.y + rayStart, candidate.z);
        float maxDist = rayStart + 30f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDist, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            groundPosition = hit.point;
            return true;
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

    private bool IsSpawnPointValid(Vector3 position, out string failReason)
    {
        failReason = string.Empty;

        // Reject positions inside hazard volumes (WaterZone, HellPit, etc.).
        if (hazardLayerMask.value != 0 && Physics.CheckSphere(position, 0.5f, hazardLayerMask, QueryTriggerInteraction.Collide))
        {
            failReason = "inside hazard zone";
            return false;
        }

        NeutralTree[] existingTrees = FindObjectsByType<NeutralTree>(FindObjectsSortMode.None);
        for (int i = 0; i < existingTrees.Length; i++)
        {
            NeutralTree tree = existingTrees[i];
            if (tree == null)
            {
                continue;
            }

            if (Vector3.Distance(tree.transform.position, position) < minTreeSpacing)
            {
                failReason = "too close to existing tree";
                return false;
            }
        }

        Collider[] nearbyUnits = Physics.OverlapSphere(position, unitExclusionRadius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < nearbyUnits.Length; i++)
        {
            UnitHealth unitHealth = nearbyUnits[i].GetComponentInParent<UnitHealth>();
            if (unitHealth == null || unitHealth.IsDead)
            {
                continue;
            }

            if (unitHealth.CompareTag("Survivor") || unitHealth.CompareTag("Monster"))
            {
                failReason = "too close to player/monster";
                return false;
            }
        }

        if (spawnBlockerMask.value != 0)
        {
            float solidCheckRadius = Mathf.Max(0.25f, minTreeSpacing * 0.5f);
            Collider[] solidColliders = Physics.OverlapSphere(position, solidCheckRadius, spawnBlockerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < solidColliders.Length; i++)
            {
                Collider collider = solidColliders[i];
                if (collider == null || collider.isTrigger)
                {
                    continue;
                }

                if (collider.GetComponentInParent<NeutralTree>() != null)
                {
                    continue;
                }

                if (collider.GetComponentInParent<UnitHealth>() != null)
                {
                    continue;
                }

                failReason = "blocked by " + collider.name;
                return false;
            }
        }

        return true;
    }

    private void LogTreeSkip(string reason)
    {
        if (enableDebugLogs)
        {
            Debug.Log("tree spawn skipped: " + reason);
        }
    }
}
