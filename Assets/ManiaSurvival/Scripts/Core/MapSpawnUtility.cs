using UnityEngine;

/// <summary>
/// Controls which NoSpawnZone flag must be active for the zone to reject a spawn.
/// Set on MapSpawnSettings.noSpawnFilter per-spawner.
/// </summary>
public enum NoSpawnFilter
{
    /// <summary>Reject if ANY block flag on the zone is true. Safe default.</summary>
    Everything,
    /// <summary>Only reject if the zone's blockTreePatches flag is true.</summary>
    Trees,
    /// <summary>Only reject if the zone's blockItems flag is true.</summary>
    Items,
    /// <summary>Only reject if the zone's blockHazards flag is true.</summary>
    Hazards,
    /// <summary>Only reject if the zone's blockNPCChaos flag is true.</summary>
    NPCChaos,
}

/// <summary>
/// Shared settings bag for MapSpawnUtility.
/// [System.Serializable] lets you embed it directly in any Inspector.
/// </summary>
[System.Serializable]
public class MapSpawnSettings
{
    [Tooltip("World-space centre of the playable area.")]
    public Vector3 mapCenter;

    [Tooltip("Width (X) and depth (Z) of the playable area in world units.")]
    public Vector2 mapSize = new Vector2(40f, 40f);

    [Tooltip("Minimum distance from each map edge. Positions closer than this are rejected.")]
    public float innerMargin = 3f;

    [Tooltip("Layers that count as valid ground. Leave empty to skip the downward raycast.")]
    public LayerMask groundLayerMask;

    [Tooltip("How far above the candidate position the downward raycast starts.")]
    public float groundRaycastStartHeight = 15f;

    [Tooltip("Layers that are hazard volumes (WaterZone, HellPit). Positions inside are rejected.")]
    public LayerMask hazardLayerMask;

    [Tooltip("Layers for solid blockers (walls, obstacles). Positions inside are rejected.")]
    public LayerMask blockerLayerMask;

    [Tooltip("Sphere radius used for the hazard layer overlap check.")]
    public float overlapCheckRadius = 0.5f;

    [Header("Clearance")]
    [Tooltip("If true, each candidate is rejected when solid obstacles occupy the spawn point. " +
             "Catches walls, existing trees, and any solid collider at the position.")]
    public bool requireClearance = true;

    [Tooltip("Sphere radius for the clearance overlap check. Should roughly match the tree/item footprint. " +
             "Default 0.75 works well for most world objects.")]
    public float occupancyCheckRadius = 0.75f;

    [Tooltip("Spawn is rejected if a live unit is within this distance. Set 0 to skip.")]
    public float minDistanceFromUnits = 3f;

    [Tooltip("Maximum random attempts before giving up on finding a position.")]
    public int maxAttempts = 20;

    [Header("No-Spawn Zones")]
    [Tooltip("Sphere radius checked for NoSpawnZone components around each candidate. Set 0 to disable.")]
    public float noSpawnZoneCheckRadius = 1.5f;

    [Tooltip("Which NoSpawnZone flag must be active for the zone to reject this spawn. " +
             "'Everything' is the safe default (any flag blocks the spawn).")]
    public NoSpawnFilter noSpawnFilter = NoSpawnFilter.Everything;
}

