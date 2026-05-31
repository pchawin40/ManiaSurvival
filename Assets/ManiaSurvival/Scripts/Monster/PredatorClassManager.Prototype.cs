using System.Collections.Generic;
using UnityEngine;

public partial class PredatorClassManager
{
    [Header("Swarm Overlord - Identity")]
    [TextArea(2, 4)]
    public string swarmOverlordClassSummary = "Swarm Overlord — Zone predator. Spit, brood, infest, then call the hive.";

    [Header("Dragon Juggernaut - Identity")]
    [TextArea(2, 4)]
    public string dragonJuggernautClassSummary = "Dragon Juggernaut — Burst predator. Flame breath, leap, roar, then meteor.";

    public AbilityDetail swarmSlot1Detail;
    public AbilityDetail swarmSlot2Detail;
    public AbilityDetail swarmSlot3Detail;
    public AbilityDetail swarmSlot4Detail;
    public AbilityDetail juggernautSlot1Detail;
    public AbilityDetail juggernautSlot2Detail;
    public AbilityDetail juggernautSlot3Detail;
    public AbilityDetail juggernautSlot4Detail;

    [Header("Swarm Overlord - Spit")]
    public float swarmSpitRange = 12f;
    public float swarmSpitHalfAngle = 38f;
    public int swarmSpitDamage = 10;
    public bool swarmSpitAppliesSlow = true;
    public float swarmSpitSlowMultiplier = 0.75f;
    public float swarmSpitSlowDuration = 1f;

    [Header("Swarm Overlord - Brood")]
    public GameObject broodlingPrefab;
    public int swarmBroodSpawnCount = 2;
    public int maxActiveBroodlings = 6;
    public float broodlingLifetime = 12f;
    public int broodlingDamage = 5;
    public float broodlingMoveSpeed = 5.5f;
    public float broodSpawnOffsetRadius = 2.2f;

    [Header("Swarm Overlord - Infest")]
    public float swarmInfestRadius = 4.5f;
    public float swarmInfestDuration = 5f;
    public float swarmInfestDps = 4f;
    public float swarmInfestSlowMultiplier = 0.65f;
    public float swarmInfestPlacementDistance = 5f;

    [Header("Swarm Overlord - Hive")]
    public float swarmHiveWarningDuration = 0.8f;
    public float swarmHiveRadius = 6f;
    public int swarmHivePulseDamage = 7;
    public float swarmHivePulseInterval = 1f;
    public int swarmHivePulseCount = 3;
    public int swarmHiveBroodSpawnCount = 3;

    [Header("Dragon Juggernaut - Flame")]
    public float juggernautFlameRange = 8f;
    public float juggernautFlameHalfAngle = 40f;
    public int juggernautFlameDamage = 12;
    public bool juggernautFlameAppliesBurn = true;
    public float juggernautBurnDuration = 2f;
    public int juggernautBurnTickDamage = 2;

    [Header("Dragon Juggernaut - Leap")]
    public float juggernautLeapDistance = 8f;
    public float juggernautLeapDuration = 0.35f;
    public float juggernautLeapImpactRadius = 3.5f;
    public int juggernautLeapDamage = 15;
    public float juggernautLeapKnockback = 4f;

    [Header("Dragon Juggernaut - Roar")]
    public float juggernautRoarRadius = 5f;
    public float juggernautRoarKnockback = 5f;
    public float juggernautRoarSlowMultiplier = 0.6f;
    public float juggernautRoarSlowDuration = 1.5f;
    public int juggernautRoarDamage = 6;

    [Header("Dragon Juggernaut - Meteor")]
    public float juggernautMeteorWarningDuration = 1f;
    public float juggernautMeteorRadius = 6f;
    public int juggernautMeteorImpactDamage = 22;
    public float juggernautMeteorFireDuration = 5f;
    public float juggernautMeteorFireDps = 4f;
    public float juggernautMeteorPlacementDistance = 7f;

    private readonly List<BroodlingMinion> activeBroodlings = new List<BroodlingMinion>();
    private Coroutine swarmHiveRoutine;
    private bool isSwarmHiveActive;
    private Coroutine juggernautMeteorRoutine;
    private bool isJuggernautMeteorActive;
    private Coroutine juggernautLeapRoutine;
    private bool isJuggernautLeaping;

    public static bool IsPlayablePrototypeClass(PredatorClass predatorClass)
    {
        return PredatorClassCatalog.IsPlayableClass(predatorClass);
    }

