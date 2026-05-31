using System.Collections;
using UnityEngine;

public partial class PredatorClassManager
{
    [Header("Iron Colossus - Identity")]
    [TextArea(2, 4)]
    public string ironColossusClassSummary = "Iron Colossus — Tank predator. Crush, guard, quake, then fortify the arena.";

    public AbilityDetail ironSlot1Detail;
    public AbilityDetail ironSlot2Detail;
    public AbilityDetail ironSlot3Detail;
    public AbilityDetail ironSlot4Detail;

    [Header("Iron Colossus - Crush")]
    public float ironCrushRadius = 3f;
    public int ironCrushDamage = 16;

    [Header("Iron Colossus - Guard")]
    public float ironGuardDuration = 3f;
    public float ironGuardDamageMultiplier = 0.45f;

    [Header("Iron Colossus - Quake")]
    public float ironQuakeRadius = 5.5f;
    public int ironQuakeDamage = 12;
    public float ironQuakeKnockback = 5.5f;
    public float ironQuakeSlowMultiplier = 0.65f;
    public float ironQuakeSlowDuration = 1.5f;

    [Header("Iron Colossus - Fort")]
    public float ironFortRadius = 6f;
    public float ironFortWarningDuration = 0.9f;
    public float ironFortDuration = 6f;
    public float ironFortPulseInterval = 1.1f;
    public int ironFortPulseDamage = 7;
    public float ironFortPlacementDistance = 5f;

    private Coroutine ironGuardRoutine;
    private bool isIronGuardActive;
    private Coroutine ironFortRoutine;
    private bool isIronFortActive;

