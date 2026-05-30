using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class HellfirePitDamageZone : MonoBehaviour
{
    [Header("Damage")]
    public float damagePerSecond = 100f;

    [Header("Debug")]
    public bool showHellfireDebugLogs = true;

    private readonly HashSet<UnitHealth> occupants = new HashSet<UnitHealth>();
    private readonly Dictionary<UnitHealth, float> pendingDamage = new Dictionary<UnitHealth, float>();
    private BoxCollider damageTrigger;
    private Bounds damageBounds;

    private void Awake()
    {
        DisableVisualCollidersOnParent();
        damageTrigger = GetComponent<BoxCollider>();
        EnsureDamageTrigger();
    }

    private void Start()
    {
        AlignToWalkableFloor();
    }

    private void AlignToWalkableFloor()
    {
        HellfirePitWalkableFloor floor = transform.parent != null
            ? transform.parent.GetComponentInChildren<HellfirePitWalkableFloor>()
            : null;
        if (floor == null)
        {
            return;
        }

        BoxCollider floorBox = floor.GetComponent<BoxCollider>();
        if (floorBox == null)
        {
            return;
        }

        Bounds floorBounds = floorBox.bounds;
        Vector3 size = new Vector3(floorBounds.size.x, Mathf.Max(1f, 3f), floorBounds.size.z);
        Vector3 center = floorBounds.center + Vector3.up * (size.y * 0.5f - floorBounds.extents.y);
        SetDamageBounds(center, size);
    }

    private void DisableVisualCollidersOnParent()
    {
        if (transform.parent == null)
        {
            return;
        }

        Collider[] colliders = transform.parent.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || col.gameObject == gameObject)
            {
                continue;
            }

            HellfirePitWalkableFloor walkable = col.GetComponent<HellfirePitWalkableFloor>();
            if (walkable != null)
            {
                continue;
            }

            col.enabled = false;
        }
    }

    private void Update()
    {
        RefreshOccupantsFromBounds();
        ApplyAccumulatedDamage();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryRegister(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryRegister(other);
    }

    private void OnTriggerExit(Collider other)
    {
        TryUnregister(other);
    }

    private void OnDisable()
    {
        occupants.Clear();
        pendingDamage.Clear();
    }

    public void EnsureDamageTrigger()
    {
        if (damageTrigger == null)
        {
            damageTrigger = GetComponent<BoxCollider>();
        }

        if (damageTrigger == null)
        {
            damageTrigger = gameObject.AddComponent<BoxCollider>();
        }

        damageTrigger.isTrigger = true;
        damageBounds = damageTrigger.bounds;

        if (showHellfireDebugLogs)
        {
            Debug.Log("[HellfirePit] Damage trigger verified: " + name
                + ", trigger true, center=" + transform.position
                + ", size=" + damageTrigger.size);
        }
    }

    public void SetDamageBounds(Vector3 worldCenter, Vector3 size)
    {
        transform.position = worldCenter;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        if (damageTrigger == null)
        {
            damageTrigger = GetComponent<BoxCollider>();
        }

        damageTrigger.size = size;
        damageTrigger.center = Vector3.zero;
        damageTrigger.isTrigger = true;
        damageBounds = new Bounds(worldCenter, size);
    }

    private void RefreshOccupantsFromBounds()
    {
        if (damageTrigger != null)
        {
            damageBounds = damageTrigger.bounds;
        }

        HashSet<UnitHealth> insideNow = new HashSet<UnitHealth>();
        UnitHealth[] allUnits = FindObjectsByType<UnitHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < allUnits.Length; i++)
        {
            UnitHealth health = allUnits[i];
            if (!IsValidTarget(health))
            {
                continue;
            }

            Vector3 sample = health.transform.position + Vector3.up * 0.75f;
            if (damageBounds.Contains(sample))
            {
                insideNow.Add(health);
            }
        }

        foreach (UnitHealth health in insideNow)
        {
            if (occupants.Add(health))
            {
                pendingDamage[health] = 0f;
                if (showHellfireDebugLogs)
                {
                    Debug.Log("[HellfirePit] " + health.name + " entered");
                }
            }
        }

        if (occupants.Count == 0)
        {
            return;
        }

        UnitHealth[] snapshot = new UnitHealth[occupants.Count];
        occupants.CopyTo(snapshot);
        for (int i = 0; i < snapshot.Length; i++)
        {
            if (snapshot[i] == null || !insideNow.Contains(snapshot[i]))
            {
                RemoveOccupant(snapshot[i]);
            }
        }
    }

    private void ApplyAccumulatedDamage()
    {
        if (occupants.Count == 0)
        {
            return;
        }

        UnitHealth[] snapshot = new UnitHealth[occupants.Count];
        occupants.CopyTo(snapshot);

        for (int i = 0; i < snapshot.Length; i++)
        {
            UnitHealth health = snapshot[i];
            if (!IsValidTarget(health))
            {
                RemoveOccupant(health);
                continue;
            }

            if (!pendingDamage.ContainsKey(health))
            {
                pendingDamage[health] = 0f;
            }

            pendingDamage[health] += damagePerSecond * Time.deltaTime;
            int damageAmount = Mathf.FloorToInt(pendingDamage[health]);
            if (damageAmount < 1)
            {
                continue;
            }

            pendingDamage[health] -= damageAmount;
            health.TakeDamage(damageAmount, gameObject);

            if (showHellfireDebugLogs)
            {
                Debug.Log("[HellfirePit] Damaged " + health.name + " for " + damageAmount);
            }
        }
    }

    private void TryRegister(Collider other)
    {
        UnitHealth health = other.GetComponentInParent<UnitHealth>();
        if (!IsValidTarget(health) || !occupants.Add(health))
        {
            return;
        }

        pendingDamage[health] = 0f;
        if (showHellfireDebugLogs)
        {
            Debug.Log("[HellfirePit] " + health.name + " entered");
        }
    }

    private void TryUnregister(Collider other)
    {
        RemoveOccupant(other.GetComponentInParent<UnitHealth>());
    }

    private void RemoveOccupant(UnitHealth health)
    {
        if (health == null)
        {
            return;
        }

        if (occupants.Remove(health))
        {
            pendingDamage.Remove(health);
            if (showHellfireDebugLogs)
            {
                Debug.Log("[HellfirePit] " + health.name + " exited");
            }
        }
    }

    private bool IsValidTarget(UnitHealth health)
    {
        return health != null && !health.IsDead;
    }
}
