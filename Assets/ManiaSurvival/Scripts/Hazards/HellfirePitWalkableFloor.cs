using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class HellfirePitWalkableFloor : MonoBehaviour
{
    [Header("Floor")]
    public float floorThickness = 0.3f;
    public float floorTopY = 0.02f;
    [Tooltip("Extends floor toward +X so entry from the arena side has no vertical lip.")]
    public float approachExtendX = 2f;

    [Header("Sizing")]
    public Vector3 floorSize = new Vector3(12f, 0.3f, 10f);
    public Vector3 floorCenter = new Vector3(-15f, 0f, 0f);

    [Header("Debug")]
    public bool showHellfireDebugLogs = true;

    private BoxCollider floorCollider;

    private void Awake()
    {
        EnsureSolidFloor();
    }

    public void EnsureSolidFloor()
    {
        floorCollider = GetComponent<BoxCollider>();
        if (floorCollider == null)
        {
            floorCollider = gameObject.AddComponent<BoxCollider>();
        }

        Vector3 size = floorSize;
        size.y = Mathf.Max(0.2f, floorThickness);
        size.x = Mathf.Max(1f, floorSize.x + approachExtendX);

        float centerY = floorTopY - (size.y * 0.5f);
        transform.localPosition = new Vector3(floorCenter.x + (approachExtendX * 0.5f), centerY, floorCenter.z);
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        floorCollider.isTrigger = false;
        floorCollider.size = size;
        floorCollider.center = Vector3.zero;

        if (showHellfireDebugLogs)
        {
            Debug.Log("[HellfirePit] Walkable floor collider verified/fixed: " + name
                + ", size " + size + ", trigger false, worldPos=" + transform.position);
        }
    }

    public void MatchVisual(Renderer visualRenderer, float extendX)
    {
        if (visualRenderer == null)
        {
            EnsureSolidFloor();
            return;
        }

        Bounds bounds = visualRenderer.bounds;
        approachExtendX = extendX;
        floorSize = new Vector3(bounds.size.x, floorThickness, bounds.size.z);

        Vector3 parentInverse = transform.parent != null
            ? transform.parent.InverseTransformPoint(bounds.center)
            : bounds.center;

        floorCenter = new Vector3(
            parentInverse.x + (approachExtendX * 0.5f),
            0f,
            parentInverse.z);

        EnsureSolidFloor();
    }
}
