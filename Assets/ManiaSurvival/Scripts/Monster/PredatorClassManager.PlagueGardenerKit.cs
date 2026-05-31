using System.Collections;
using UnityEngine;

public partial class PredatorClassManager
{
    [Header("Plague Gardener - Identity")]
    [TextArea(2, 4)]
    public string plagueGardenerClassSummary = "Plague Gardener — Zone predator. Thorn, root, spore, then bloom the garden.";

    public AbilityDetail plagueSlot1Detail;
    public AbilityDetail plagueSlot2Detail;
    public AbilityDetail plagueSlot3Detail;
    public AbilityDetail plagueSlot4Detail;

    [Header("Plague Gardener - Thorn")]
    public float plagueThornRange = 10f;
    public float plagueThornHalfAngle = 35f;
    public int plagueThornDamage = 11;
    public float plagueThornSlowMultiplier = 0.72f;
    public float plagueThornSlowDuration = 1.2f;

    [Header("Plague Gardener - Root")]
    public float plagueRootRadius = 4f;
    public float plagueRootDuration = 2.5f;
    public float plagueRootPlacementDistance = 5f;

    [Header("Plague Gardener - Spore")]
    public float plagueSporeRadius = 4.5f;
    public float plagueSporeDuration = 5f;
    public float plagueSporeDps = 4f;
    public float plagueSporeSlowMultiplier = 0.62f;
    public float plagueSporePlacementDistance = 5.5f;

    [Header("Plague Gardener - Bloom")]
    public float plagueBloomRadius = 6.5f;
    public float plagueBloomWarningDuration = 0.85f;
    public int plagueBloomPulseDamage = 9;
    public float plagueBloomPulseInterval = 1f;
    public int plagueBloomPulseCount = 3;
    public float plagueBloomPlacementDistance = 6f;

    private Coroutine plagueBloomRoutine;
    private bool isPlagueBloomActive;

