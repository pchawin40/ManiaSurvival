using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HellPitHazard : MonoBehaviour
{
    private static readonly Dictionary<UnitHealth, float> NextSharedDamageTime = new Dictionary<UnitHealth, float>();

    [Header("Damage")]
    [Tooltip("If true, survivors touching the HellPit are killed instantly on contact.")]
    public bool instantKill = true;

    public float survivorDamagePerSecond = 100f;

    [Tooltip("Damage-per-second applied to the monster while inside the pit.")]
    public float monsterDamagePerSecond = 100f;

    public float tickInterval = 0.25f;
    public bool instantKillMonster = false;
    public bool destroyOnMonsterDeath = false;

    [Header("Walkable Floor")]
    public bool ensureWalkableFloor = true;
    public float floorThickness = 0.35f;
    public float floorSurfaceOffset = 0.05f;

    [Tooltip("Monsters get a solid floor and take monsterDamagePerSecond instead of instant death.")]
    public bool monsterCanWalkOut = true;

    [Header("Detection")]
    public float hazardVolumeHeight = 4f;
    public float occupancyPollInterval = 0.1f;
    [Tooltip("Extra padding added around the pit bounds on X/Z.")]
    public float boundsPadding = 0.5f;
    [Tooltip("Extends the solid floor beyond the red visual so there is no vertical lip blocking entry.")]
    public float floorApproachPadding = 2f;

    [Header("Debug")]
    public bool drawGizmo = true;

    private readonly HashSet<UnitHealth> occupants = new HashSet<UnitHealth>();
    private readonly Dictionary<UnitHealth, float> tickTimers = new Dictionary<UnitHealth, float>();
    private float nextOccupancyPollTime;
    private Bounds detectionBounds;

    private void Start()
    {
        ConfigureVisualColliders();

        RefreshDetectionBounds();

        if (ensureWalkableFloor)
        {
            EnsureWalkableFloor();
            RefreshDetectionBounds();
        }

        Debug.Log("[HellPit] Hazard ready on '" + name + "' bounds=" + detectionBounds.size
            + " center=" + detectionBounds.center + " monsterDps=" + monsterDamagePerSecond);
    }

    private void ConfigureVisualColliders()
    {
        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null)
            {
                continue;
            }

            // The red plane mesh must not block CharacterController entry.
            if (col is MeshCollider)
            {
                col.isTrigger = true;
                col.enabled = false;
                continue;
            }

            // Remove any stray solid box left on this object; walkable floor lives on a child.
            if (col is BoxCollider)
            {
                Destroy(col);
            }
        }
    }

    private void RefreshDetectionBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            detectionBounds = ExpandBounds(renderers[0].bounds);
            for (int i = 1; i < renderers.Length; i++)
            {
                detectionBounds.Encapsulate(renderers[i].bounds);
            }

            detectionBounds = ExpandBounds(detectionBounds);
            return;
        }

        Collider pitCollider = GetComponent<Collider>();
        if (pitCollider != null)
        {
            detectionBounds = ExpandBounds(pitCollider.bounds);
            return;
        }

        detectionBounds = new Bounds(transform.position, new Vector3(10f, hazardVolumeHeight, 10f));
    }

    private Bounds ExpandBounds(Bounds source)
    {
        float height = Mathf.Max(1f, hazardVolumeHeight);
        Vector3 size = new Vector3(
            Mathf.Max(1f, source.size.x + boundsPadding * 2f),
            height,
            Mathf.Max(1f, source.size.z + boundsPadding * 2f));
        float floorTop = source.max.y;
        Vector3 center = new Vector3(source.center.x, floorTop - floorSurfaceOffset + (height * 0.5f), source.center.z);
        return new Bounds(center, size);
    }

    public void EnsureWalkableFloor()
    {
        const string walkableChildName = "HellWalkableFloor";
        RefreshDetectionBounds();

        Transform walkable = transform.Find(walkableChildName);
        if (walkable == null)
        {
            GameObject floorObject = new GameObject(walkableChildName);
            floorObject.transform.SetParent(transform, true);
            walkable = floorObject.transform;
        }

        float surfaceY = Mathf.Max(0.02f, detectionBounds.min.y + 0.01f);
        walkable.position = new Vector3(detectionBounds.center.x, surfaceY - (floorThickness * 0.5f), detectionBounds.center.z);
        walkable.rotation = Quaternion.identity;
        walkable.localScale = Vector3.one;

        BoxCollider box = walkable.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = walkable.gameObject.AddComponent<BoxCollider>();
        }

        box.isTrigger = false;
        box.size = new Vector3(
            Mathf.Max(1f, detectionBounds.size.x + floorApproachPadding * 2f),
            Mathf.Max(0.05f, floorThickness),
            Mathf.Max(1f, detectionBounds.size.z + floorApproachPadding * 2f));
        box.center = Vector3.zero;
    }

    private void PollOccupants()
    {
        RefreshDetectionBounds();

        HashSet<UnitHealth> currentlyInside = new HashSet<UnitHealth>();
        UnitHealth[] allUnits = FindObjectsByType<UnitHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < allUnits.Length; i++)
        {
            UnitHealth health = allUnits[i];
            if (!IsEligibleTarget(health) || currentlyInside.Contains(health))
            {
                continue;
            }

            if (IsUnitInsideBounds(health))
            {
                currentlyInside.Add(health);
            }
        }

        foreach (UnitHealth health in currentlyInside)
        {
            RegisterOccupant(health);
        }

        if (occupants.Count == 0)
        {
            return;
        }

        UnitHealth[] snapshot = new UnitHealth[occupants.Count];
        occupants.CopyTo(snapshot);
        for (int i = 0; i < snapshot.Length; i++)
        {
            if (snapshot[i] == null || !currentlyInside.Contains(snapshot[i]))
            {
                UnregisterOccupant(snapshot[i]);
            }
        }
    }

    private bool IsUnitInsideBounds(UnitHealth health)
    {
        Vector3 sample = health.transform.position + Vector3.up * 0.75f;
        return detectionBounds.Contains(sample);
    }

    private void Update()
    {
        if (Time.time >= nextOccupancyPollTime)
        {
            nextOccupancyPollTime = Time.time + Mathf.Max(0.05f, occupancyPollInterval);
            PollOccupants();
        }

        if (occupants.Count == 0)
        {
            return;
        }

        UnitHealth[] snapshot = new UnitHealth[occupants.Count];
        occupants.CopyTo(snapshot);

        for (int i = 0; i < snapshot.Length; i++)
        {
            UnitHealth unitHealth = snapshot[i];
            if (unitHealth == null)
            {
                continue;
            }

            if (!tickTimers.ContainsKey(unitHealth))
            {
                tickTimers[unitHealth] = 0f;
            }

            tickTimers[unitHealth] += Time.deltaTime;
            if (tickTimers[unitHealth] >= Mathf.Max(0.01f, tickInterval))
            {
                tickTimers[unitHealth] = 0f;
                ApplyDamageTick(unitHealth);
            }
        }
    }

    private void ApplyDamageTick(UnitHealth unitHealth)
    {
        if (unitHealth == null || unitHealth.IsDead)
        {
            UnregisterOccupant(unitHealth);
            return;
        }

        if (!IsEligibleTarget(unitHealth))
        {
            return;
        }

        float damagePerSecond = GetDamagePerSecond(unitHealth);
        if (damagePerSecond <= 0f)
        {
            return;
        }

        if (NextSharedDamageTime.TryGetValue(unitHealth, out float nextAllowedTime) && Time.time < nextAllowedTime)
        {
            return;
        }

        if (instantKillMonster && IsMonster(unitHealth) && !monsterCanWalkOut)
        {
            unitHealth.TakeDamage(unitHealth.currentHealth, gameObject);
            if (destroyOnMonsterDeath)
            {
                Destroy(unitHealth.gameObject);
            }

            return;
        }

        int damageAmount = Mathf.Max(1, Mathf.RoundToInt(damagePerSecond * Mathf.Max(0.01f, tickInterval)));
        unitHealth.TakeDamage(damageAmount, gameObject);
        NextSharedDamageTime[unitHealth] = Time.time + Mathf.Max(0.01f, tickInterval);

        Debug.Log("[HellPit] damage tick " + damageAmount + " to " + unitHealth.name
            + " from '" + name + "' (" + unitHealth.currentHealth + "/" + unitHealth.maxHealth + ")");
    }

    private float GetDamagePerSecond(UnitHealth unitHealth)
    {
        if (IsMonster(unitHealth))
        {
            return monsterDamagePerSecond;
        }

        if (IsSurvivor(unitHealth))
        {
            return survivorDamagePerSecond;
        }

        return 0f;
    }

    private bool IsEligibleTarget(UnitHealth unitHealth)
    {
        if (unitHealth == null || unitHealth.IsDead)
        {
            return false;
        }

        return IsSurvivor(unitHealth) || IsMonster(unitHealth);
    }

    private bool IsSurvivor(UnitHealth unitHealth)
    {
        return unitHealth != null && unitHealth.CompareTag("Survivor");
    }

    private bool IsMonster(UnitHealth unitHealth)
    {
        return unitHealth != null && (unitHealth.CompareTag("Monster") || unitHealth.CompareTag("Predator"));
    }

    private void RegisterOccupant(UnitHealth unitHealth)
    {
        if (unitHealth == null || unitHealth.IsDead || !IsEligibleTarget(unitHealth))
        {
            return;
        }

        if (occupants.Add(unitHealth))
        {
            tickTimers[unitHealth] = 0f;
            Debug.Log("[HellPit] " + unitHealth.name + " entered HellPit on '" + name + "'.");

            if (instantKill && IsSurvivor(unitHealth))
            {
                unitHealth.TakeDamage(unitHealth.maxHealth * 2 + 1, gameObject);
            }
            else if (instantKillMonster && IsMonster(unitHealth) && !monsterCanWalkOut)
            {
                unitHealth.TakeDamage(unitHealth.currentHealth, gameObject);
                if (destroyOnMonsterDeath)
                {
                    Destroy(unitHealth.gameObject);
                }
            }
        }
    }

    private void UnregisterOccupant(UnitHealth unitHealth)
    {
        if (unitHealth == null)
        {
            return;
        }

        if (occupants.Remove(unitHealth))
        {
            tickTimers.Remove(unitHealth);
            Debug.Log("[HellPit] " + unitHealth.name + " exited HellPit on '" + name + "'.");
        }
    }

    private void OnDisable()
    {
        occupants.Clear();
        tickTimers.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo)
        {
            return;
        }

        Bounds bounds = Application.isPlaying ? detectionBounds : ExpandBounds(GetEditorBounds());
        Gizmos.color = new Color(1f, 0.25f, 0.1f, 0.35f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }

    private Bounds GetEditorBounds()
    {
        Collider pitCollider = GetComponent<Collider>();
        if (pitCollider != null)
        {
            return pitCollider.bounds;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(transform.position, Vector3.one * 4f);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }
}
