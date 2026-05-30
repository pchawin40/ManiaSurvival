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

        Vector3 origin = GetChestCastOrigin();
        Vector3 forward = GetFlatForward(logAim: true);
        float range = Mathf.Max(1f, swarmSpitRange);
        float halfAngle = Mathf.Max(5f, swarmSpitHalfAngle);
        int damage = Mathf.Max(1, swarmSpitDamage);

        UnitHealth[] hits = GetSurvivorsInCone(origin, forward, range, halfAngle);
        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth target = hits[i];
            target.TakeDamage(damage, gameObject);
            if (swarmSpitAppliesSlow)
            {
                ApplySurvivorSlow(target, swarmSpitSlowMultiplier, swarmSpitSlowDuration);
            }

            SpawnHitMarkerVfx(target.transform.position + Vector3.up * 1f, 0.35f);
        }

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnFireCone(origin, forward, range, halfAngle, new Color(0.45f, 0.95f, 0.25f, 0.4f), 0.4f);
            PredatorAbilityFeelVfx.SpawnSpitProjectile(origin, forward, range, new Color(0.55f, 1f, 0.3f, 0.85f), 0.35f);
        }

        LogPredatorAbility("Swarm Spit hit " + hits.Length + " survivors");
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

        LogPredatorAbility("Brood spawned " + toSpawn + " broodlings");
        return true;
    }

    public bool CastSwarmInfest()
    {
        if (!CanCastPrototypeAbility())
        {
            return false;
        }

        Vector3 zonePos = GetAbilityGroundTarget(swarmInfestPlacementDistance);
        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner != null)
        {
            spawner.SpawnPoisonZone(
                zonePos,
                swarmInfestRadius,
                swarmInfestDuration,
                swarmInfestDps,
                swarmInfestSlowMultiplier);
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
            PredatorAbilityFeelVfx.SpawnWarningCircle(zonePos, swarmInfestRadius, 0.25f, new Color(0.4f, 0.9f, 0.25f, 0.5f));
        }

        LogPredatorAbility("Swarm Infest at " + zonePos);
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
            if (pulse == 0)
            {
                SpawnSwarmHiveChokepoint(center, swarmHiveRadius);
            }
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
        }

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnShockwaveRing(center, swarmHiveRadius, new Color(0.5f, 1f, 0.25f, 0.55f), 0.45f);
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
            minionObj = BroodlingMinion.SpawnRuntimeCapsule(spawnPos, new Color(0.4f, 0.9f, 0.25f, 1f));
        }

        BroodlingMinion broodling = minionObj.GetComponent<BroodlingMinion>();
        if (broodling == null)
        {
            broodling = minionObj.AddComponent<BroodlingMinion>();
        }

        broodling.Initialize(this, targetLayers, survivorTag, broodlingLifetime, broodlingDamage, broodlingMoveSpeed);
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