    public bool CastPlagueThorn()
    {
        if (!CanCastPrototypeAbility())
        {
            return false;
        }

        Vector3 origin = GetChestCastOrigin();
        Vector3 forward = GetFlatForward(logAim: true);
        UnitHealth[] hits = GetSurvivorsInCone(origin, forward, plagueThornRange, plagueThornHalfAngle);

        for (int i = 0; i < hits.Length; i++)
        {
            hits[i].TakeDamage(plagueThornDamage, gameObject);
            ApplySurvivorSlow(hits[i], plagueThornSlowMultiplier, plagueThornSlowDuration);
            SpawnHitMarkerVfx(hits[i].transform.position + Vector3.up * 1f, 0.35f);
        }

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnSpitProjectile(origin, forward, plagueThornRange, new Color(0.35f, 0.85f, 0.3f, 0.85f), 0.35f);
        }

        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner != null)
        {
            spawner.SpawnConeScorchMarks(origin, forward, plagueThornRange, plagueThornHalfAngle, 3, 0.8f, 2.5f);
        }

        LogPredatorAbility("Plague Thorn hit " + hits.Length);
        return true;
    }

    public bool CastPlagueRoot()
    {
        if (!CanCastPrototypeAbility())
        {
            return false;
        }

        Vector3 center = GetAbilityGroundTarget(plagueRootPlacementDistance);
        float radius = Mathf.Max(0.5f, plagueRootRadius);
        UnitHealth[] hits = GetSurvivorsInRange(center, radius);

        for (int i = 0; i < hits.Length; i++)
        {
            StartCoroutine(ApplySurvivorControlLock(hits[i], plagueRootDuration));
            ApplySurvivorSlow(hits[i], 0.35f, plagueRootDuration);
            SpawnHitMarkerVfx(hits[i].transform.position + Vector3.up * 0.8f, 0.4f);
        }

        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner != null)
        {
            spawner.SpawnSlowZone(center, radius, plagueRootDuration, 0.7f);
            spawner.SpawnVineWall(center + Vector3.forward * (radius * 0.35f), Quaternion.identity, 1.8f, plagueRootDuration);
        }

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnWarningCircle(center, radius, 0.35f, new Color(0.3f, 0.75f, 0.25f, 0.5f));
        }

        LogPredatorAbility("Plague Root hit " + hits.Length);
        return true;
    }

    public bool CastPlagueSpore()
    {
        if (!CanCastPrototypeAbility())
        {
            return false;
        }

        Vector3 zonePos = GetAbilityGroundTarget(plagueSporePlacementDistance);
        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner != null)
        {
            spawner.SpawnPoisonZone(
                zonePos,
                plagueSporeRadius,
                plagueSporeDuration,
                plagueSporeDps,
                plagueSporeSlowMultiplier);
        }

        PredatorSurvivorZone.Spawn(
            zonePos,
            plagueSporeRadius,
            plagueSporeDuration,
            plagueSporeDps,
            plagueSporeSlowMultiplier,
            0.75f,
            survivorTag,
            targetLayers,
            gameObject,
            new Color(0.35f, 0.82f, 0.28f, 0.45f));

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnWarningCircle(zonePos, plagueSporeRadius, 0.25f, new Color(0.4f, 0.9f, 0.3f, 0.5f));
        }

        LogPredatorAbility("Plague Spore at " + zonePos);
        return true;
    }

    public bool CastPlagueBloom()
    {
        if (!CanCastPrototypeAbility() || IsPlagueBloomActive())
        {
            return false;
        }

        plagueBloomRoutine = StartCoroutine(PlagueBloomRoutine());
        return plagueBloomRoutine != null;
    }

    internal bool IsPlagueBloomActive()
    {
        return isPlagueBloomActive || plagueBloomRoutine != null;
    }

    private void StopPlagueClassState(string reason)
    {
        if (plagueBloomRoutine != null)
        {
            StopCoroutine(plagueBloomRoutine);
            plagueBloomRoutine = null;
        }

        isPlagueBloomActive = false;
    }

    private IEnumerator PlagueBloomRoutine()
    {
        isPlagueBloomActive = true;
        Vector3 center = GetAbilityGroundTarget(plagueBloomPlacementDistance);
        float warning = Mathf.Max(0.1f, plagueBloomWarningDuration);
        float radius = Mathf.Max(0.5f, plagueBloomRadius);

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnWarningCircle(center, radius, warning, new Color(0.45f, 0.95f, 0.35f, 0.55f));
        }

        yield return new WaitForSeconds(warning);

        int pulses = Mathf.Max(1, plagueBloomPulseCount);
        for (int pulse = 0; pulse < pulses; pulse++)
        {
            if (!isActiveAndEnabled || unitHealth == null || unitHealth.IsDead || !IsRoundActive())
            {
                break;
            }

            ApplyPlagueBloomPulse(center, radius, pulse == 0);
            if (pulse < pulses - 1)
            {
                yield return new WaitForSeconds(Mathf.Max(0.25f, plagueBloomPulseInterval));
            }
        }

        isPlagueBloomActive = false;
        plagueBloomRoutine = null;
        LogPredatorAbility("Plague Bloom ended");
    }

    private void ApplyPlagueBloomPulse(Vector3 center, float radius, bool spawnWalls)
    {
        UnitHealth[] hits = GetSurvivorsInRange(center, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            hits[i].TakeDamage(plagueBloomPulseDamage, gameObject);
            ApplySurvivorSlow(hits[i], 0.6f, plagueBloomPulseInterval + 0.5f);
            SpawnHitMarkerVfx(hits[i].transform.position + Vector3.up * 1f, 0.3f);
        }

        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner != null)
        {
            spawner.SpawnPoisonZone(center, radius * 0.85f, plagueBloomPulseInterval + 0.5f, 3f, 0.65f);
            if (spawnWalls)
            {
                SpawnSwarmHiveChokepoint(center, radius);
            }
        }

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnShockwaveRing(center, radius, new Color(0.42f, 0.9f, 0.32f, 0.55f), 0.45f);
        }
    }

    private void EnsurePlagueAbilityDetails()
    {
        if (!plagueSlot1Detail.IsConfigured)
        {
            plagueSlot1Detail = AbilityPresentationFallback.GetPlagueGardenerDetail(1);
        }

        if (!plagueSlot2Detail.IsConfigured)
        {
            plagueSlot2Detail = AbilityPresentationFallback.GetPlagueGardenerDetail(2);
        }

        if (!plagueSlot3Detail.IsConfigured)
        {
            plagueSlot3Detail = AbilityPresentationFallback.GetPlagueGardenerDetail(3);
        }

        if (!plagueSlot4Detail.IsConfigured)
        {
            plagueSlot4Detail = AbilityPresentationFallback.GetPlagueGardenerDetail(4);
        }
    }

    private AbilityDetail GetPlagueSlotDetail(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1: return plagueSlot1Detail;
            case 2: return plagueSlot2Detail;
            case 3: return plagueSlot3Detail;
            default: return plagueSlot4Detail;
        }
    }
}
