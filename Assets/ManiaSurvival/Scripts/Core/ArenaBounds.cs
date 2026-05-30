using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ArenaBounds : MonoBehaviour
{
    public static ArenaBounds Instance { get; private set; }

    [Header("Playable XZ Bounds")]
    public float minX = -17f;
    public float maxX = 17f;
    public float minZ = -17f;
    public float maxZ = 17f;

    [Header("Startup")]
    public bool fixBorderCollidersOnAwake = true;

    private static readonly Dictionary<int, float> LastClampLogTimeByUnit = new Dictionary<int, float>();
    private const float ClampLogCooldownSeconds = 10f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapAfterSceneLoad()
    {
        if (FindFirstObjectByType<ArenaBounds>() != null)
        {
            return;
        }

        GameObject go = new GameObject("ArenaBounds");
        go.AddComponent<ArenaBounds>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (fixBorderCollidersOnAwake)
        {
            FixBorderColliders();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool IsInside(Vector3 position)
    {
        return position.x >= minX && position.x <= maxX
            && position.z >= minZ && position.z <= maxZ;
    }

    public Vector3 ClampPosition(Vector3 position)
    {
        return new Vector3(
            Mathf.Clamp(position.x, minX, maxX),
            position.y,
            Mathf.Clamp(position.z, minZ, maxZ));
    }

    public Vector3 GetRandomPointInside()
    {
        float x = Random.Range(minX, maxX);
        float z = Random.Range(minZ, maxZ);
        return new Vector3(x, 0f, z);
    }

    public Vector3 GetRandomPointNear(Vector3 center, float radius)
    {
        Vector2 offset = Random.insideUnitCircle * Mathf.Max(0.1f, radius);
        Vector3 point = center + new Vector3(offset.x, 0f, offset.y);
        return ClampPosition(point);
    }

    public void ClampUnitTransform(Transform unitTransform, string reason)
    {
        if (unitTransform == null)
        {
            return;
        }

        Vector3 clamped = ClampPosition(unitTransform.position);
        if ((clamped - unitTransform.position).sqrMagnitude <= 0.0001f)
        {
            return;
        }

        CharacterController controller = unitTransform.GetComponent<CharacterController>();
        bool controllerWasEnabled = controller != null && controller.enabled;
        if (controller != null && controllerWasEnabled)
        {
            controller.enabled = false;
        }

        unitTransform.position = clamped;

        if (controller != null && controllerWasEnabled && controller.gameObject.activeInHierarchy)
        {
            controller.enabled = true;
        }

        int unitId = unitTransform.GetInstanceID();
        float lastLogTime = 0f;
        LastClampLogTimeByUnit.TryGetValue(unitId, out lastLogTime);
        if (Time.time - lastLogTime >= ClampLogCooldownSeconds)
        {
            LastClampLogTimeByUnit[unitId] = Time.time;
            Debug.Log("[Bounds] Unit returned/clamped inside arena (" + reason + ") on '" + unitTransform.name + "'.");
        }
    }

    private void FixBorderColliders()
    {
        int fixedCount = 0;
        Collider[] allColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);

        for (int i = 0; i < allColliders.Length; i++)
        {
            Collider col = allColliders[i];
            if (col == null)
            {
                continue;
            }

            string objectName = col.gameObject.name;
            if (!objectName.StartsWith("Blocker_Wall"))
            {
                continue;
            }

            if (col.isTrigger)
            {
                col.isTrigger = false;
                fixedCount++;
            }
        }

        if (fixedCount > 0)
        {
            Debug.Log("[Bounds] Fixed " + fixedCount + " arena Blocker_Wall collider(s) (set isTrigger=false).");
        }
    }
}
