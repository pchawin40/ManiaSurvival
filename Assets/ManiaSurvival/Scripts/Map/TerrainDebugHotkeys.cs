using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Dev-only hotkeys for spawning test terrain props near the local player.
/// </summary>
[DisallowMultipleComponent]
public class TerrainDebugHotkeys : MonoBehaviour
{
    [Header("Debug Hotkeys")]
    public bool enableTerrainDebugHotkeys;

    private DynamicTerrainSpawner spawner;

    private void Awake()
    {
        spawner = DynamicTerrainSpawner.GetOrCreate();
    }

    private void Update()
    {
        if (!enableTerrainDebugHotkeys || Keyboard.current == null)
        {
            return;
        }

        if (WasPressed(Key.F5))
        {
            SpawnNearPlayer((pos, rot) =>
            {
                spawner.SpawnWall(pos, rot, 2.5f, 12f);
                Debug.Log("[TerrainDebug] Spawned test wall");
            });
        }

        if (WasPressed(Key.F6))
        {
            SpawnNearPlayer((pos, rot) =>
            {
                spawner.SpawnRamp(pos, rot, 14f);
                Debug.Log("[TerrainDebug] Spawned test ramp");
            });
        }

        if (WasPressed(Key.F7))
        {
            SpawnNearPlayer((pos, _) =>
            {
                spawner.SpawnFireZone(pos, 2.5f, 8f, 3f);
                Debug.Log("[TerrainDebug] Spawned test fire zone");
            });
        }

        if (WasPressed(Key.F8))
        {
            SpawnNearPlayer((pos, rot) =>
            {
                spawner.SpawnJumpPad(pos, 20f);
                Debug.Log("[TerrainDebug] Spawned test jump pad");
            });
        }
    }

    private delegate void SpawnAction(Vector3 position, Quaternion rotation);

    private void SpawnNearPlayer(SpawnAction action)
    {
        if (spawner == null)
        {
            spawner = DynamicTerrainSpawner.GetOrCreate();
        }

        Transform player = FindLocalPlayerTransform();
        Vector3 origin = player != null ? player.position : Vector3.zero;
        Vector3 forward = player != null ? player.forward : Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.001f)
        {
            forward = Vector3.forward;
        }

        Vector3 spawnPos = origin + forward.normalized * 2.5f;
        action(spawnPos, Quaternion.LookRotation(forward));
    }

    private static Transform FindLocalPlayerTransform()
    {
        SurvivorMovement survivor = FindFirstObjectByType<SurvivorMovement>();
        if (survivor != null)
        {
            return survivor.transform;
        }

        MonsterPlayerMovement predator = FindFirstObjectByType<MonsterPlayerMovement>();
        if (predator != null)
        {
            return predator.transform;
        }

        UnitHealth[] units = FindObjectsByType<UnitHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            UnitHealth unit = units[i];
            if (unit != null && !unit.IsDead)
            {
                return unit.transform;
            }
        }

        return null;
    }

    private static bool WasPressed(Key key)
    {
        return Keyboard.current[key].wasPressedThisFrame;
    }
}
