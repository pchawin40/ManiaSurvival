using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ground zone that damages and slows survivors only. Ticks on an interval to avoid per-frame hit spam.
/// </summary>
[DisallowMultipleComponent]
public class PredatorSurvivorZone : MonoBehaviour
{
    [Header("Zone")]
    public float radius = 4.5f;
    public float duration = 5f;
    public float damagePerSecond = 4f;
    public float survivorSlowMultiplier = 0.65f;
    public float slowRefreshDuration = 0.75f;
    public float tickInterval = 0.75f;

    [Header("Targets")]
    public string survivorTag = "Survivor";
    public LayerMask targetLayers = ~0;

    private GameObject damageSource;
    private float endTime;
    private float nextTickTime;
    private readonly Dictionary<UnitHealth, float> pendingDamage = new Dictionary<UnitHealth, float>();
    private readonly HashSet<UnitHealth> survivorsHitThisTick = new HashSet<UnitHealth>();

    public static PredatorSurvivorZone Spawn(
        Vector3 worldPosition,
        float zoneRadius,
        float zoneDuration,
        float dps,
        float slowMultiplier,
        float tickSeconds,
        string tag,
        LayerMask layers,
        GameObject source,
        Color tint)
    {
        GameObject host = new GameObject("PredatorSurvivorZone");
        host.transform.position = SnapToGround(worldPosition);

        PredatorSurvivorZone zone = host.AddComponent<PredatorSurvivorZone>();
        zone.radius = Mathf.Max(0.5f, zoneRadius);
        zone.duration = Mathf.Max(0.1f, zoneDuration);
        zone.damagePerSecond = Mathf.Max(0f, dps);
        zone.survivorSlowMultiplier = Mathf.Clamp(slowMultiplier, 0.1f, 1f);
        zone.tickInterval = Mathf.Clamp(tickSeconds, 0.5f, 1f);
        zone.survivorTag = tag;
        zone.targetLayers = layers;
        zone.damageSource = source;
        zone.endTime = Time.time + zone.duration;
        zone.nextTickTime = Time.time;

        TemporaryGroundEffect.Spawn(
            host.transform.position,
            tint,
            zone.duration,
            zone.radius,
            null,
            false);

        return zone;
    }

    private static Vector3 SnapToGround(Vector3 worldPosition)
    {
        Vector3 rayStart = worldPosition + Vector3.up * 8f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        return worldPosition;
    }

    private void Update()
    {
        if (Time.time >= endTime)
        {
            Destroy(gameObject);
            return;
        }

        if (Time.time < nextTickTime)
        {
            return;
        }

        nextTickTime = Time.time + tickInterval;
        TickZone();
    }

    private void TickZone()
    {
        survivorsHitThisTick.Clear();
        float safeRadius = Mathf.Max(0.1f, radius);
        Collider[] hits = Physics.OverlapSphere(transform.position, safeRadius, targetLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth survivor = hits[i].GetComponentInParent<UnitHealth>();
            if (!IsValidSurvivor(survivor))
            {
                continue;
            }

            Vector3 offset = survivor.transform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > safeRadius * safeRadius)
            {
                continue;
            }

            survivorsHitThisTick.Add(survivor);
            ApplySlowToSurvivor(survivor);

            if (damagePerSecond <= 0f)
            {
                continue;
            }

            float pending = 0f;
            pendingDamage.TryGetValue(survivor, out pending);
            pending += damagePerSecond * tickInterval;

            while (pending >= 1f)
            {
                int damage = Mathf.FloorToInt(pending);
                pending -= damage;
                survivor.TakeDamage(damage, damageSource != null ? damageSource : gameObject);
            }

            pendingDamage[survivor] = pending;
        }

        List<UnitHealth> stale = null;
        foreach (KeyValuePair<UnitHealth, float> pair in pendingDamage)
        {
            if (pair.Key == null || pair.Key.IsDead || !survivorsHitThisTick.Contains(pair.Key))
            {
                stale ??= new List<UnitHealth>();
                stale.Add(pair.Key);
            }
        }

        if (stale != null)
        {
            for (int i = 0; i < stale.Count; i++)
            {
                pendingDamage.Remove(stale[i]);
            }
        }
    }

    private bool IsValidSurvivor(UnitHealth health)
    {
        return health != null && !health.IsDead && health.CompareTag(survivorTag);
    }

    private void ApplySlowToSurvivor(UnitHealth survivor)
    {
        if (survivor == null || survivor.IsDead || survivorSlowMultiplier >= 0.99f)
        {
            return;
        }

        SurvivorMovement movement = survivor.GetComponent<SurvivorMovement>();
        if (movement != null)
        {
            movement.ApplyTemporarySpeedMultiplier(survivorSlowMultiplier, slowRefreshDuration);
        }

        OfflineSurvivorBotAI bot = survivor.GetComponent<OfflineSurvivorBotAI>();
        if (bot != null)
        {
            bot.ApplyTemporarySpeedMultiplier(survivorSlowMultiplier, slowRefreshDuration);
        }
    }
}
