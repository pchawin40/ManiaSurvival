using System.Collections;
using UnityEngine;

public partial class PredatorClassManager
{
    public bool CastSwarmSpit()
    {
        if (!CanCastPrototypeAbility())
        {
            return false;
        }

        StartCoroutine(SwarmSpitRoutine());
        return true;
    }

    public bool CastSwarmBrood()
    {
        if (!CanCastPrototypeAbility())
        {
            return false;
        }

        PruneBroodlingList();
        int spawnCount = Mathf.Max(1, swarmBroodSpawnCount);
        int available = Mathf.Max(0, maxActiveBroodlings - activeBroodlings.Count);
        if (available <= 0)
        {
            LogPredatorAbility("Brood rejected: max broodlings active.");
            return false;
        }

        int toSpawn = Mathf.Min(spawnCount, available);
        for (int i = 0; i < toSpawn; i++)
        {
            SpawnBroodlingAtIndex(i, toSpawn);
        }

        LogPredatorAbility("Brood spawned " + toSpawn + " broodlings (cap " + maxActiveBroodlings + ", cost " + swarmBroodManaCost.ToString("0") + " mana)");
        return true;
    }

    public bool CastSwarmInfest()
    {
        if (!CanCastPrototypeAbility())
        {
            return false;
        }

        StartCoroutine(SwarmInfestRoutine());
        return true;
    }

    public bool CastSwarmHive()
    {
        if (!CanCastPrototypeAbility() || IsSwarmHiveActive())
        {
            return false;
        }

        if (!TryBeginSwarmHive())
        {
            return false;
        }

        swarmHiveRoutine = StartCoroutine(SwarmHiveRoutine());
        return swarmHiveRoutine != null;
    }

    private IEnumerator SwarmSpitRoutine()
    {
        Vector3 origin = GetChestCastOrigin();
        Vector3 forward = GetFlatForward(logAim: true);
        float range = Mathf.Max(2f, swarmSpitRange);
        Vector3 target = origin + forward * range;
        target.y = transform.position.y;

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnWarningCircle(origin + forward * 1.2f, 1.1f, swarmSpitWindUp, new Color(0.45f, 0.95f, 0.25f, 0.45f));
        }

        yield return new WaitForSeconds(Mathf.Max(0.05f, swarmSpitWindUp));

        if (!CanCastPrototypeAbility())
        {
            yield break;
        }