/// <summary>
/// Reusable static helper for finding safe spawn positions on the playable map.
/// Used by WildkeeperController, InvisibilityScrollSpawner, NPCChaosCaster, and any
/// future system that needs to place an object safely inside the map.
/// </summary>
public static class MapSpawnUtility
{
    /// <summary>
    /// Tries to find a random valid position anywhere inside the map bounds
    /// (respecting inner margin). Returns true and sets <paramref name="result"/>
    /// on success; returns false and logs nothing (caller should log) on failure.
    /// </summary>
    public static bool TryGetValidPosition(MapSpawnSettings s, out Vector3 result)
    {
        result = s.mapCenter;

        if (s == null)
        {
            return false;
        }

        for (int i = 0; i < Mathf.Max(1, s.maxAttempts); i++)
        {
            Vector3 candidate = RandomXZInMap(s);

            if (!TryLandOnGround(candidate, s, out Vector3 groundPos))
            {
                continue;
            }

            if (!IsPositionSafe(groundPos, s, out _))
            {
                continue;
            }

            result = groundPos;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to find a random valid position within <paramref name="radius"/> of
    /// <paramref name="center"/>, clamped to stay inside the map with inner margin.
    /// </summary>
    public static bool TryGetValidPositionNear(Vector3 center, float radius, MapSpawnSettings s, out Vector3 result)
    {
        result = center;

        if (s == null)
        {
            return false;
        }

        for (int i = 0; i < Mathf.Max(1, s.maxAttempts); i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(0f, Mathf.Max(0f, radius));
            Vector3 offset = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            Vector3 candidate = ClampToMapWithMargin(center + offset, s);

            if (!TryLandOnGround(candidate, s, out Vector3 groundPos))
            {
                continue;
            }

            if (!IsPositionSafe(groundPos, s, out _))
            {
                continue;
            }

            result = groundPos;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Clamps a position to the inner region of the map (bounds minus innerMargin).
    /// </summary>
    public static Vector3 ClampToMapWithMargin(Vector3 pos, MapSpawnSettings s)
    {
        float hw = Mathf.Max(0f, s.mapSize.x * 0.5f - s.innerMargin);
        float hl = Mathf.Max(0f, s.mapSize.y * 0.5f - s.innerMargin);
        float x = Mathf.Clamp(pos.x, s.mapCenter.x - hw, s.mapCenter.x + hw);
        float z = Mathf.Clamp(pos.z, s.mapCenter.z - hl, s.mapCenter.z + hl);
        return new Vector3(x, pos.y, z);
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private static Vector3 RandomXZInMap(MapSpawnSettings s)
    {
        float hw = Mathf.Max(0f, s.mapSize.x * 0.5f - s.innerMargin);
        float hl = Mathf.Max(0f, s.mapSize.y * 0.5f - s.innerMargin);
        float x = Random.Range(s.mapCenter.x - hw, s.mapCenter.x + hw);
        float z = Random.Range(s.mapCenter.z - hl, s.mapCenter.z + hl);
        return new Vector3(x, s.mapCenter.y, z);
    }

    private static bool TryLandOnGround(Vector3 xzPos, MapSpawnSettings s, out Vector3 groundPos)
    {
        groundPos = xzPos;

        if (s.groundLayerMask.value == 0)
        {
            // No mask assigned — accept the raw position (flat-map fallback).
            return true;
        }

        float startH = s.groundRaycastStartHeight > 0f ? s.groundRaycastStartHeight : 15f;
        Vector3 origin = new Vector3(xzPos.x, xzPos.y + startH, xzPos.z);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, startH + 30f, s.groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            groundPos = hit.point;
            return true;
        }

        return false;
    }

    private static bool IsPositionSafe(Vector3 pos, MapSpawnSettings s, out string failReason)
    {
        failReason = string.Empty;

        // Hazard volume check — uses QueryTriggerInteraction.Collide so trigger colliders (WaterZone, HellPit) are caught.
        if (s.hazardLayerMask.value != 0 &&
            Physics.CheckSphere(pos, Mathf.Max(0.05f, s.overlapCheckRadius), s.hazardLayerMask, QueryTriggerInteraction.Collide))
        {
            failReason = "inside hazard";
            return false;
        }

        // Clearance check — rejects the candidate if any solid obstacle occupies this position.
        // Works with or without a blockerLayerMask:
        //   - blockerLayerMask assigned → query only that set of layers (fastest, most precise).
        //   - blockerLayerMask unassigned → query all layers except the ground layer,
        //     so the ground surface itself does not veto the spawn.
        if (s.requireClearance && s.occupancyCheckRadius > 0f)
        {
            int clearanceMask = s.blockerLayerMask.value != 0
                ? s.blockerLayerMask.value
                : ~s.groundLayerMask.value; // ~0 = all layers when ground unset

            Collider[] hits = Physics.OverlapSphere(
                pos,
                Mathf.Max(0.05f, s.occupancyCheckRadius),
                clearanceMask,
                QueryTriggerInteraction.Ignore); // trigger zones are handled separately

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                {
                    continue;
                }

                // Trees have their own per-spawner spacing checks; skip them here
                // so the clearance radius does not inadvertently prevent trees from
                // spawning near (but not overlapping) existing ones.
                if (hit.GetComponentInParent<NeutralTree>() != null)
                {
                    continue;
                }

                // Live units are handled by minDistanceFromUnits — skip here.
                if (hit.GetComponentInParent<UnitHealth>() != null)
                {
                    continue;
                }

                failReason = "blocked by obstacle: " + hit.name;
                return false;
            }
        }

        // Proximity to live units.
        if (s.minDistanceFromUnits > 0f)
        {
            Collider[] nearby = Physics.OverlapSphere(pos, s.minDistanceFromUnits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < nearby.Length; i++)
            {
                UnitHealth h = nearby[i].GetComponentInParent<UnitHealth>();
                if (h != null && !h.IsDead)
                {
                    failReason = "too close to live unit";
                    return false;
                }
            }
        }

        // NoSpawnZone check — uses QueryTriggerInteraction.Collide so trigger-collider zones are hit.
        if (s.noSpawnZoneCheckRadius > 0f)
        {
            Collider[] zoneHits = Physics.OverlapSphere(
                pos,
                s.noSpawnZoneCheckRadius,
                ~0,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < zoneHits.Length; i++)
            {
                if (zoneHits[i] == null)
                {
                    continue;
                }

                NoSpawnZone zone = zoneHits[i].GetComponentInParent<NoSpawnZone>();

                if (zone == null)
                {
                    continue;
                }

                if (!ZoneBlocksFilter(zone, s.noSpawnFilter))
                {
                    continue;
                }

                failReason = "inside NoSpawnZone: " + zone.zoneName;
                return false;
            }
        }

        return true;
    }

    private static bool ZoneBlocksFilter(NoSpawnZone zone, NoSpawnFilter filter)
    {
        switch (filter)
        {
            case NoSpawnFilter.Trees:    return zone.blockTreePatches;
            case NoSpawnFilter.Items:    return zone.blockItems;
            case NoSpawnFilter.Hazards:  return zone.blockHazards;
            case NoSpawnFilter.NPCChaos: return zone.blockNPCChaos;
            default: // Everything — reject if any flag is set.
                return zone.blockTreePatches || zone.blockItems
                    || zone.blockHazards || zone.blockNPCChaos;
        }
    }
}
