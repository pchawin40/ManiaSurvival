using System.Collections;
using UnityEngine;

public partial class PredatorClassManager
{
    [Header("Shadow Stalker - Identity")]
    [TextArea(2, 4)]
    public string shadowStalkerClassSummary = "Shadow Stalker — Stealth predator. Slash, vanish, mark prey, then bring night.";

    public AbilityDetail shadowSlot1Detail;
    public AbilityDetail shadowSlot2Detail;
    public AbilityDetail shadowSlot3Detail;
    public AbilityDetail shadowSlot4Detail;

    [Header("Shadow Stalker - Slash")]
    public float shadowSlashRange = 3.2f;
    public float shadowSlashHalfAngle = 55f;
    public int shadowSlashDamage = 14;
    public float shadowSlashKnockback = 3.5f;

    [Header("Shadow Stalker - Vanish")]
    public float shadowVanishDuration = 2.2f;
    public float shadowVanishSpeedMultiplier = 1.45f;

    [Header("Shadow Stalker - Mark")]
    public float shadowMarkRange = 18f;
    public float shadowMarkHalfAngle = 22f;
    public float shadowMarkSlowMultiplier = 0.5f;
    public float shadowMarkSlowDuration = 4f;
    public int shadowMarkDamage = 4;

    [Header("Shadow Stalker - Night")]
    public float shadowNightRadius = 7f;
    public float shadowNightWarningDuration = 0.85f;
    public float shadowNightDuration = 4f;
    public float shadowNightPulseInterval = 1f;
    public int shadowNightPulseDamage = 8;
    public float shadowNightSlowMultiplier = 0.55f;
    public float shadowNightPlacementDistance = 6f;

    private Coroutine shadowVanishRoutine;
    private bool isShadowVanished;
    private Coroutine shadowNightRoutine;
    private bool isShadowNightActive;
    private GameObject activeShadowMarkVfx;

