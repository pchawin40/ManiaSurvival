using UnityEngine;

public class SpeedBootsPickup : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Optional prefab used as the equipped boot visual on the holder. If empty, a placeholder cube is created.")]
    public GameObject bootVisualPrefab;
    [Tooltip("Where the boot visual attaches relative to the holder root.")]
    public Vector3 equippedLocalOffset = new Vector3(0f, -0.8f, 0.2f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Collider pickupCollider;
    private Renderer pickupRenderer;
    private GameObject equippedVisual;
    private bool isHeld;

    private void Awake()
    {
        pickupCollider = GetComponent<Collider>();
        pickupRenderer = GetComponent<Renderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isHeld)
        {
            return;
        }

        CarriedItemHolder holder = other.GetComponentInParent<CarriedItemHolder>();
        if (holder == null)
        {
            return;
        }

        holder.SetNearbySpeedBoots(this);

        if (showDebugLogs)
        {
            Debug.Log("[SpeedBootsPickup] In range of " + holder.name + " — press E to pick up.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        CarriedItemHolder holder = other.GetComponentInParent<CarriedItemHolder>();
        if (holder == null)
        {
            return;
        }

        holder.ClearNearbySpeedBoots(this);
    }

    // Called by CarriedItemHolder when this item is picked up.
    public void AttachToHolder(Transform holderRoot, Vector3 localOffset)
    {
        isHeld = true;

        SetWorldVisible(false);

        equippedVisual = CreateOrInstantiateVisual();
        equippedVisual.transform.SetParent(holderRoot, false);
        equippedVisual.transform.localPosition = localOffset;
        equippedVisual.transform.localRotation = Quaternion.identity;
        equippedVisual.name = "SpeedBoots_EquippedVisual";

        transform.SetParent(holderRoot, false);
        transform.localPosition = Vector3.zero;

        if (showDebugLogs)
        {
            Debug.Log("[SpeedBootsPickup] Attached to " + holderRoot.name);
        }
    }

    // Called by CarriedItemHolder when the item is dropped.
    public void DropAt(Vector3 worldPosition)
    {
        isHeld = false;

        if (equippedVisual != null)
        {
            Destroy(equippedVisual);
            equippedVisual = null;
        }

        transform.SetParent(null);
        transform.position = worldPosition;
        transform.rotation = Quaternion.identity;

        SetWorldVisible(true);

        if (showDebugLogs)
        {
            Debug.Log("[SpeedBootsPickup] Dropped at " + worldPosition);
        }
    }

    private void SetWorldVisible(bool visible)
    {
        if (pickupCollider != null)
        {
            pickupCollider.enabled = visible;
        }

        if (pickupRenderer != null)
        {
            pickupRenderer.enabled = visible;
        }
    }

    private GameObject CreateOrInstantiateVisual()
    {
        if (bootVisualPrefab != null)
        {
            return Instantiate(bootVisualPrefab);
        }

        GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Collider col = placeholder.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        placeholder.transform.localScale = new Vector3(0.2f, 0.1f, 0.35f);
        return placeholder;
    }
}