    public PredatorClass GetCurrentPredatorClass()
    {
        return activeClass;
    }

    public void SetPredatorClass(PredatorClass newClass)
    {
        if (activeClass == newClass)
        {
            return;
        }

        ResetPrototypeClassState();
        activeClass = newClass;
        Debug.Log("[PredatorClass] Switched to " + GetClassDisplayName());

        if (monsterMovement == null)
        {
            monsterMovement = GetComponent<MonsterPlayerMovement>();
        }

        if (monsterMovement != null)
        {
            monsterMovement.ApplyPredatorMovementProfile(activeClass);
        }

        EnsurePrototypeAbilityDetails();
        ApplyPredatorClassThemeColor(newClass);
        RefreshPredatorAbilityUi();
    }

    public AbilityDetail GetPredatorAbilityDetail(int slotNumber)
    {
        return GetAbilityDetail(slotNumber);
    }

    public AbilityDetail GetAbilityDetail(int slotNumber)
    {
        int clamped = Mathf.Clamp(slotNumber, 1, 4);

        switch (activeClass)
        {
            case PredatorClass.SwarmOverlord:
                EnsureSwarmAbilityDetails();
                return GetSwarmSlotDetail(clamped);
            case PredatorClass.Juggernaut:
                EnsureJuggernautAbilityDetails();
                return GetJuggernautSlotDetail(clamped);
            case PredatorClass.RelentlessHook:
                EnsureRelentlessAbilityDetails();
                return GetRelentlessSlotDetail(clamped);
            case PredatorClass.ShadowStalker:
                EnsureShadowAbilityDetails();
                return GetShadowSlotDetail(clamped);
            case PredatorClass.IronColossus:
                EnsureIronAbilityDetails();
                return GetIronSlotDetail(clamped);
            case PredatorClass.PlagueGardener:
                EnsurePlagueAbilityDetails();
                return GetPlagueSlotDetail(clamped);
            default:
                return AbilityDetail.CreateFallback(clamped, GetClassDisplayName());
        }
    }

    public string GetPredatorShortButtonLabel(int slotNumber)
    {
        AbilityDetail detail = GetAbilityDetail(slotNumber);
        if (detail.IsConfigured && !string.IsNullOrWhiteSpace(detail.buttonLabel))
        {
            return detail.buttonLabel;
        }

        return AbilityPresentationFallback.GetPredatorShortLabel(activeClass, slotNumber);
    }

    public string GetPredatorClassSummary()
    {
        return GetClassSummary();
    }

    public string GetClassDisplayName()
    {
        PredatorClassDetail catalogDetail = PredatorClassCatalog.GetDetail(activeClass);
        if (catalogDetail != null && !string.IsNullOrWhiteSpace(catalogDetail.displayName))
        {
            return catalogDetail.displayName;
        }

        switch (activeClass)
        {
            case PredatorClass.RelentlessHook:
                return relentlessHookDisplayName;
            case PredatorClass.SwarmOverlord:
                return "Swarm Overlord";
            case PredatorClass.Juggernaut:
                return "Dragon Juggernaut";
            default:
                return activeClass.ToString();
        }
    }

    public string GetClassSummary()
    {
        switch (activeClass)
        {
            case PredatorClass.RelentlessHook:
                return relentlessHookClassSummary;
            case PredatorClass.SwarmOverlord:
                return swarmOverlordClassSummary;
            case PredatorClass.Juggernaut:
                return dragonJuggernautClassSummary;
            case PredatorClass.ShadowStalker:
                return shadowStalkerClassSummary;
            case PredatorClass.IronColossus:
                return ironColossusClassSummary;
            case PredatorClass.PlagueGardener:
                return plagueGardenerClassSummary;
            default:
                return GetClassDisplayName();
        }
    }

    public PredatorClassDetail GetPredatorClassDetail()
    {
        return PredatorClassCatalog.GetDetail(activeClass);
    }

    public Color GetClassThemeColor()
    {
        return PredatorClassCatalog.GetDetail(activeClass).themeColor;
    }

    public void RegisterBroodling(BroodlingMinion broodling)
    {
        if (broodling == null || activeBroodlings.Contains(broodling))
        {
            return;
        }

        activeBroodlings.Add(broodling);
        PruneBroodlingList();
    }

