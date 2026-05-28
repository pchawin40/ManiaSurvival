using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to any unit (Survivor or Monster) that should be able to carry world items.
/// Currently supports one carried item: Speed Boots.
/// </summary>
public class CarriedItemHolder : MonoBehaviour
{
    [Header("Speed Boots")]
    [Tooltip("Speed multiplier applied to SurvivorMovement while holding boots.")]
    [Range(1f, 1.3f)] public float survivorSpeedMultiplier = 1.15f;
    [Tooltip("Speed multiplier applied to MonsterPlayerMovement while holding boots.")]
    [Range(1f, 1.2f)] public float monsterSpeedMultiplier = 1.09f;

    [Header("Interact Input")]
    public bool keyboardInteractEnabled = true;
    public Key interactKey = Key.E;

    [Header("Drop Input")]
    public bool keyboardDropEnabled = true;
    public Key dropKey = Key.G;

    [Header("Visual Offset")]
    [Tooltip("Where the equipped boot visual appears, relative to this transform.")]
    public Vector3 bootsLocalOffset = new Vector3(0f, -0.8f, 0.2f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Read by InventoryDebugUI and future UI.
    public bool HasItem => currentBoots != null;
    public string CurrentItemName => HasItem ? "Speed Boots" : "";
    public bool HasNearbyItem => nearbyBoots != null;
    public string NearbyItemName => HasNearbyItem ? "Speed Boots" : "";

    private SpeedBootsPickup currentBoots;
    private SpeedBootsPickup nearbyBoots;
    private SurvivorMovement survivorMovement;
    private MonsterPlayerMovement monsterMovement;
    private UnitHealth unitHealth;

    private void Awake()
    {
        survivorMovement = GetComponent<SurvivorMovement>();
        monsterMovement = GetComponent<MonsterPlayerMovement>();
        unitHealth = GetComponent<UnitHealth>();

        if (unitHealth != null)
        {
            unitHealth.onDeath.AddListener(OnHolderDied);
        }
    }

    private void OnDestroy()
    {
        if (unitHealth != null)
        {
            unitHealth.onDeath.RemoveListener(OnHolderDied);
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (keyboardInteractEnabled && interactKey != Key.None
            && Keyboard.current[interactKey].wasPressedThisFrame)
        {
            TryPickupNearbyItem();
        }

        if (keyboardDropEnabled && dropKey != Key.None
            && Keyboard.current[dropKey].wasPressedThisFrame)
        {
            DropCurrentItem();
        }
    }

    // Called by SpeedBootsPickup when it enters trigger range.
    public void SetNearbySpeedBoots(SpeedBootsPickup boots)
    {
        if (boots == null)
        {
            return;
        }

        nearbyBoots = boots;
    }

    // Called by SpeedBootsPickup when it leaves trigger range.
    public void ClearNearbySpeedBoots(SpeedBootsPickup boots)
    {
        if (nearbyBoots == boots)
        {
            nearbyBoots = null;
        }
    }

    // Called on E key or mobile Interact button.
    public void TryPickupNearbyItem()
    {
        if (nearbyBoots == null)
        {
            if (showDebugLogs)
            {
                Debug.Log("[CarriedItemHolder] " + name + ": No item nearby.");
            }

            return;
        }

        TryPickupSpeedBoots(nearbyBoots);
    }

    // Public alias so a mobile Interact button can call this.
    public void TryInteract()
    {
        TryPickupNearbyItem();
    }

    // Called directly by CarriedItemHolder after manual pickup is confirmed.
    public bool TryPickupSpeedBoots(SpeedBootsPickup boots)
    {
        if (boots == null)
        {
            return false;
        }

        if (HasItem)
        {
            if (showDebugLogs)
            {
                Debug.Log("[CarriedItemHolder] " + name + " already holding an item, cannot pick up.");
            }

            return false;
        }

        currentBoots = boots;
        if (nearbyBoots == boots)
        {
            nearbyBoots = null;
        }

        ApplySpeedBoost();
        boots.AttachToHolder(transform, bootsLocalOffset);

        if (showDebugLogs)
        {
            Debug.Log("[CarriedItemHolder] Speed Boots picked up by " + name);
        }

        return true;
    }

    // Drop at current position.
    public void DropCurrentItem()
    {
        if (!HasItem)
        {
            if (showDebugLogs)
            {
                Debug.Log("[CarriedItemHolder] " + name + ": No item to drop.");
            }

            return;
        }

        DropCurrentItemAt(transform.position);
    }

    // Drop at an explicit world position (used on death).
    public void DropCurrentItemAt(Vector3 position)
    {
        if (!HasItem)
        {
            return;
        }

        SpeedBootsPickup boots = currentBoots;
        currentBoots = null;

        RemoveSpeedBoost();
        boots.DropAt(position);

        if (showDebugLogs)
        {
            Debug.Log("[CarriedItemHolder] Speed Boots dropped at " + position + " by " + name);
        }
    }

    // Public alias for future mobile UI button.
    public void DropCurrentItemFromButton()
    {
        DropCurrentItem();
    }

    private void OnHolderDied()
    {
        if (!HasItem)
        {
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log("[CarriedItemHolder] " + name + " died and dropped Speed Boots.");
        }

        DropCurrentItemAt(transform.position);
    }

    private void ApplySpeedBoost()
    {
        if (survivorMovement != null)
        {
            survivorMovement.SetCarriedSpeedBoost(survivorSpeedMultiplier);
            return;
        }

        if (monsterMovement != null)
        {
            monsterMovement.SetCarriedSpeedBoost(monsterSpeedMultiplier);
        }
    }

    private void RemoveSpeedBoost()
    {
        if (survivorMovement != null)
        {
            survivorMovement.ClearCarriedSpeedBoost();
            return;
        }

        if (monsterMovement != null)
        {
            monsterMovement.ClearCarriedSpeedBoost();
        }
    }
}