        float travel = Mathf.Max(0.1f, swarmSpitTravelTime);
        float elapsed = 0f;
        Vector3 lobStart = origin + Vector3.up * 1.1f;
        while (elapsed < travel)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travel);
            Vector3 flat = Vector3.Lerp(origin, target, t);
            float arc = 4f * swarmSpitLobHeight * t * (1f - t);
            Vector3 blobPos = flat + Vector3.up * (1.1f + arc);

            if (enableAbilityFeel && elapsed <= Time.deltaTime * 1.5f)
            {
                PredatorAbilityFeelVfx.SpawnSpitProjectile(blobPos, forward, 0.5f, new Color(0.55f, 1f, 0.3f, 0.85f), travel);
            }

            yield return null;
        }

        ApplySwarmSpitImpact(target);
    }

    private void ApplySwarmSpitImpact(Vector3 impactPoint)
    {
        float radius = Mathf.Max(0.5f, swarmSpitImpactRadius);
        int damage = Mathf.Max(1, swarmSpitDamage);
        UnitHealth[] hits = GetSurvivorsInRange(impactPoint, radius);

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth target = hits[i];
            target.TakeDamage(damage, gameObject);
            LogPredatorAbility("Swarm Spit impact damaged " + target.name + " for " + damage + " (single hit)");

            if (swarmSpitAppliesSlow)
            {
                ApplySurvivorSlow(target, swarmSpitSlowMultiplier, swarmSpitSlowDuration);
            }

            SpawnHitMarkerVfx(target.transform.position + Vector3.up * 1f, 0.35f);
        }

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnShockwaveRing(impactPoint, radius, new Color(0.45f, 0.95f, 0.25f, 0.5f), 0.35f);
            PredatorAbilityFeelVfx.SpawnSpitProjectile(impactPoint + Vector3.up * 0.4f, Vector3.up, 0.35f, new Color(0.35f, 0.85f, 0.18f, 0.75f), 0.4f);
        }

        if (swarmSpitPuddleDuration > 0f && swarmSpitPuddleDps > 0f)
        {
            PredatorSurvivorZone.Spawn(
                impactPoint,
                radius * 0.85f,
                swarmSpitPuddleDuration,
                swarmSpitPuddleDps,
                swarmSpitSlowMultiplier,
                0.75f,
                survivorTag,
                targetLayers,
                gameObject,
                new Color(0.35f, 0.85f, 0.18f, 0.35f));
        }

        LogPredatorAbility("Swarm Spit splat hit " + hits.Length + " survivors");
    }

    private IEnumerator SwarmInfestRoutine()
    {
        Vector3 zonePos = GetAbilityGroundTarget(swarmInfestPlacementDistance);
        float warning = Mathf.Max(0.2f, swarmInfestWarningDuration);

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnWarningCircle(zonePos, swarmInfestRadius, warning, new Color(0.4f, 0.9f, 0.25f, 0.55f));
        }

        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner != null)
        {
            // Visual + slow only — gameplay damage comes from PredatorSurvivorZone below (avoids double DPS bug).
            spawner.SpawnPoisonZone(
                zonePos,
                swarmInfestRadius,
                swarmInfestDuration,
                0f,
                swarmInfestSlowMultiplier);
        }

        yield return new WaitForSeconds(warning);

        if (!CanCastPrototypeAbility())
        {
            yield break;
        }

        PredatorSurvivorZone.Spawn(
            zonePos,
            swarmInfestRadius,
            swarmInfestDuration,
            swarmInfestDps,
            swarmInfestSlowMultiplier,
            0.75f,
            survivorTag,
            targetLayers,
            gameObject,
            new Color(0.35f, 0.85f, 0.2f, 0.45f));

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnShockwaveRing(zonePos, swarmInfestRadius * 0.65f, new Color(0.4f, 0.9f, 0.25f, 0.45f), 0.35f);
        }

        LogPredatorAbility("Swarm Infest bloomed at " + zonePos + " (dps=" + swarmInfestDps.ToString("0.0") + ", single zone source)");
    }

    private bool TryBeginSwarmHive()
    {
        if (IsSwarmHiveActive())
        {
            return false;
        }

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy || unitHealth == null || unitHealth.IsDead)
        {
            return false;
        }

        if (!IsRoundActive())
        {
            return false;
        }

        isSwarmHiveActive = true;
        return true;
    }

    private IEnumerator SwarmHiveRoutine()
    {
        Vector3 center = GetAbilityGroundTarget(swarmHiveRadius * 0.5f);
        float warning = Mathf.Max(0.1f, swarmHiveWarningDuration);

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnWarningCircle(center, swarmHiveRadius, warning, new Color(0.55f, 0.95f, 0.2f, 0.55f));
        }

        yield return new WaitForSeconds(warning);

        int pulses = Mathf.Max(1, swarmHivePulseCount);
        for (int pulse = 0; pulse < pulses; pulse++)
        {
            if (!isActiveAndEnabled || unitHealth == null || unitHealth.IsDead || !IsRoundActive())
            {
                break;
            }

            ApplySwarmHivePulse(center);
            if (pulse < pulses - 1)
            {
                yield return new WaitForSeconds(Mathf.Max(0.25f, swarmHivePulseInterval));
            }
        }

        int broodToSpawn = Mathf.Max(0, swarmHiveBroodSpawnCount);
        for (int i = 0; i < broodToSpawn; i++)
        {
            if (GetActiveBroodlingCount() >= maxActiveBroodlings)
            {
                break;
            }

            SpawnBroodlingAtIndex(i, broodToSpawn);
        }

        isSwarmHiveActive = false;
        swarmHiveRoutine = null;
        LogPredatorAbility("Swarm Hive ended");
    }

    private void ApplySwarmHivePulse(Vector3 center)
    {
        int damage = Mathf.Max(1, swarmHivePulseDamage);
        UnitHealth[] hits = GetSurvivorsInRange(center, swarmHiveRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            hits[i].TakeDamage(damage, gameObject);
            SpawnHitMarkerVfx(hits[i].transform.position + Vector3.up * 1f, 0.3f);
            LogPredatorAbility("Swarm Hive pulse damaged " + hits[i].name + " for " + damage + " (single pulse hit)");
        }

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnShockwaveRing(center, swarmHiveRadius, new Color(0.5f, 1f, 0.25f, 0.55f), 0.45f);
        }

        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner != null)
        {
            spawner.SpawnPoisonZone(center, swarmHiveRadius * 0.7f, swarmHivePulseInterval + 0.5f, 0f, 0.72f);
        }

        LogPredatorAbility("Swarm Hive pulse hit " + hits.Length);
    }

    private void SpawnBroodlingAtIndex(int index, int total)
    {
        float angleStep = total <= 1 ? 0f : 360f / total;
        float yaw = angleStep * index + Random.Range(-12f, 12f);
        Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * GetFlatForward() * broodSpawnOffsetRadius;
        Vector3 spawnPos = ResolveBroodSpawnPosition(transform.position + offset);

        GameObject prefab = broodlingPrefab != null ? broodlingPrefab : minionPrefab;
        GameObject minionObj;
        if (prefab != null)
        {
            minionObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        }
        else
        {
            minionObj = BroodlingMinion.SpawnRuntimeCapsule(spawnPos, new Color(0.4f, 0.9f, 0.25f, 1f), broodlingScale);
        }

        minionObj.transform.localScale = Vector3.one * Mathf.Clamp(broodlingScale, 0.45f, 0.85f);

        BroodlingMinion broodling = minionObj.GetComponent<BroodlingMinion>();
        if (broodling == null)
        {
            broodling = minionObj.AddComponent<BroodlingMinion>();
        }

        broodling.Initialize(
            this,
            targetLayers,
            survivorTag,
            broodlingLifetime,
            broodlingDamage,
            broodlingMoveSpeed,
            broodlingHatchDuration,
            broodlingContactInterval);
        SpawnSwarmBroodNest(spawnPos);
    }

    private Vector3 ResolveBroodSpawnPosition(Vector3 desired)
    {
        desired.y = transform.position.y;
        Collider[] overlaps = Physics.OverlapSphere(desired, 0.9f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlaps.Length; i++)
        {
            UnitHealth unit = overlaps[i].GetComponentInParent<UnitHealth>();
            if (unit != null && !unit.IsDead)
            {
                Vector3 push = desired - unit.transform.position;
                push.y = 0f;
                if (push.sqrMagnitude <= 0.01f)
                {
                    push = Random.insideUnitSphere;
                    push.y = 0f;
                }

                desired = unit.transform.position + push.normalized * 1.6f;
            }
        }

        if (ArenaBounds.Instance != null)
        {
            desired = ArenaBounds.Instance.ClampPosition(desired);
        }

        return desired;
    }

    private bool CanCastPrototypeAbility()
    {
        return unitHealth != null && !unitHealth.IsDead && IsRoundActive();
    }
}