    public void UnregisterBroodling(BroodlingMinion broodling)
    {
        if (broodling == null)
        {
            return;
        }

        activeBroodlings.Remove(broodling);
    }

    private void ResetPrototypeClassState()
    {
        StopRelentlessBarrage("class-switch");
        StopTonicEffects("class-switch");

        if (swarmHiveRoutine != null)
        {
            StopCoroutine(swarmHiveRoutine);
            swarmHiveRoutine = null;
        }

        isSwarmHiveActive = false;

        if (juggernautMeteorRoutine != null)
        {
            StopCoroutine(juggernautMeteorRoutine);
            juggernautMeteorRoutine = null;
        }

        isJuggernautMeteorActive = false;

        if (juggernautLeapRoutine != null)
        {
            StopCoroutine(juggernautLeapRoutine);
            juggernautLeapRoutine = null;
        }

        isJuggernautLeaping = false;

        for (int i = activeBroodlings.Count - 1; i >= 0; i--)
        {
            if (activeBroodlings[i] != null)
            {
                Destroy(activeBroodlings[i].gameObject);
            }
        }

        activeBroodlings.Clear();

        StopShadowClassState("class-switch");
        StopIronClassState("class-switch");
        StopPlagueClassState("class-switch");
    }

    private void RefreshPredatorAbilityUi()
    {
        ManiaGameUI gameUi = FindFirstObjectByType<ManiaGameUI>();
        if (gameUi != null)
        {
            gameUi.RefreshAbilityLabels(force: true);
            gameUi.RefreshAbilityInfo(force: true);
        }
    }

    private void EnsurePrototypeAbilityDetails()
    {
        EnsureRelentlessAbilityDetails();
        EnsureSwarmAbilityDetails();
        EnsureJuggernautAbilityDetails();
        EnsureShadowAbilityDetails();
        EnsureIronAbilityDetails();
        EnsurePlagueAbilityDetails();
    }

    private void EnsureSwarmAbilityDetails()
    {
        if (!swarmSlot1Detail.IsConfigured)
        {
            swarmSlot1Detail = AbilityPresentationFallback.GetSwarmOverlordDetail(1);
        }

        if (!swarmSlot2Detail.IsConfigured)
        {
            swarmSlot2Detail = AbilityPresentationFallback.GetSwarmOverlordDetail(2);
        }

        if (!swarmSlot3Detail.IsConfigured)
        {
            swarmSlot3Detail = AbilityPresentationFallback.GetSwarmOverlordDetail(3);
        }

        if (!swarmSlot4Detail.IsConfigured)
        {
            swarmSlot4Detail = AbilityPresentationFallback.GetSwarmOverlordDetail(4);
        }
    }

    private void EnsureJuggernautAbilityDetails()
    {
        if (!juggernautSlot1Detail.IsConfigured)
        {
            juggernautSlot1Detail = AbilityPresentationFallback.GetJuggernautDetail(1);
        }

        if (!juggernautSlot2Detail.IsConfigured)
        {
            juggernautSlot2Detail = AbilityPresentationFallback.GetJuggernautDetail(2);
        }

        if (!juggernautSlot3Detail.IsConfigured)
        {
            juggernautSlot3Detail = AbilityPresentationFallback.GetJuggernautDetail(3);
        }

        if (!juggernautSlot4Detail.IsConfigured)
        {
            juggernautSlot4Detail = AbilityPresentationFallback.GetJuggernautDetail(4);
        }
    }

