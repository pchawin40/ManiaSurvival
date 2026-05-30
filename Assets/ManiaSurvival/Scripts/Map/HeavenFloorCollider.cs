using UnityEngine;

[DisallowMultipleComponent]
public class HeavenFloorCollider : MonoBehaviour
{
    private const string WalkableChildName = "HeavenWalkableFloor";

    [Header("Walkable Floor")]
    [Tooltip("World-space center of the solid Heaven floor collider.")]
    public Vector3 floorWorldCenter = new Vector3(0f, 0.25f, -100f);
    [Tooltip("Size of the solid BoxCollider that blocks CharacterController movement.")]
    public Vector3 floorSize = new Vector3(80f, 0.5f, 160f);
    public bool autoFixOnAwake = true;

    private void Awake()
    {
        if (autoFixOnAwake)
        {
            EnsureWalkableFloor();
        }
    }

    public void EnsureWalkableFloor()
    {
        Transform parent = transform.parent != null ? transform.parent : transform;
        Transform walkable = parent.Find(WalkableChildName);
        if (walkable == null)
        {
            GameObject floorObject = new GameObject(WalkableChildName);
            floorObject.transform.SetParent(parent, true);
            walkable = floorObject.transform;
        }

        walkable.position = floorWorldCenter;
        walkable.rotation = Quaternion.identity;
        walkable.localScale = Vector3.one;

        BoxCollider box = walkable.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = walkable.gameObject.AddComponent<BoxCollider>();
        }

        box.isTrigger = false;
        box.size = floorSize;
        box.center = Vector3.zero;

        Debug.Log("[HeavenFloor] Collider verified/fixed on " + walkable.name
            + " at " + walkable.position + " size " + floorSize);
    }

    public static Vector3 GetSafeStandPosition(Vector3 nearPosition, float verticalOffset)
    {
        Vector3 rayStart = nearPosition + Vector3.up * 12f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 40f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * verticalOffset;
        }

        return nearPosition + Vector3.up * verticalOffset;
    }
}
