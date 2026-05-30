using System.Collections;
using UnityEngine;

public partial class PredatorClassManager
{
    public bool CastJuggernautFlameBreath()
    {
        if (!CanCastPrototypeAbility())
        {
            return false;
        }

        Vector3 origin = GetChestCastOrigin();
        Vector3 forward = GetFlatForward(logAim: true);
        float range = Mathf.Max(1f, juggernautFlameRange);
        float halfAngle = Mathf.Max(5f, juggernautFlameHalfAngle);
        int damage = Mathf.Max(1, juggernautFlameDamage);

        UnitHealth[] hits = GetSurvivorsInCone(origin, forward, range, halfAngle);
        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth target = hits[i];
            target.TakeDamage(damage, gameObject);
            if (juggernautFlameAppliesBurn)
            {
                StartCoroutine(ApplyBleed(target, juggernautBurnDuration, juggernautBurnTickDamage));
            }

            SpawnHitMarkerVfx(target.transform.position + Vector3.up * 1f, 0.35f);
        }

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnFireCone(origin, forward, range, halfAngle, new Color(1f, 0.45f, 0.1f, 0.5f), 0.45f);
        }

        SpawnJuggernautFlameStrip(origin, forward, range, halfAngle);

        LogPredatorAbility("Dragon Flame hit " + hits.Length);
        return true;
    }

    public bool CastJuggernautLeap()
    {
        if (!CanCastPrototypeAbility() || isJuggernautLeaping)
        {
            return false;
        }

        if (!TryBeginDragonLeap(out Vector3 destination))
        {
            return false;
        }

        juggernautLeapRoutine = StartCoroutine(DragonLeapRoutine(destination));
        return juggernautLeapRoutine != null;
    }

    public bool CastJuggernautRoar()
    {
        if (!CanCastPrototypeAbility())
        {
            return false;
        }

        Vector3 center = transform.position;
        float radius = Mathf.Max(0.5f, juggernautRoarRadius);
        int damage = Mathf.Max(0, juggernautRoarDamage);
        UnitHealth[] hits = GetSurvivorsInRange(center, radius);

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth target = hits[i];
            if (damage > 0)
            {
                target.TakeDamage(damage, gameObject);
            }

            Vector3 knockDir = target.transform.position - center;
            knockDir.y = 0f;
            if (knockDir.sqrMagnitude <= 0.001f)
            {
                knockDir = GetFlatForward();
            }

            ApplyKnockback(target, knockDir.normalized, juggernautRoarKnockback);
            ApplySurvivorSlow(target, juggernautRoarSlowMultiplier, juggernautRoarSlowDuration);
            SpawnHitMarkerVfx(target.transform.position + Vector3.up * 1f, 0.35f);
        }

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnShockwaveRing(center, radius, new Color(1f, 0.55f, 0.15f, 0.55f), 0.5f);
        }

        SpawnJuggernautRoarGust(center, radius);

        LogPredatorAbility("Dragon Roar hit " + hits.Length);
        return true;
    }

    public bool CastJuggernautMeteor()
    {
        if (!CanCastPrototypeAbility() || IsJuggernautMeteorActive())
        {
            return false;
        }

        if (!TryBeginJuggernautMeteor())
        {
            return false;
        }

        juggernautMeteorRoutine = StartCoroutine(JuggernautMeteorRoutine());
        return juggernautMeteorRoutine != null;
    }

    private bool TryBeginDragonLeap(out Vector3 destination)
    {
        destination = transform.position;
        if (isJuggernautLeaping || characterController == null)
        {
            return false;
        }

        Vector3 forward = GetFlatForward();
        float distance = Mathf.Max(1f, juggernautLeapDistance);
        destination = transform.position + forward * distance;
        destination.y = transform.position.y;

        if (ArenaBounds.Instance != null)
        {
            destination = ArenaBounds.Instance.ClampPosition(destination);
        }

        Vector3 delta = destination - transform.position;
        float travel = ResolvePredatorLeapTravelDistance(transform.position, delta.normalized, delta.magnitude);
        if (travel <= 0.15f)
        {
            LogPredatorAbility("Dragon Leap rejected: blocked path.");
            return false;
        }

        destination = transform.position + delta.normalized * travel;
        destination.y = transform.position.y;

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnWarningCircle(destination, juggernautLeapImpactRadius, juggernautLeapDuration, new Color(1f, 0.5f, 0.1f, 0.5f));
        }

        isJuggernautLeaping = true;
        return true;
    }

    private IEnumerator DragonLeapRoutine(Vector3 destination)
    {
        Vector3 start = transform.position;
        float duration = Mathf.Max(0.05f, juggernautLeapDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 pos = Vector3.Lerp(start, destination, t);
            pos.y = start.y;
            MovePredatorTo(pos);
            yield return null;
        }

        ApplyDragonLeapImpact(destination);
        isJuggernautLeaping = false;
        juggernautLeapRoutine = null;
    }

    private void ApplyDragonLeapImpact(Vector3 center)
    {
        float radius = Mathf.Max(0.5f, juggernautLeapImpactRadius);
        int damage = Mathf.Max(1, juggernautLeapDamage);
        UnitHealth[] hits = GetSurvivorsInRange(center, radius);

        for (int i = 0; i < hits.Length; i++)
        {
            hits[i].TakeDamage(damage, gameObject);
            Vector3 knockDir = hits[i].transform.position - center;
            knockDir.y = 0f;
            ApplyKnockback(hits[i], knockDir.normalized, juggernautLeapKnockback);
            SpawnHitMarkerVfx(hits[i].transform.position + Vector3.up * 1f, 0.35f);
        }

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnShockwaveRing(center, radius, new Color(1f, 0.4f, 0.08f, 0.6f), 0.45f);
        }

        SpawnJuggernautLeapTerrain(center, radius);

        LogPredatorAbility("Dragon Leap impact hit " + hits.Length);
    }

    private bool TryBeginJuggernautMeteor()
    {
        if (IsJuggernautMeteorActive())
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

        isJuggernautMeteorActive = true;
        return true;
    }

    private IEnumerator JuggernautMeteorRoutine()
    {
        Vector3 impactPoint = GetAbilityGroundTarget(juggernautMeteorPlacementDistance);
        float warning = Mathf.Max(0.1f, juggernautMeteorWarningDuration);
        float radius = Mathf.Max(0.5f, juggernautMeteorRadius);

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnWarningCircle(impactPoint, radius, warning, new Color(1f, 0.35f, 0.08f, 0.55f));
        }

        yield return new WaitForSeconds(warning);

        if (!isActiveAndEnabled || unitHealth == null || unitHealth.IsDead)
        {
            isJuggernautMeteorActive = false;
            juggernautMeteorRoutine = null;
            yield break;
        }

        int impactDamage = Mathf.Max(1, juggernautMeteorImpactDamage);
        UnitHealth[] hits = GetSurvivorsInRange(impactPoint, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            hits[i].TakeDamage(impactDamage, gameObject);
            SpawnHitMarkerVfx(hits[i].transform.position + Vector3.up * 1f, 0.4f);
        }

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnBombExplosion(impactPoint, radius * 0.35f, 0.5f);
        }

        SpawnJuggernautMeteorTerrain(impactPoint, radius);

        PredatorSurvivorZone.Spawn(
            impactPoint,
            radius * 0.85f,
            juggernautMeteorFireDuration,
            juggernautMeteorFireDps,
            0.75f,
            0.75f,
            survivorTag,
            targetLayers,
            gameObject,
            new Color(1f, 0.35f, 0.08f, 0.45f));

        isJuggernautMeteorActive = false;
        juggernautMeteorRoutine = null;
        LogPredatorAbility("Dragon Meteor impact hit " + hits.Length);
    }

    private float ResolvePredatorLeapTravelDistance(Vector3 start, Vector3 direction, float maxDistance)
    {
        float radius = characterController != null ? characterController.radius : 0.5f;
        float probeHeight = characterController != null ? Mathf.Max(0.5f, characterController.height * 0.5f) : 1f;
        Vector3 origin = start + Vector3.up * probeHeight;

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            radius,
            direction,
            maxDistance,
            ~0,
            QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0)
        {
            return maxDistance;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        float bestDistance = maxDistance;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            UnitHealth otherUnit = hitCollider.GetComponentInParent<UnitHealth>();
            if (otherUnit != null && otherUnit != unitHealth && !otherUnit.IsDead)
            {
                continue;
            }

            bestDistance = Mathf.Min(bestDistance, Mathf.Max(0f, hits[i].distance - radius * 0.35f));
            break;
        }

        return bestDistance;
    }

    private void MovePredatorTo(Vector3 position)
    {
        if (characterController != null && characterController.enabled)
        {
            Vector3 delta = position - transform.position;
            characterController.Move(delta);
            return;
        }

        transform.position = position;
        if (ArenaBounds.Instance != null)
        {
            ArenaBounds.Instance.ClampUnitTransform(transform, "DragonLeap");
        }
    }
}