    private AbilityDetail GetSwarmSlotDetail(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1: return swarmSlot1Detail;
            case 2: return swarmSlot2Detail;
            case 3: return swarmSlot3Detail;
            default: return swarmSlot4Detail;
        }
    }

    private AbilityDetail GetJuggernautSlotDetail(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1: return juggernautSlot1Detail;
            case 2: return juggernautSlot2Detail;
            case 3: return juggernautSlot3Detail;
            default: return juggernautSlot4Detail;
        }
    }

    private AbilityDetail GetRelentlessSlotDetail(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1: return relentlessSlot1Detail;
            case 2: return relentlessSlot2Detail;
            case 3: return relentlessSlot3Detail;
            default: return relentlessSlot4Detail;
        }
    }

    internal bool IsSwarmHiveActive()
    {
        return isSwarmHiveActive || swarmHiveRoutine != null;
    }

    internal bool IsJuggernautMeteorActive()
    {
        return isJuggernautMeteorActive || juggernautMeteorRoutine != null;
    }

    internal void ApplySurvivorSlow(UnitHealth survivor, float slowMultiplier, float duration)
    {
        if (survivor == null || survivor.IsDead || slowMultiplier >= 0.99f || duration <= 0f)
        {
            return;
        }

        SurvivorMovement movement = survivor.GetComponent<SurvivorMovement>();
        if (movement != null)
        {
            movement.ApplyTemporarySpeedMultiplier(slowMultiplier, duration);
        }

        OfflineSurvivorBotAI bot = survivor.GetComponent<OfflineSurvivorBotAI>();
        if (bot != null)
        {
            bot.ApplyTemporarySpeedMultiplier(slowMultiplier, duration);
        }
    }

    internal int GetActiveBroodlingCount()
    {
        PruneBroodlingList();
        return activeBroodlings.Count;
    }

    private void PruneBroodlingList()
    {
        for (int i = activeBroodlings.Count - 1; i >= 0; i--)
        {
            if (activeBroodlings[i] == null)
            {
                activeBroodlings.RemoveAt(i);
            }
        }
    }

    internal Vector3 GetAbilityGroundTarget(float forwardDistance)
    {
        Vector3 forward = GetFlatForward();
        Vector3 target = transform.position + forward * Mathf.Max(1f, forwardDistance);
        target.y = transform.position.y;

        if (ArenaBounds.Instance != null)
        {
            target = ArenaBounds.Instance.ClampPosition(target);
        }

        return target;
    }

    internal DynamicTerrainSpawner GetTerrainSpawner()
    {
        return DynamicTerrainSpawner.GetOrCreate();
    }

    internal void SpawnRelentlessSprayTerrain(Vector3 origin, Vector3 forward, float range, float halfAngle)
    {
        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner == null)
        {
            return;
        }

        spawner.SpawnConeScorchMarks(origin, forward, range, halfAngle, 3, 0.85f, 1.5f);
        spawner.SpawnSlowZone(origin + forward * (range * 0.35f), Mathf.Max(1.2f, range * 0.22f), 1.5f, 0.88f);
    }

    internal void SpawnHookAnchorMark(Vector3 point)
    {
        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner == null)
        {
            return;
        }

        spawner.SpawnRockObstacle(point, 2f);
    }

    internal void SpawnHookDragTrail(Vector3 start, Vector3 end)
    {
        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner == null)
        {
            return;
        }

        for (int i = 1; i <= 3; i++)
        {
            float t = i / 4f;
            Vector3 point = Vector3.Lerp(start, end, t);
            spawner.SpawnCrater(point, 0.7f, 1.2f);
        }
    }

    internal void SpawnTonicTerrainZone()
    {
        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner == null)
        {
            return;
        }

        spawner.SpawnPoisonZone(
            transform.position,
            tonicGasRadius,
            tonicDuration,
            tonicGasDamagePerSecond,
            tonicGasSlowMultiplier);
    }

    internal void SpawnBarragePulseTerrain(Vector3 origin, Vector3 forward, float range)
    {
        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner == null)
        {
            return;
        }

        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.001f)
        {
            forward = GetFlatForward();
        }

        forward.Normalize();
        spawner.SpawnCrater(origin + forward * (range * 0.35f), 1.1f, barrageCraterDuration);
        spawner.SpawnFireZone(origin + forward * (range * 0.55f), 1.4f, 2f, 2f);
    }

    internal void SpawnBarrageFinalObstacles(Vector3 origin, Vector3 forward, float range)
    {
        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner == null)
        {
            return;
        }

        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.001f)
        {
            forward = GetFlatForward();
        }

        forward.Normalize();
        Vector3 center = origin + forward * (range * 0.45f);
        spawner.SpawnCrater(center, 1.6f, barrageCraterDuration);

        for (int i = 0; i < 3; i++)
        {
            float yaw = -20f + i * 20f;
            Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * forward * 1.8f;
            spawner.SpawnRockObstacle(center + offset, 4f);
        }
    }

    internal void SpawnSwarmBroodNest(Vector3 spawnPos)
    {
        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner == null)
        {
            return;
        }

        spawner.SpawnPlatform(spawnPos, new Vector3(1.2f, 0.2f, 1.2f), 8f);
    }

    internal void SpawnSwarmHiveChokepoint(Vector3 center, float radius)
    {
        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner == null)
        {
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f + 45f;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 pos = center + dir * (radius * 0.75f);
            if (i % 2 == 0)
            {
                spawner.SpawnVineWall(pos, Quaternion.LookRotation(dir), 2.2f, 6f);
            }
            else
            {
                spawner.SpawnRockObstacle(pos, 5f);
            }
        }
    }

    internal void SpawnJuggernautFlameStrip(Vector3 origin, Vector3 forward, float range, float halfAngle)
    {
        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner == null)
        {
            return;
        }

        spawner.SpawnConeScorchMarks(origin, forward, range, halfAngle, 4, 1f, 3f);
        spawner.SpawnFireZone(origin + forward * (range * 0.5f), range * 0.35f, 3f, juggernautBurnTickDamage);
    }

    internal void SpawnJuggernautLeapTerrain(Vector3 center, float radius)
    {
        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner == null)
        {
            return;
        }

        spawner.SpawnCrater(center, radius * 0.55f, 8f);
        spawner.SpawnConeScorchMarks(center, transform.forward, radius, 35f, 3, 0.9f, 6f);
    }

    internal void SpawnJuggernautRoarGust(Vector3 center, float radius)
    {
        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner == null)
        {
            return;
        }

        spawner.SpawnSlowZone(center, radius * 0.85f, 2f, 0.8f);
    }

    internal void SpawnJuggernautMeteorTerrain(Vector3 center, float radius)
    {
        DynamicTerrainSpawner spawner = GetTerrainSpawner();
        if (spawner == null)
        {
            return;
        }

        spawner.SpawnCrater(center, radius * 0.65f, 10f);
        spawner.SpawnFireZone(center, radius * 0.75f, juggernautMeteorFireDuration, juggernautMeteorFireDps);

        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f + Random.Range(-15f, 15f);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            spawner.SpawnRockObstacle(center + dir * (radius * 0.55f), 6f);
        }
    }

    [Header("Class Theme")]
    [Tooltip("Optional body renderer to tint when class is selected. Falls back to all child renderers.")]
    public Renderer predatorThemeRenderer;

    private readonly System.Collections.Generic.List<Renderer> themedRenderers = new System.Collections.Generic.List<Renderer>();
    private readonly System.Collections.Generic.List<Color> themedRendererBaseColors = new System.Collections.Generic.List<Color>();

    internal void ApplyPredatorClassThemeColor(PredatorClass predatorClass)
    {
        CacheThemedRenderersIfNeeded();
        Color theme = PredatorClassCatalog.GetDetail(predatorClass).themeColor;
        Color tint = Color.Lerp(Color.white, theme, 0.55f);

        for (int i = 0; i < themedRenderers.Count; i++)
        {
            Renderer renderer = themedRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            Color baseColor = i < themedRendererBaseColors.Count ? themedRendererBaseColors[i] : Color.white;
            if (renderer.material != null && renderer.material.HasProperty("_Color"))
            {
                renderer.material.color = Color.Lerp(baseColor, tint, 0.65f);
            }
            else if (renderer.material != null && renderer.material.HasProperty("_BaseColor"))
            {
                renderer.material.SetColor("_BaseColor", Color.Lerp(baseColor, tint, 0.65f));
            }
        }
    }

    private void CacheThemedRenderersIfNeeded()
    {
        if (themedRenderers.Count > 0)
        {
            return;
        }

        if (predatorThemeRenderer != null)
        {
            themedRenderers.Add(predatorThemeRenderer);
        }
        else
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                string name = renderers[i].name;
                if (name.Contains("VFX") || name.Contains("Mark") || name.Contains("UI"))
                {
                    continue;
                }

                themedRenderers.Add(renderers[i]);
            }
        }

        themedRendererBaseColors.Clear();
        for (int i = 0; i < themedRenderers.Count; i++)
        {
            Renderer renderer = themedRenderers[i];
            Color baseColor = Color.white;
            if (renderer != null && renderer.sharedMaterial != null)
            {
                if (renderer.sharedMaterial.HasProperty("_Color"))
                {
                    baseColor = renderer.sharedMaterial.color;
                }
                else if (renderer.sharedMaterial.HasProperty("_BaseColor"))
                {
                    baseColor = renderer.sharedMaterial.GetColor("_BaseColor");
                }
            }

            themedRendererBaseColors.Add(baseColor);
        }
    }
}