    public bool CastShadowSlash()
    {
        if (!CanCastPrototypeAbility())
        {
            return false;
        }

        Vector3 origin = GetChestCastOrigin();
        Vector3 forward = GetFlatForward(logAim: true);
        UnitHealth[] hits = GetSurvivorsInCone(origin, forward, shadowSlashRange, shadowSlashHalfAngle);

        for (int i = 0; i < hits.Length; i++)
        {
            hits[i].TakeDamage(shadowSlashDamage, gameObject);
            Vector3 knockDir = hits[i].transform.position - transform.position;
            ApplyKnockback(hits[i], knockDir, shadowSlashKnockback);
            SpawnHitMarkerVfx(hits[i].transform.position + Vector3.up * 1f, 0.35f);
        }

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnFireCone(origin, forward, shadowSlashRange, shadowSlashHalfAngle, new Color(0.45f, 0.2f, 0.75f, 0.45f), 0.35f);
        }

        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner != null)
        {
            spawner.SpawnConeScorchMarks(origin, forward, shadowSlashRange, shadowSlashHalfAngle, 2, 0.75f, 2f);
        }

        LogPredatorAbility("Shadow Slash hit " + hits.Length);
        return true;
    }

    public bool CastShadowVanish()
    {
        if (!CanCastPrototypeAbility() || isShadowVanished)
        {
            return false;
        }

        shadowVanishRoutine = StartCoroutine(ShadowVanishRoutine());
        return shadowVanishRoutine != null;
    }

    public bool CastShadowMark()
    {
        if (!CanCastPrototypeAbility())
        {
            return false;
        }

        Vector3 origin = GetChestCastOrigin();
        Vector3 forward = GetFlatForward(logAim: true);
        UnitHealth[] candidates = GetSurvivorsInCone(origin, forward, shadowMarkRange, shadowMarkHalfAngle);
        UnitHealth target = candidates.Length > 0 ? candidates[0] : FindClosestSurvivorInForwardArc(origin, forward, shadowMarkRange, shadowMarkHalfAngle);

        if (target == null)
        {
            LogPredatorAbility("Shadow Mark missed.");
            return true;
        }

        if (shadowMarkDamage > 0)
        {
            target.TakeDamage(shadowMarkDamage, gameObject);
        }

        ApplySurvivorSlow(target, shadowMarkSlowMultiplier, shadowMarkSlowDuration);
        SpawnShadowMarkVfx(target);
        SpawnHitMarkerVfx(target.transform.position + Vector3.up * 1.2f, 0.5f);
        LogPredatorAbility("Shadow Mark applied to " + target.name);
        return true;
    }

    public bool CastShadowNight()
    {
        if (!CanCastPrototypeAbility() || IsShadowNightActive())
        {
            return false;
        }

        shadowNightRoutine = StartCoroutine(ShadowNightRoutine());
        return shadowNightRoutine != null;
    }

    internal bool IsShadowNightActive()
    {
        return isShadowNightActive || shadowNightRoutine != null;
    }

    private void StopShadowClassState(string reason)
    {
        if (shadowVanishRoutine != null)
        {
            StopCoroutine(shadowVanishRoutine);
            shadowVanishRoutine = null;
        }

        if (isShadowVanished)
        {
            RestoreShadowVisibility();
        }

        if (shadowNightRoutine != null)
        {
            StopCoroutine(shadowNightRoutine);
            shadowNightRoutine = null;
        }

        isShadowNightActive = false;
        ClearShadowMarkVfx();
    }

    private IEnumerator ShadowVanishRoutine()
    {
        isShadowVanished = true;
        SetPredatorRenderersVisible(false);

        if (monsterMovement != null)
        {
            monsterMovement.SetAbilitySpeedMultiplier(shadowVanishSpeedMultiplier);
        }

        SpawnSelfBurstVfx(new Color(0.35f, 0.15f, 0.65f, 0.65f), 0.4f);
        yield return new WaitForSeconds(Mathf.Max(0.1f, shadowVanishDuration));

        if (monsterMovement != null)
        {
            monsterMovement.ClearAbilitySpeedMultiplier();
        }

        RestoreShadowVisibility();
        isShadowVanished = false;
        shadowVanishRoutine = null;
        LogPredatorAbility("Shadow Vanish ended");
    }

    private void SetPredatorRenderersVisible(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].name.Contains("VFX") || renderers[i].name.Contains("Mark"))
            {
                continue;
            }

            renderers[i].enabled = visible;
        }
    }

    private void RestoreShadowVisibility()
    {
        SetPredatorRenderersVisible(true);
        ApplyPredatorClassThemeColor(activeClass);
    }

    private IEnumerator ShadowNightRoutine()
    {
        isShadowNightActive = true;
        Vector3 center = GetAbilityGroundTarget(shadowNightPlacementDistance);
        float warning = Mathf.Max(0.1f, shadowNightWarningDuration);
        float radius = Mathf.Max(0.5f, shadowNightRadius);

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnWarningCircle(center, radius, warning, new Color(0.35f, 0.12f, 0.55f, 0.55f));
        }

        yield return new WaitForSeconds(warning);

        float endTime = Time.time + Mathf.Max(0.5f, shadowNightDuration);
        while (Time.time < endTime)
        {
            if (!isActiveAndEnabled || unitHealth == null || unitHealth.IsDead || !IsRoundActive())
            {
                break;
            }

            ApplyShadowNightPulse(center, radius);
            yield return new WaitForSeconds(Mathf.Max(0.25f, shadowNightPulseInterval));
        }

        isShadowNightActive = false;
        shadowNightRoutine = null;
        LogPredatorAbility("Shadow Night ended");
    }

    private void ApplyShadowNightPulse(Vector3 center, float radius)
    {
        UnitHealth[] hits = GetSurvivorsInRange(center, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            hits[i].TakeDamage(shadowNightPulseDamage, gameObject);
            ApplySurvivorSlow(hits[i], shadowNightSlowMultiplier, shadowNightPulseInterval + 0.5f);
            SpawnHitMarkerVfx(hits[i].transform.position + Vector3.up * 1f, 0.3f);
        }

        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner != null)
        {
            spawner.SpawnSlowZone(center, radius * 0.9f, shadowNightPulseInterval + 0.25f, 0.82f);
        }

        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnShockwaveRing(center, radius, new Color(0.42f, 0.18f, 0.72f, 0.5f), 0.45f);
        }
    }

    private void SpawnShadowMarkVfx(UnitHealth target)
    {
        ClearShadowMarkVfx();
        if (target == null)
        {
            return;
        }

        GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mark.name = "ShadowMarkVFX";
        Collider col = mark.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        mark.transform.SetParent(target.transform, false);
        mark.transform.localPosition = Vector3.up * 2f;
        mark.transform.localScale = Vector3.one * 0.55f;
        ApplySimpleVfxColor(mark, new Color(0.55f, 0.25f, 0.95f, 0.85f));
        activeShadowMarkVfx = mark;
        Destroy(mark, shadowMarkSlowDuration);
    }

    private void ClearShadowMarkVfx()
    {
        if (activeShadowMarkVfx != null)
        {
            Destroy(activeShadowMarkVfx);
            activeShadowMarkVfx = null;
        }
    }

    private void EnsureShadowAbilityDetails()
    {
        if (!shadowSlot1Detail.IsConfigured)
        {
            shadowSlot1Detail = AbilityPresentationFallback.GetShadowStalkerDetail(1);
        }

        if (!shadowSlot2Detail.IsConfigured)
        {
            shadowSlot2Detail = AbilityPresentationFallback.GetShadowStalkerDetail(2);
        }

        if (!shadowSlot3Detail.IsConfigured)
        {
            shadowSlot3Detail = AbilityPresentationFallback.GetShadowStalkerDetail(3);
        }

        if (!shadowSlot4Detail.IsConfigured)
        {
            shadowSlot4Detail = AbilityPresentationFallback.GetShadowStalkerDetail(4);
        }
    }

    private AbilityDetail GetShadowSlotDetail(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1: return shadowSlot1Detail;
            case 2: return shadowSlot2Detail;
            case 3: return shadowSlot3Detail;
            default: return shadowSlot4Detail;
        }
    }
}
