using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class HellfirePitDamageZone : MonoBehaviour
{
    [Header("Damage")]
    public float damagePerSecond = 100f;

    [Header("Manual Damage Bounds")]
    public bool useManualDamageBounds = true;
    public Vector3 localDamageCenter = new Vector3(0f, -1.48f, 0f);
    public Vector3 localDamageSize = new Vector3(9.5f, 1f, 10f);
    public float verticalTolerance = 3f;

    [Header("Candidate Trigger")]
    [Tooltip("Trigger volume used only to detect nearby units. Damage still requires manual feet bounds.")]
    public Vector3 triggerLocalCenter = new Vector3(0f, -1.48f, 0f);
    public Vector3 triggerLocalSize = new Vector3(14f, 3f, 10f);

    [Header("Debug")]
    public bool showHellfireDebugLogs = true;
    public bool showHellfireDebugGizmos = true;
    [Tooltip("One-time feet check for a known green-floor position that must stay outside damage.")]
    public Vector3 safetyCheckWorldPosition = new Vector3(-16.29f, 1.72f, -3.17f);

    private readonly HashSet<UnitHealth> triggerCandidates = new HashSet<UnitHealth>();
    private readonly HashSet<UnitHealth> feetInsideUnits = new HashSet<UnitHealth>();
    private readonly HashSet<UnitHealth> loggedFeetOutsideInTrigger = new HashSet<UnitHealth>();
    private readonly Dictionary<UnitHealth, float> pendingDamage = new Dictionary<UnitHealth, float>();
    private BoxCollider damageTrigger;
    private bool loggedSafetyCheck;

    private void Awake()
    {
        DisableVisualCollidersOnParent();
        damageTrigger = GetComponent<BoxCollider>();
        EnsureCandidateTrigger();
    }

    private void Start()
    {
        LogSafetyCheckPosition();
    }

    private void LogSafetyCheckPosition()
    {
        if (!showHellfireDebugLogs || loggedSafetyCheck)
        {
            return;
        }

        loggedSafetyCheck = true;
        Vector3 localFeet = transform.InverseTransformPoint(safetyCheckWorldPosition);
        bool inside = IsLocalFeetInsideManualDamageBounds(localFeet);
        Debug.Log("[HellfirePit] Safety check position " + safetyCheckWorldPosition
            + " localFeet=" + localFeet
            + " insideManualBounds=" + inside
            + " center=" + localDamageCenter
            + " size=" + localDamageSize);
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

            if (col.GetComponent<HellfirePitWalkableFloor>() != null)
            {
                continue;
            }

            col.enabled = false;
        }
    }

    private void Update()
    {
        ReconcileFeetInsideCandidates();
        ApplyAccumulatedDamage();
    }

    private void OnTriggerEnter(Collider other)
    {
        AddTriggerCandidate(other);
    }

    private void OnTriggerStay(Collider other)
    {
        AddTriggerCandidate(other);
    }

    private void OnTriggerExit(Collider other)
    {
        RemoveTriggerCandidate(other.GetComponentInParent<UnitHealth>());
    }

    private void OnDisable()
    {
        triggerCandidates.Clear();
        feetInsideUnits.Clear();
        loggedFeetOutsideInTrigger.Clear();
        pendingDamage.Clear();
    }

    public void EnsureCandidateTrigger()
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
        damageTrigger.center = triggerLocalCenter;
        damageTrigger.size = triggerLocalSize;

        if (showHellfireDebugLogs)
        {
            Debug.Log("[HellfirePit] Candidate trigger verified: " + name + ", center=" + triggerLocalCenter
                + ", size=" + triggerLocalSize);
        }
    }

    private void ReconcileFeetInsideCandidates()
    {
        if (triggerCandidates.Count == 0)
        {
            return;
        }

        UnitHealth[] snapshot = new UnitHealth[triggerCandidates.Count];
        triggerCandidates.CopyTo(snapshot);

        for (int i = 0; i < snapshot.Length; i++)
        {
            UnitHealth health = snapshot[i];
            if (!IsValidTarget(health))
            {
                RemoveTriggerCandidate(health);
                continue;
            }

            bool inside = IsFeetInsideManualDamageBounds(health);
            bool wasInside = feetInsideUnits.Contains(health);

            if (inside)
            {
                if (!wasInside)
                {
                    feetInsideUnits.Add(health);
                    pendingDamage[health] = 0f;
                    if (showHellfireDebugLogs)
                    {
                        Debug.Log("[HellfirePit] " + health.name + " feet entered red damage area");
                    }
                }

                continue;
            }

            if (wasInside)
            {
                feetInsideUnits.Remove(health);
                pendingDamage.Remove(health);
                if (showHellfireDebugLogs)
                {
                    Debug.Log("[HellfirePit] " + health.name + " feet exited red damage area");
                }
            }

            if (!loggedFeetOutsideInTrigger.Contains(health))
            {
                loggedFeetOutsideInTrigger.Add(health);
                if (showHellfireDebugLogs)
                {
                    Vector3 localFeet = transform.InverseTransformPoint(health.transform.position);
                    Debug.Log("[HellfirePit] " + health.name
                        + " in trigger but feet outside manual damage bounds. localFeet=" + localFeet
                        + ", center=" + localDamageCenter + ", size=" + localDamageSize);
                }
            }
        }
    }

    private void ApplyAccumulatedDamage()
    {
        if (feetInsideUnits.Count == 0)
        {
            return;
        }

        UnitHealth[] snapshot = new UnitHealth[feetInsideUnits.Count];
        feetInsideUnits.CopyTo(snapshot);

        for (int i = 0; i < snapshot.Length; i++)
        {
            UnitHealth health = snapshot[i];
            if (!IsValidTarget(health) || !IsFeetInsideManualDamageBounds(health))
            {
                feetInsideUnits.Remove(health);
                pendingDamage.Remove(health);
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

    private void AddTriggerCandidate(Collider other)
    {
        UnitHealth health = other.GetComponentInParent<UnitHealth>();
        if (!IsValidTarget(health))
        {
            return;
        }

        if (!triggerCandidates.Add(health))
        {
            return;
        }

        loggedFeetOutsideInTrigger.Remove(health);
        if (showHellfireDebugLogs)
        {
            Debug.Log("[HellfirePit] " + health.name + " entered trigger only at feet "
                + health.transform.position);
        }
    }

    private void RemoveTriggerCandidate(UnitHealth health)
    {
        if (health == null)
        {
            return;
        }

        triggerCandidates.Remove(health);
        loggedFeetOutsideInTrigger.Remove(health);

        if (feetInsideUnits.Remove(health))
        {
            pendingDamage.Remove(health);
            if (showHellfireDebugLogs)
            {
                Debug.Log("[HellfirePit] " + health.name + " feet exited red damage area");
            }
        }
    }

    private bool IsFeetInsideManualDamageBounds(UnitHealth health)
    {
        if (health == null || !useManualDamageBounds)
        {
            return false;
        }

        Vector3 localFeet = transform.InverseTransformPoint(health.transform.position);
        return IsLocalFeetInsideManualDamageBounds(localFeet);
    }

    private bool IsLocalFeetInsideManualDamageBounds(Vector3 localFeet)
    {
        float halfX = localDamageSize.x * 0.5f;
        float halfZ = localDamageSize.z * 0.5f;

        if (Mathf.Abs(localFeet.x - localDamageCenter.x) > halfX)
        {
            return false;
        }

        if (Mathf.Abs(localFeet.z - localDamageCenter.z) > halfZ)
        {
            return false;
        }

        float yMin = localDamageCenter.y - 0.5f;
        float yMax = localDamageCenter.y + verticalTolerance;
        return localFeet.y >= yMin && localFeet.y <= yMax;
    }

    private bool IsValidTarget(UnitHealth health)
    {
        return health != null && !health.IsDead;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showHellfireDebugGizmos)
        {
            return;
        }

        DrawManualDamageBoundsGizmo();
        DrawTriggerGizmo();
        DrawWalkableFloorGizmo();
    }

    private void DrawManualDamageBoundsGizmo()
    {
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        float gizmoHeight = verticalTolerance + 0.5f;
        float gizmoCenterY = localDamageCenter.y + ((verticalTolerance - 0.5f) * 0.5f);
        Vector3 gizmoCenter = new Vector3(localDamageCenter.x, gizmoCenterY, localDamageCenter.z);
        Vector3 gizmoSize = new Vector3(localDamageSize.x, gizmoHeight, localDamageSize.z);

        Gizmos.color = new Color(1f, 0.15f, 0.05f, 0.9f);
        Gizmos.DrawWireCube(gizmoCenter, gizmoSize);

        Gizmos.matrix = previousMatrix;
    }

    private void DrawTriggerGizmo()
    {
        BoxCollider box = damageTrigger != null ? damageTrigger : GetComponent<BoxCollider>();
        if (box == null)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.92f, 0.1f, 0.85f);
        Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
    }

    private void DrawWalkableFloorGizmo()
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

        Gizmos.color = new Color(0.2f, 0.85f, 0.25f, 0.85f);
        Gizmos.DrawWireCube(floorBox.bounds.center, floorBox.bounds.size);
    }
}
