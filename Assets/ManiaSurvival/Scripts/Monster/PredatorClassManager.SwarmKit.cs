using System.Collections;
using System.Text;
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
        Debug.Log("[SwarmCast] Brood cast requested.");

        if (!CanCastPrototypeAbility())
        {
            Debug.Log("[SwarmSpawnFail] reason=predator dead or round not active.");
            return false;
        }

        UnitMana mana = GetComponent<UnitMana>();
        float manaBefore = mana != null ? mana.currentMana : -1f;
        Debug.Log("[SwarmCast] Mana before=" + manaBefore.ToString("0") + " cost=" + swarmBroodManaCost.ToString("0"));

        PruneBroodlingList();
        int activeBefore = CleanupActiveBroodlingsWithLog();
        Debug.Log("[SwarmCast] Active brood count before=" + activeBefore + " max=" + maxActiveBroodlings);

        int spawnCount = Mathf.Max(1, swarmBroodSpawnCount);
        int available = GetAvailableBroodSlots();
        if (available <= 0)
        {
            Debug.Log("[AbilityBlock] Brood blocked: active brood cap reached " + activeBefore + "/" + maxActiveBroodlings);
            LogPredatorAbility("Brood rejected: max broodlings active.");
            return false;
        }

        int toSpawn = Mathf.Min(spawnCount, available);
        Debug.Log("[SwarmCast] Attempting to spawn " + toSpawn + " broodlings.");

        LogBroodSpawnPoints(toSpawn);

        int spawned = 0;
        for (int i = 0; i < toSpawn; i++)
        {
            if (SpawnSafeBroodling(i, toSpawn, "Brood"))
            {
                spawned++;
            }
        }

        if (spawned <= 0)
        {
            Debug.Log("[SwarmSpawnFail] reason=all broodling spawn attempts failed.");
            return false;
        }

        Debug.Log("[SwarmCast] Brood requested " + spawnCount + ", spawned " + spawned + ".");
        LogPredatorAbility("Brood spawned " + spawned + " broodlings (cap " + maxActiveBroodlings + ", cost " + swarmBroodManaCost.ToString("0") + " mana)");
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
        Debug.Log("[SwarmCast] Hive cast requested.");

        if (!CanCastPrototypeAbility() || IsSwarmHiveActive())
        {
            Debug.Log("[SwarmSpawnFail] reason=hive cast blocked (round inactive or hive already active).");
            return false;
        }

        if (!TryBeginSwarmHive())
        {
            Debug.Log("[SwarmSpawnFail] reason=TryBeginSwarmHive failed.");
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

        int broodRequested = Mathf.Max(0, swarmHiveBroodSpawnCount);
        int activeBeforeHive = CleanupActiveBroodlingsWithLog();
        int hiveAvailable = Mathf.Max(0, maxActiveBroodlings - activeBeforeHive);
        int broodToSpawn = Mathf.Min(broodRequested, hiveAvailable);

        if (broodToSpawn <= 0)
        {
            Debug.Log("[SwarmCast] Hive brood spawn skipped: active cap reached " + activeBeforeHive + "/" + maxActiveBroodlings);
        }
        else
        {
            Debug.Log("[SwarmCast] Hive requested " + broodRequested + " broodlings, spawning " + broodToSpawn
                + " (active " + activeBeforeHive + "/" + maxActiveBroodlings + ").");
        }

        int hiveSpawned = 0;
        for (int i = 0; i < broodToSpawn; i++)
        {
            if (SpawnSafeBroodling(i, broodToSpawn, "Hive"))
            {
                hiveSpawned++;
            }
        }

        Debug.Log("[SwarmCast] Hive requested " + broodRequested + " broodlings, spawned " + hiveSpawned
            + " due to active cap " + GetActiveBroodlingCount() + "/" + maxActiveBroodlings);

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

    private void LogBroodSpawnPoints(int total)
    {
        for (int i = 0; i < total; i++)
        {
            if (TryResolveBroodSpawnPosition(i, total, out Vector3 spawnPos, out _))
            {
                Debug.Log("[SwarmCast] Spawn point " + (i + 1) + " = " + spawnPos.ToString("F2"));
            }
            else
            {
                Debug.Log("[SwarmCast] Spawn point " + (i + 1) + " = unresolved (will use fallback ring search)");
            }
        }
    }

    private bool SpawnSafeBroodling(int index, int total, string sourceAbility)
    {
        Debug.Log("[SwarmSpawn] " + sourceAbility + " broodling spawn attempt index=" + index
            + " position=pending");

        if (!TryResolveBroodSpawnPosition(index, total, out Vector3 spawnPos, out string resolveFailReason))
        {
            Debug.Log("[SwarmSpawnFail] reason=no valid spawn position for index " + index + " (" + resolveFailReason + ").");
            return false;
        }

        Debug.Log("[SwarmSpawn] Broodling spawn attempt index=" + index + " position=" + spawnPos.ToString("F2"));

        bool groundOk = TrySnapBroodlingToGround(ref spawnPos, out float groundY, out string groundFailReason);
        Debug.Log("[SwarmSpawn] Ground check result=" + (groundOk ? "success" : "fail")
            + " groundY=" + groundY.ToString("F2")
            + (groundOk ? string.Empty : " reason=" + groundFailReason));

        bool forbidden = PlayableBoundsHelper.TryGetForbiddenSpawnPositionReason(spawnPos, 0.85f, out string forbiddenReason);
        Debug.Log("[SwarmSpawn] Forbidden zone check=" + (forbidden ? "warn" : "success")
            + (forbidden ? " reason=" + forbiddenReason : string.Empty));
        if (forbidden)
        {
            Debug.LogWarning("[SwarmSpawn] Spawning broodling near forbidden zone anyway at " + spawnPos.ToString("F2"));
        }

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnWarningCircle(
                spawnPos,
                0.85f,
                broodlingHatchDuration,
                new Color(0.45f, 0.95f, 0.25f, 0.5f));
        }

        SpawnBroodHatchMarker(spawnPos, broodlingHatchDuration + 0.15f);

        GameObject minionObj;
        string prefabLabel;
        bool usedPrefab = TryInstantiateSafeBroodlingPrefab(spawnPos, out minionObj, out prefabLabel);
        if (!usedPrefab)
        {
            prefabLabel = "runtime capsule";
            minionObj = BroodlingMinion.SpawnRuntimeCapsule(
                spawnPos,
                new Color(0.35f, 0.95f, 0.22f, 1f),
                broodlingScale);
        }

        if (minionObj == null)
        {
            Debug.Log("[SwarmSpawnFail] reason=spawn returned null object.");
            return false;
        }

        minionObj.transform.SetParent(null, true);
        minionObj.transform.position = spawnPos;
        minionObj.transform.localScale = Vector3.one * Mathf.Clamp(broodlingScale, 0.45f, 0.55f);

        Debug.Log("[SwarmSpawn] Prefab used=" + prefabLabel);

        BroodlingMinion.SanitizeLegacyComponents(minionObj);
        BroodlingMinion.EnsureBroodlingPhysics(minionObj);

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
            broodlingContactInterval,
            broodlingMaxHealth,
            broodlingFadeOutDuration,
            index + 1,
            broodlingPostHatchBiteDelay);

        minionObj.SetActive(true);

        LogBroodlingSpawnResult(minionObj, broodling, sourceAbility, prefabLabel);
        SpawnSwarmBroodNest(spawnPos);
        return true;
    }

    private static void LogBroodlingSpawnResult(GameObject minionObj, BroodlingMinion broodling, string sourceAbility, string prefabLabel)
    {
        StringBuilder componentList = new StringBuilder();
        MonoBehaviour[] behaviours = minionObj.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null)
            {
                continue;
            }

            if (componentList.Length > 0)
            {
                componentList.Append(", ");
            }

            componentList.Append(behaviours[i].GetType().Name);
        }

        UnitHealth health = minionObj.GetComponent<UnitHealth>();
        Debug.Log("[SwarmSpawn] Spawned object=" + minionObj.name
            + " active=" + minionObj.activeInHierarchy
            + " position=" + minionObj.transform.position.ToString("F2")
            + " scale=" + minionObj.transform.localScale.ToString("F2"));
        Debug.Log("[SwarmSpawn] Components=" + componentList);
        Debug.Log("[SwarmSpawn] UnitHealth=" + (health != null ? "exists" : "missing")
            + " hp=" + (health != null ? health.currentHealth + "/" + health.maxHealth : "n/a"));
        Debug.Log("[SwarmSpawn] BroodlingMinion=" + (broodling != null ? "exists" : "missing")
            + " damage=" + (broodling != null ? broodling.contactDamage.ToString() : "n/a")
            + " interval=" + (broodling != null ? broodling.contactInterval.ToString("0.00") + "s" : "n/a")
            + " speed=" + (broodling != null ? broodling.moveSpeed.ToString("0.0") : "n/a")
            + " lifetime=" + (broodling != null ? broodling.lifetime.ToString("0.0") + "s" : "n/a")
            + " source=" + sourceAbility + " prefab=" + prefabLabel);
    }

    private void SpawnBroodHatchMarker(Vector3 position, float lifetime)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "BroodHatchMarker";
        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null)
        {
            Destroy(markerCollider);
        }

        marker.transform.position = position + Vector3.up * 0.2f;
        marker.transform.localScale = Vector3.one * 0.38f;
        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = BroodlingMinion.CreateBroodlingMaterial(new Color(0.25f, 0.55f, 0.15f, 0.85f));
        }

        Destroy(marker, Mathf.Max(0.2f, lifetime));
    }

    private bool TryInstantiateSafeBroodlingPrefab(Vector3 spawnPos, out GameObject minionObj, out string prefabLabel)
    {
        minionObj = null;
        prefabLabel = "none";
        if (!IsSafeBroodlingPrefab(broodlingPrefab))
        {
            if (broodlingPrefab != null)
            {
                Debug.LogWarning("[SwarmSpawn] Rejected unsafe broodlingPrefab '" + broodlingPrefab.name + "'.");
            }

            return false;
        }

        minionObj = Instantiate(broodlingPrefab, spawnPos, Quaternion.identity);
        prefabLabel = broodlingPrefab.name;
        return true;
    }

    private bool IsSafeBroodlingPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            return false;
        }

        if (minionPrefab != null && prefab == minionPrefab)
        {
            return false;
        }

        if (prefab.name.IndexOf("TrackingMinion", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        if (prefab.GetComponentInChildren<TrackingMinion>(true) != null)
        {
            return false;
        }

        if (prefab.GetComponentInChildren<MonsterAttack>(true) != null)
        {
            return false;
        }

        return true;
    }

    private bool TryResolveBroodSpawnPosition(int index, int total, out Vector3 spawnPos, out string failReason)
    {
        failReason = string.Empty;
        float angleStep = total <= 1 ? 0f : 360f / total;
        float yaw = angleStep * index + Random.Range(-12f, 12f);
        Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * GetFlatForward() * broodSpawnOffsetRadius;
        Vector3 desired = transform.position + offset;

        if (TryFinalizeBroodSpawnCandidate(desired, out spawnPos))
        {
            return true;
        }

        const int fallbackAttempts = 12;
        for (int attempt = 0; attempt < fallbackAttempts; attempt++)
        {
            float ringAngle = attempt * (360f / fallbackAttempts);
            float ringRadius = Random.Range(1.5f, 3.5f);
            Vector3 ringOffset = Quaternion.Euler(0f, ringAngle, 0f) * GetFlatForward() * ringRadius;
            desired = transform.position + ringOffset;
            if (TryFinalizeBroodSpawnCandidate(desired, out spawnPos))
            {
                Debug.LogWarning("[SwarmSpawn] Used fallback ring spawn for index " + index + " at " + spawnPos.ToString("F2"));
                return true;
            }
        }

        Vector3 emergency = transform.position + GetFlatForward() * 1.5f;
        if (TryFinalizeBroodSpawnCandidate(emergency, out spawnPos))
        {
            Debug.LogWarning("[SwarmSpawn] Used emergency forward spawn for index " + index + " at " + spawnPos.ToString("F2"));
            return true;
        }

        failReason = "no valid candidate after ring search";
        spawnPos = transform.position;
        return false;
    }

    private bool TryFinalizeBroodSpawnCandidate(Vector3 desired, out Vector3 spawnPos)
    {
        spawnPos = ResolveBroodSpawnPosition(desired);

        if (!PlayableBoundsHelper.IsPositionInsidePlayableBounds(spawnPos))
        {
            spawnPos = PlayableBoundsHelper.ClampToPlayableBounds(spawnPos);
        }

        return true;
    }

    private bool TrySnapBroodlingToGround(ref Vector3 position, out float groundY, out string failReason)
    {
        failReason = string.Empty;
        Vector3 rayStart = position + Vector3.up * 12f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 24f, ~0, QueryTriggerInteraction.Ignore))
        {
            groundY = hit.point.y;
            position.y = groundY + Mathf.Max(0.35f, broodlingGroundClearance);
            return true;
        }

        groundY = transform.position.y;
        position.y = groundY;
        failReason = "no ground raycast hit — using predator Y";
        return false;
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