    public bool CastIronCrush()
    {
        if (!CanCastPrototypeAbility())
        {
            return false;
        }

        Vector3 center = transform.position + transform.forward * 0.8f;
        float radius = Mathf.Max(0.5f, ironCrushRadius);
        UnitHealth[] hits = GetSurvivorsInRange(center, radius);

        for (int i = 0; i < hits.Length; i++)
        {
            hits[i].TakeDamage(ironCrushDamage, gameObject);
            SpawnHitMarkerVfx(hits[i].transform.position + Vector3.up * 1f, 0.35f);
        }

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnShockwaveRing(center, radius, new Color(0.55f, 0.65f, 0.85f, 0.55f), 0.4f);
        }

        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner != null)
        {
            spawner.SpawnCrater(center, radius * 0.45f, 4f);
        }

        LogPredatorAbility("Iron Crush hit " + hits.Length);
        return true;
    }

    public bool CastIronGuard()
    {
        if (!CanCastPrototypeAbility() || isIronGuardActive)
        {
            return false;
        }

        ironGuardRoutine = StartCoroutine(IronGuardRoutine());
        return ironGuardRoutine != null;
    }

    public bool CastIronQuake()
    {
        if (!CanCastPrototypeAbility())
        {
            return false;
        }

        Vector3 center = transform.position;
        float radius = Mathf.Max(0.5f, ironQuakeRadius);
        UnitHealth[] hits = GetSurvivorsInRange(center, radius);

        for (int i = 0; i < hits.Length; i++)
        {
            hits[i].TakeDamage(ironQuakeDamage, gameObject);
            Vector3 knockDir = hits[i].transform.position - center;
            ApplyKnockback(hits[i], knockDir, ironQuakeKnockback);
            ApplySurvivorSlow(hits[i], ironQuakeSlowMultiplier, ironQuakeSlowDuration);
            SpawnHitMarkerVfx(hits[i].transform.position + Vector3.up * 1f, 0.35f);
        }

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnShockwaveRing(center, radius, new Color(0.62f, 0.72f, 0.9f, 0.6f), 0.55f);
        }

        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner != null)
        {
            spawner.SpawnCrater(center, radius * 0.5f, 6f);
            spawner.SpawnSlowZone(center, radius * 0.85f, 2f, 0.78f);
        }

        LogPredatorAbility("Iron Quake hit " + hits.Length);
        return true;
    }

    public bool CastIronFort()
    {
        if (!CanCastPrototypeAbility() || IsIronFortActive())
        {
            return false;
        }

        ironFortRoutine = StartCoroutine(IronFortRoutine());
        return ironFortRoutine != null;
    }

    internal bool IsIronFortActive()
    {
        return isIronFortActive || ironFortRoutine != null;
    }

    private void StopIronClassState(string reason)
    {
        if (ironGuardRoutine != null)
        {
            StopCoroutine(ironGuardRoutine);
            ironGuardRoutine = null;
        }

        isIronGuardActive = false;

        if (ironFortRoutine != null)
        {
            StopCoroutine(ironFortRoutine);
            ironFortRoutine = null;
        }

        isIronFortActive = false;
    }

    private IEnumerator IronGuardRoutine()
    {
        isIronGuardActive = true;
        if (unitHealth != null && !unitHealth.IsDead)
        {
            unitHealth.Heal(Mathf.Max(1, Mathf.RoundToInt(unitHealth.maxHealth * 0.12f)));
        }

        SpawnSelfBurstVfx(new Color(0.45f, 0.55f, 0.75f, 0.7f), 0.45f);

        float endTime = Time.time + Mathf.Max(0.1f, ironGuardDuration);
        while (Time.time < endTime)
        {
            if (!isActiveAndEnabled || unitHealth == null || unitHealth.IsDead)
            {
                break;
            }

            yield return null;
        }

        isIronGuardActive = false;
        ironGuardRoutine = null;
        LogPredatorAbility("Iron Guard ended");
    }

    internal float GetIronGuardDamageMultiplier()
    {
        return isIronGuardActive ? ironGuardDamageMultiplier : 1f;
    }

    private IEnumerator IronFortRoutine()
    {
        isIronFortActive = true;
        Vector3 center = GetAbilityGroundTarget(ironFortPlacementDistance);
        float warning = Mathf.Max(0.1f, ironFortWarningDuration);
        float radius = Mathf.Max(0.5f, ironFortRadius);

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnWarningCircle(center, radius, warning, new Color(0.5f, 0.6f, 0.78f, 0.55f));
        }

        yield return new WaitForSeconds(warning);

        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner != null)
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f + 45f;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 pos = center + dir * (radius * 0.75f);
                if (i % 2 == 0)
                {
                    spawner.SpawnRockObstacle(pos, ironFortDuration);
                }
                else
                {
                    spawner.SpawnVineWall(pos, Quaternion.LookRotation(dir), 2f, ironFortDuration);
                }
            }
        }

        float endTime = Time.time + Mathf.Max(0.5f, ironFortDuration);
        while (Time.time < endTime)
        {
            if (!isActiveAndEnabled || unitHealth == null || unitHealth.IsDead || !IsRoundActive())
            {
                break;
            }

            ApplyIronFortPulse(center, radius);
            yield return new WaitForSeconds(Mathf.Max(0.25f, ironFortPulseInterval));
        }

        isIronFortActive = false;
        ironFortRoutine = null;
        LogPredatorAbility("Iron Fort ended");
    }

    private void ApplyIronFortPulse(Vector3 center, float radius)
    {
        UnitHealth[] hits = GetSurvivorsInRange(center, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            hits[i].TakeDamage(ironFortPulseDamage, gameObject);
            SpawnHitMarkerVfx(hits[i].transform.position + Vector3.up * 1f, 0.3f);
        }

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnShockwaveRing(center, radius * 0.85f, new Color(0.58f, 0.68f, 0.88f, 0.45f), 0.4f);
        }
    }

    private void EnsureIronAbilityDetails()
    {
        if (!ironSlot1Detail.IsConfigured)
        {
            ironSlot1Detail = AbilityPresentationFallback.GetIronColossusDetail(1);
        }

        if (!ironSlot2Detail.IsConfigured)
        {
            ironSlot2Detail = AbilityPresentationFallback.GetIronColossusDetail(2);
        }

        if (!ironSlot3Detail.IsConfigured)
        {
            ironSlot3Detail = AbilityPresentationFallback.GetIronColossusDetail(3);
        }

        if (!ironSlot4Detail.IsConfigured)
        {
            ironSlot4Detail = AbilityPresentationFallback.GetIronColossusDetail(4);
        }
    }

    private AbilityDetail GetIronSlotDetail(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1: return ironSlot1Detail;
            case 2: return ironSlot2Detail;
            case 3: return ironSlot3Detail;
            default: return ironSlot4Detail;
        }
    }
}
