using UnityEngine;

public enum AbilitySlot
{
    Primary = 1,
    Ability2 = 2,
    Ability3 = 3,
    Ultimate = 4
}

[DisallowMultipleComponent]
public class AbilityController : MonoBehaviour
{
    [Header("Identity")]
    public bool controlsPredator;
    public bool enforceTestLoadouts = true;
    public bool logAbilityFlow = true;
    public bool logAbilityMana = true;
    public bool disableLegacyAbilityComponents = true;
    public bool pipelineTestMode = true;
    public bool abilityControllerOwnsCooldowns = true;

    [Header("References")]
    public LocalRoleController localRoleController;
    public ManiaGameManager gameManager;
    public UnitHealth unitHealth;
    public UnitMana unitMana;
    public SurvivorClassManager survivorClassManager;
    public PredatorClassManager predatorClassManager;

    [Header("Survivor Mana Costs")]
    public float survivorSlot1ManaCost = 2f;
    public float survivorSlot2ManaCost = 5f;
    public float survivorSlot3ManaCost = 4f;
    public float survivorSlot4ManaCost = 12f;

    [Header("Predator Mana Costs")]
    public float predatorSlot1ManaCost = 8f;
    public float predatorSlot2ManaCost = 25f;
    public float predatorSlot3ManaCost = 35f;
    public float predatorSlot4ManaCost = 80f;

    [Header("Survivor Slot Cooldowns")]
    public float survivorSlot1Cooldown = 2.5f;
    public float survivorSlot2Cooldown = 8f;
    public float survivorSlot3Cooldown = 10f;
    public float survivorSlot4Cooldown = 28f;

    [Header("Predator Slot Cooldowns")]
    public float predatorSlot1Cooldown = 3.75f;
    public float predatorSlot2Cooldown = 10f;
    public float predatorSlot3Cooldown = 14f;
    public float predatorSlot4Cooldown = 35f;

    [Header("Debug VFX")]
    public bool spawnPlaceholderVfx = true;
    public float placeholderVfxLifetime = 1.25f;

    [Header("Audio")]
    public AudioSource abilityAudioSource;
    public AudioClip slot1Sound;
    public AudioClip slot2Sound;
    public AudioClip slot3Sound;
    public AudioClip slot4Sound;
    public AudioClip failedCastSound;
    public AudioClip noManaSound;
    public AudioClip cooldownFailSound;
    public bool usePlaceholderAbilityAudio = true;
    [Range(0f, 1f)] public float abilitySfxVolume = 0.85f;
    public bool logAbilityAudio;

    [Header("Ground Effects")]
    public bool spawnGroundEffects = true;
    public GameObject groundEffectPrefab;
    public float groundEffectDuration = 2.5f;

    [Header("Cast Feedback")]
    public bool logAbilityCasts = true;
    public bool notifyUiOnCast = true;

    private readonly float[] nextReadyTimeBySlot = new float[4];
    private int lastProcessedSlot = -1;
    private int lastProcessedFrame = -1;

    private void Awake()
    {
        if (unitHealth == null)
        {
            unitHealth = GetComponent<UnitHealth>();
        }

        if (unitMana == null)
        {
            unitMana = GetComponent<UnitMana>();
        }

        if (survivorClassManager == null)
        {
            survivorClassManager = GetComponent<SurvivorClassManager>();
        }

        if (predatorClassManager == null)
        {
            predatorClassManager = GetComponent<PredatorClassManager>();
        }

        if (localRoleController == null)
        {
            localRoleController = FindFirstObjectByType<LocalRoleController>();
        }

        if (gameManager == null)
        {
            gameManager = ManiaGameManager.Instance;
        }

        if (abilityAudioSource == null)
        {
            abilityAudioSource = GetComponent<AudioSource>();
        }

        if (survivorClassManager != null)
        {
            controlsPredator = false;
        }
        else if (predatorClassManager != null)
        {
            controlsPredator = true;
        }

        if (unitMana == null)
        {
            unitMana = UnitMana.EnsureOn(gameObject, controlsPredator);
        }

        PurgeLegacySurvivorMana();

        if (disableLegacyAbilityComponents)
        {
            DisableLegacyComponentsOnHost();
        }

        if (abilityControllerOwnsCooldowns)
        {
            if (survivorClassManager != null)
            {
                survivorClassManager.useExternalCooldownAuthority = true;
            }

            if (predatorClassManager != null)
            {
                predatorClassManager.useExternalCooldownAuthority = true;
            }
        }

        ResetAllCooldowns();
        LogInitialCooldownState("Awake");
    }

    private void Start()
    {
        ResetAllCooldowns();
        LogInitialCooldownState("Start");
    }

    public void ResetAllCooldowns()
    {
        for (int i = 0; i < nextReadyTimeBySlot.Length; i++)
        {
            nextReadyTimeBySlot[i] = 0f;
        }
    }

    public void UseAbilitySlot(int slotNumber)
    {
        int clampedSlot = Mathf.Clamp(slotNumber, 1, 4);
        float now = Time.time;

        if (lastProcessedFrame == Time.frameCount && lastProcessedSlot == clampedSlot)
        {
            LogFlow("[AbilityController] Ignoring duplicate slot " + clampedSlot + " press in the same frame.");
            return;
        }

        lastProcessedFrame = Time.frameCount;
        lastProcessedSlot = clampedSlot;

        ResolveUnitMana();

        LogFlow($"[AbilityController] Slot pressed: {clampedSlot}");
        LogFlow($"[AbilityController] Received slot {clampedSlot}");
        LogFlow($"[AbilityController] Current role: {GetCurrentRoleLabel()}");
        LogFlow($"[AbilityController] Current class: {GetCurrentClassLabel()}");

        if (!CanUseBase(out string denyReason))
        {
            LogAbilityBlocked(clampedSlot, denyReason);
            LogFlow($"[AbilityController] Current class/loadout: {GetCurrentLoadoutLabel()}");
            LogFlow("[AbilityController] Ability resolved: none");
            LogFlow($"[AbilityController] Cooldown check: blocked ({denyReason})");
            LogFlow($"[AbilityController] Cooldown remaining: {GetCooldownRemaining(clampedSlot):0.00}s");
            LogFlow("[AbilityController] Can use ability? no");
            LogFlow("[AbilityController] Target found? no");
            LogFlow("[AbilityController] VFX assigned? no");
            LogFlow($"[AbilityController] Ability succeeded/failed: failed ({denyReason})");
            return;
        }

        float cooldownRemaining = GetCooldownRemaining(clampedSlot);
        if (cooldownRemaining > 0f)
        {
            LogAbilityBlocked(clampedSlot, "cooldown " + cooldownRemaining.ToString("0.0") + " seconds remaining");
            LogAbilityManaBlocked(clampedSlot, cooldownRemaining, "cooldown");
            LogFlow($"[AbilityController] Current class/loadout: {GetCurrentLoadoutLabel()}");
            LogFlow($"[AbilityController] Ability resolved: {GetResolvedAbilityName(clampedSlot)}");
            LogFlow($"[AbilityController] Cooldown blocked slot {clampedSlot}: {cooldownRemaining:0.00}s remaining");
            LogFlow($"[AbilityController] Cooldown remaining: {cooldownRemaining:0.00}s");
            LogFlow("[AbilityController] Can use ability? no");
            LogFlow("[AbilityController] Target found? no");
            LogFlow($"[AbilityController] VFX assigned? {HasAssignedVfx(clampedSlot).ToString().ToLowerInvariant()}");
            LogFlow("[AbilityController] Ability succeeded/failed: failed (cooldown)");
            PlayFailedCastSound();
            return;
        }

        float manaCost = GetSlotManaCostInternal(clampedSlot);
        if (manaCost > 0f)
        {
            if (unitMana == null)
            {
                LogAbilityBlocked(clampedSlot, "UnitMana missing — cannot spend mana");
                LogFlow("[AbilityController] Ability succeeded/failed: failed (UnitMana missing)");
                PlayNoManaSound();
                return;
            }

            if (!unitMana.HasMana(manaCost))
            {
                LogAbilityBlocked(clampedSlot, "insufficient mana (have " + unitMana.currentMana.ToString("0")
                    + ", need " + manaCost.ToString("0") + ")");
                LogAbilityManaBlocked(clampedSlot, unitMana.currentMana, manaCost, "insufficient mana");
                LogFlow($"[AbilityController] Insufficient mana for slot {clampedSlot}: need {manaCost:0.0}, have {unitMana.currentMana:0.0}");
                LogFlow("[AbilityController] Ability succeeded/failed: failed (insufficient mana)");
                PlayNoManaSound();
                return;
            }
        }
        else if (unitMana == null)
        {
            unitMana = GetComponent<UnitMana>();
        }

        bool targetFound = HasLikelyTarget(clampedSlot);
        bool assignedVfx = HasAssignedVfx(clampedSlot);
        string abilityName = GetResolvedAbilityName(clampedSlot);

        LogFlow($"[AbilityController] Current class/loadout: {GetCurrentLoadoutLabel()}");
        LogFlow($"[AbilityController] Ability resolved: {abilityName}");
        LogFlow("[AbilityController] Cooldown check: pass");
        LogFlow("[AbilityController] Cooldown remaining: 0.00s");
        LogFlow("[AbilityController] Can use ability? yes");
        LogFlow($"[AbilityController] Target found? {(targetFound ? "yes" : "no")}");
        LogFlow($"[AbilityController] VFX assigned? {(assignedVfx ? "yes" : "no")}");
        LogFlow("[AbilityController] Calling class manager: " + (controlsPredator ? "PredatorClassManager" : "SurvivorClassManager"));

        bool succeeded = ExecuteSlot(clampedSlot);
        if (succeeded)
        {
            float manaBefore = unitMana != null ? unitMana.currentMana : 0f;
            if (manaCost > 0f)
            {
                if (unitMana == null || !unitMana.SpendMana(manaCost))
                {
                    LogFlow("[AbilityController] Ability succeeded but mana spend failed — cooldown not started.");
                    LogFlow("[AbilityController] Ability succeeded/failed: failed (mana spend)");
                    return;
                }

                LogFlow($"[AbilityController] Spent {manaCost:0.0} mana on slot {clampedSlot}");
                LogAbilityManaSpend(clampedSlot, manaCost, manaBefore, unitMana.currentMana);
            }

            float cooldownDuration = GetSlotCooldown(clampedSlot);
            nextReadyTimeBySlot[clampedSlot - 1] = now + cooldownDuration;
            LogFlow($"[AbilityController] Starting cooldown for slot {clampedSlot}: {cooldownDuration:0.00}s");
            LogAbilityCast(clampedSlot);
            SpawnDebugVfx(clampedSlot, assignedVfx);
            PlaySlotSound(clampedSlot);
            SpawnGroundEffect(clampedSlot);
        }
        else
        {
            LogAbilityBlocked(clampedSlot, "ability execution failed");
        }

        LogFlow($"[AbilityController] Ability succeeded/failed: {(succeeded ? "succeeded" : "failed")}");
    }

    private bool CanUseBase(out string denyReason)
    {
        denyReason = string.Empty;

        if (unitHealth == null)
        {
            unitHealth = GetComponent<UnitHealth>();
        }

        if (unitHealth == null)
        {
            denyReason = "UnitHealth missing";
            return false;
        }

        if (unitHealth.IsDead)
        {
            denyReason = "unit dead";
            return false;
        }

        if (gameManager == null)
        {
            gameManager = ManiaGameManager.Instance;
        }

        if (gameManager != null && gameManager.State != ManiaGameState.Playing)
        {
            denyReason = "game not playing";
            return false;
        }

        return true;
    }

    public float GetCooldownRemaining(int slotNumber)
    {
        int clampedSlot = Mathf.Clamp(slotNumber, 1, 4);
        float now = Time.time;
        return Mathf.Max(0f, nextReadyTimeBySlot[clampedSlot - 1] - now);
    }

    public bool IsAbilityReady(int slotNumber)
    {
        return GetCooldownRemaining(slotNumber) <= 0f && HasSufficientMana(slotNumber);
    }

    public float GetSlotManaCost(int slotNumber)
    {
        return GetSlotManaCostInternal(slotNumber);
    }

    public float GetSlotCooldownDuration(int slotNumber)
    {
        return GetSlotCooldown(slotNumber);
    }

    public AbilityDetail GetAbilityDetail(int slotNumber)
    {
        return ResolveAbilityDetail(slotNumber);
    }

    private AbilityDetail GetAbilityDetailInternal(int slotNumber)
    {
        int clampedSlot = Mathf.Clamp(slotNumber, 1, 4);

        if (controlsPredator)
        {
            if (predatorClassManager == null)
            {
                predatorClassManager = GetComponent<PredatorClassManager>();
            }

            if (predatorClassManager != null)
            {
                return predatorClassManager.GetAbilityDetail(clampedSlot);
            }
        }
        else
        {
            if (survivorClassManager == null)
            {
                survivorClassManager = GetComponent<SurvivorClassManager>();
            }

            if (survivorClassManager != null)
            {
                return survivorClassManager.GetAbilityDetail(clampedSlot);
            }
        }

        return AbilityDetail.CreateFallback(clampedSlot, controlsPredator ? "Predator" : "Survivor");
    }

    private AbilityDetail ResolveAbilityDetail(int slotNumber)
    {
        AbilityDetail detail = GetAbilityDetailInternal(slotNumber);
        if (detail.IsConfigured)
        {
            return detail;
        }

        return AbilityPresentationFallback.ResolveForUi(controlsPredator, slotNumber, detail);
    }

    public string GetClassDisplayName()
    {
        if (controlsPredator)
        {
            return predatorClassManager != null
                ? predatorClassManager.GetClassDisplayName()
                : "Predator";
        }

        return survivorClassManager != null
            ? survivorClassManager.GetClassDisplayName()
            : "Survivor";
    }

    public string GetClassSummary()
    {
        if (controlsPredator)
        {
            return predatorClassManager != null
                ? predatorClassManager.GetClassSummary()
                : "Predator";
        }

        return survivorClassManager != null
            ? survivorClassManager.GetClassSummary()
            : "Survivor";
    }

    public bool HasSufficientMana(int slotNumber)
    {
        float cost = GetSlotManaCostInternal(slotNumber);
        if (cost <= 0f)
        {
            return true;
        }

        ResolveUnitMana();
        if (unitMana == null)
        {
            return false;
        }

        return unitMana.HasMana(cost);
    }

    private UnitMana ResolveUnitMana()
    {
        if (unitMana == null)
        {
            unitMana = GetComponent<UnitMana>();
        }

        if (unitMana == null)
        {
            unitMana = UnitMana.EnsureOn(gameObject, controlsPredator);
        }

        return unitMana;
    }

    private void PurgeLegacySurvivorMana()
    {
        if (controlsPredator)
        {
            return;
        }

        SurvivorMana legacy = GetComponent<SurvivorMana>();
        if (legacy != null)
        {
            Destroy(legacy);
        }
    }

    private float GetSlotManaCostInternal(int slotNumber)
    {
        if (controlsPredator)
        {
            switch (slotNumber)
            {
                case 1: return Mathf.Max(0f, predatorSlot1ManaCost);
                case 2: return Mathf.Max(0f, predatorSlot2ManaCost);
                case 3: return Mathf.Max(0f, predatorSlot3ManaCost);
                default: return Mathf.Max(0f, predatorSlot4ManaCost);
            }
        }

        switch (slotNumber)
        {
            case 1: return Mathf.Max(0f, survivorSlot1ManaCost);
            case 2: return Mathf.Max(0f, survivorSlot2ManaCost);
            case 3: return Mathf.Max(0f, survivorSlot3ManaCost);
            default: return Mathf.Max(0f, survivorSlot4ManaCost);
        }
    }

    private float GetSlotCooldown(int slotNumber)
    {
        if (controlsPredator)
        {
            switch (slotNumber)
            {
                case 1: return Mathf.Max(0f, predatorSlot1Cooldown);
                case 2: return Mathf.Max(0f, predatorSlot2Cooldown);
                case 3: return Mathf.Max(0f, predatorSlot3Cooldown);
                default: return Mathf.Max(0f, predatorSlot4Cooldown);
            }
        }

        switch (slotNumber)
        {
            case 1: return Mathf.Max(0f, survivorSlot1Cooldown) * GetAiSurvivorCooldownMultiplier();
            case 2: return Mathf.Max(0f, survivorSlot2Cooldown) * GetAiSurvivorCooldownMultiplier();
            case 3: return Mathf.Max(0f, survivorSlot3Cooldown) * GetAiSurvivorCooldownMultiplier();
            default: return Mathf.Max(0f, survivorSlot4Cooldown) * GetAiSurvivorCooldownMultiplier();
        }
    }

    private float GetAiSurvivorCooldownMultiplier()
    {
        if (controlsPredator)
        {
            return 1f;
        }

        OfflineSurvivorBotAI botAi = GetComponent<OfflineSurvivorBotAI>();
        return botAi != null ? botAi.GetAbilityCooldownMultiplier() : 1f;
    }

    private bool ExecuteSlot(int slotNumber)
    {
        if (controlsPredator)
        {
            if (predatorClassManager == null)
            {
                predatorClassManager = GetComponent<PredatorClassManager>();
            }

            if (predatorClassManager == null)
            {
                Debug.LogWarning("[AbilityController] Missing PredatorClassManager on Predator prefab '" + gameObject.name + "'.");
                return false;
            }

            if (abilityControllerOwnsCooldowns)
            {
                predatorClassManager.useExternalCooldownAuthority = true;
            }

            if (enforceTestLoadouts)
            {
                predatorClassManager.activeClass = PredatorClass.RelentlessHook;
            }

            if (predatorClassManager.activeClass != PredatorClass.RelentlessHook)
            {
                Debug.LogWarning("[AbilityController] Predator class disabled for this pass. Use RelentlessHook.");
                return false;
            }

            switch (slotNumber)
            {
                case 1:
                    return predatorClassManager.CastMeleeAttack();
                case 2:
                    return predatorClassManager.CastAbility1();
                case 3:
                    return predatorClassManager.CastAbility2();
                case 4:
                    return predatorClassManager.CastUltimate();
                default:
                    return false;
            }
        }

        if (survivorClassManager == null)
        {
            survivorClassManager = GetComponent<SurvivorClassManager>();
        }

        if (survivorClassManager == null)
        {
            Debug.LogWarning("[AbilityController] Missing SurvivorClassManager on Survivor prefab '" + gameObject.name + "'.");
            return false;
        }

        if (abilityControllerOwnsCooldowns)
        {
            survivorClassManager.useExternalCooldownAuthority = true;
        }

        if (enforceTestLoadouts)
        {
            survivorClassManager.activeClass = SurvivorClass.Medic;
        }

        if (survivorClassManager.activeClass != SurvivorClass.Medic)
        {
            Debug.LogWarning("[AbilityController] Survivor class disabled for this pass. Use Medic.");
            return false;
        }

        switch (slotNumber)
        {
            case 1:
                return survivorClassManager.ExecutePrimary();
            case 2:
                return survivorClassManager.ExecuteAbility2();
            case 3:
                return survivorClassManager.ExecuteAbility3();
            case 4:
                return survivorClassManager.ExecuteUltimate();
            default:
                return false;
        }
    }

    private string GetCurrentLoadoutLabel()
    {
        if (controlsPredator)
        {
            if (predatorClassManager == null)
            {
                return "Predator (missing manager)";
            }

            return $"Predator/{predatorClassManager.activeClass}";
        }

        if (survivorClassManager == null)
        {
            return "Survivor (missing manager)";
        }

        return $"Survivor/{survivorClassManager.activeClass}";
    }

    private string GetCurrentRoleLabel()
    {
        return controlsPredator ? "Predator" : "Survivor";
    }

    private string GetCurrentClassLabel()
    {
        if (controlsPredator)
        {
            return predatorClassManager != null ? predatorClassManager.activeClass.ToString() : "None";
        }

        return survivorClassManager != null ? survivorClassManager.activeClass.ToString() : "None";
    }

    private string GetResolvedAbilityName(int slotNumber)
    {
        AbilityDetail detail = GetAbilityDetail(slotNumber);
        string className = GetClassDisplayName();
        if (detail.HasDisplayName)
        {
            return className + " - " + detail.displayName;
        }

        return className + " - Slot " + slotNumber;
    }

    private void LogAbilityCast(int slotNumber)
    {
        AbilityDetail detail = GetAbilityDetail(slotNumber);
        string roleLabel = controlsPredator ? "Predator" : GetClassDisplayName();
        string abilityLabel = detail.HasDisplayName ? detail.displayName : "Slot " + slotNumber;

        if (logAbilityCasts)
        {
            Debug.Log("[AbilityCast] " + roleLabel + " used " + abilityLabel);
        }

        if (!notifyUiOnCast)
        {
            return;
        }

        ManiaGameUI ui = FindFirstObjectByType<ManiaGameUI>();
        if (ui != null)
        {
            ui.ShowAbilityCastFlash(detail);
        }
    }

    private bool HasLikelyTarget(int slotNumber)
    {
        if (controlsPredator)
        {
            if (slotNumber == 3)
            {
                return true;
            }

            Collider[] survivors = Physics.OverlapSphere(transform.position, 14f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < survivors.Length; i++)
            {
                UnitHealth target = survivors[i].GetComponentInParent<UnitHealth>();
                if (target != null && !target.IsDead && target.CompareTag("Survivor"))
                {
                    return true;
                }
            }

            return false;
        }

        if (slotNumber == 2 || slotNumber == 4)
        {
            return true;
        }

        Collider[] allies = Physics.OverlapSphere(transform.position, 20f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < allies.Length; i++)
        {
            UnitHealth target = allies[i].GetComponentInParent<UnitHealth>();
            if (target != null && !target.IsDead && target != unitHealth && target.CompareTag("Survivor"))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasAssignedVfx(int slotNumber)
    {
        if (controlsPredator && predatorClassManager != null)
        {
            switch (slotNumber)
            {
                case 1:
                    return false;
                case 2:
                    return predatorClassManager.hookProjectilePrefab != null;
                case 3:
                    return false;
                default:
                    return false;
            }
        }

        if (!controlsPredator && survivorClassManager != null)
        {
            switch (slotNumber)
            {
                case 1:
                    return survivorClassManager.bioticDartProjectilePrefab != null;
                case 2:
                    return false;
                case 3:
                    return false;
                default:
                    return survivorClassManager.immortalityFieldPrefab != null;
            }
        }

        return false;
    }

    private void SpawnDebugVfx(int slotNumber, bool hasAssignedVfx)
    {
        AbilityDetail detail = GetAbilityDetail(slotNumber);

        if (detail.castVfxPrefab != null)
        {
            GameObject castVfx = Instantiate(detail.castVfxPrefab, transform.position, transform.rotation);
            Debug.Log("[AbilityVFX] Spawned cast VFX: " + castVfx.name + " for slot " + slotNumber);
            Destroy(castVfx, Mathf.Max(0.1f, placeholderVfxLifetime));
            return;
        }

        if (hasAssignedVfx || !spawnPlaceholderVfx)
        {
            return;
        }

        PrimitiveType type = slotNumber == 4 ? PrimitiveType.Cylinder : PrimitiveType.Sphere;
        Color tint = detail.themeColor.a > 0.01f
            ? detail.themeColor
            : (controlsPredator ? new Color(1f, 0.4f, 0.1f, 1f) : new Color(0.2f, 0.95f, 0.4f, 1f));
        float size = slotNumber == 4 ? 1.6f : 0.45f;
        Vector3 pos = transform.position + Vector3.up * (slotNumber == 4 ? 0.05f : 1f);

        GameObject vfx = GameObject.CreatePrimitive(type);
        vfx.name = controlsPredator ? "PredatorAbilityPlaceholderVFX" : "SurvivorAbilityPlaceholderVFX";
        vfx.transform.position = pos;
        vfx.transform.localScale = type == PrimitiveType.Cylinder
            ? new Vector3(size, 0.05f, size)
            : Vector3.one * size;

        Collider col = vfx.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        MeshRenderer renderer = vfx.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = tint;
            renderer.material = material;
        }

        float lifetime = Mathf.Max(0.1f, placeholderVfxLifetime);
        Debug.Log($"[AbilityVFX] Spawned VFX: {vfx.name}, position={vfx.transform.position}, duration={lifetime:0.00}s");
        Destroy(vfx, lifetime);
    }

    private void PlaySlotSound(int slotNumber)
    {
        AbilityDetail detail = GetAbilityDetail(slotNumber);
        AudioClip clip = detail.castSound != null ? detail.castSound : GetSlotSound(slotNumber);

        if (controlsPredator)
        {
            if (clip != null)
            {
                PlayAbilityClip(clip, AbilityPlaceholderSound.Generic, clip == null);
            }

            return;
        }

        AbilityPlaceholderSound fallback = slotNumber <= 2
            ? AbilityPlaceholderSound.Heal
            : AbilityPlaceholderSound.Generic;
        PlayAbilityClip(clip, fallback, clip == null);
    }

    private void PlayFailedCastSound()
    {
        PlayAbilityClip(cooldownFailSound != null ? cooldownFailSound : failedCastSound, AbilityPlaceholderSound.CooldownFail, true);
    }

    private void PlayNoManaSound()
    {
        PlayAbilityClip(noManaSound, AbilityPlaceholderSound.NoMana, true);
    }

    private void PlayAbilityClip(AudioClip clip, AbilityPlaceholderSound fallback, bool allowPlaceholder)
    {
        if (abilityAudioSource == null)
        {
            abilityAudioSource = GetComponent<AudioSource>();
        }

        AbilityPlaceholderAudio.Play(
            abilityAudioSource,
            clip,
            fallback,
            abilitySfxVolume,
            allowPlaceholder && usePlaceholderAbilityAudio);

        if (logAbilityAudio && logAbilityFlow && clip != null)
        {
            Debug.Log("[AbilityAudio] Played sound " + clip.name);
        }
    }

    private AudioClip GetSlotSound(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1: return slot1Sound;
            case 2: return slot2Sound;
            case 3: return slot3Sound;
            default: return slot4Sound;
        }
    }

    private void SpawnGroundEffect(int slotNumber)
    {
        if (!spawnGroundEffects)
        {
            return;
        }

        float radius = slotNumber >= 4 ? 4f : slotNumber == 2 ? 3f : 2.5f;
        Color tint = controlsPredator
            ? (slotNumber == 3
                ? new Color(1f, 0.55f, 0.15f, 0.75f)
                : new Color(1f, 0.25f, 0.1f, 0.75f))
            : (slotNumber == 4
                ? new Color(0.2f, 0.75f, 1f, 0.75f)
                : new Color(0.2f, 0.95f, 0.45f, 0.75f));

        TemporaryGroundEffect.Spawn(
            transform.position,
            tint,
            groundEffectDuration,
            radius,
            groundEffectPrefab,
            logAbilityFlow && pipelineTestMode);
    }

    private void LogInitialCooldownState(string phase)
    {
        if (!logAbilityFlow || !pipelineTestMode)
        {
            return;
        }

        Debug.Log("[AbilityController] Initial cooldown state (" + phase + ") on '" + gameObject.name + "': "
            + "slot 1 = " + FormatSlotCooldownState(1) + ", "
            + "slot 2 = " + FormatSlotCooldownState(2) + ", "
            + "slot 3 = " + FormatSlotCooldownState(3) + ", "
            + "slot 4 = " + FormatSlotCooldownState(4));
    }

    private string FormatSlotCooldownState(int slotNumber)
    {
        float remaining = GetCooldownRemaining(slotNumber);
        return remaining <= 0f ? "ready" : remaining.ToString("0.00") + "s";
    }

    private void LogFlow(string message)
    {
        if (!logAbilityFlow || !pipelineTestMode)
        {
            return;
        }

        Debug.Log(message);
    }

    private void LogAbilityBlocked(int slotNumber, string reason)
    {
        if (!logAbilityFlow)
        {
            return;
        }

        Debug.Log("[AbilityBlock] " + GetCurrentRoleLabel() + " Slot " + slotNumber + " blocked: " + reason);
    }

    private void LogAbilityManaBlocked(int slotNumber, float value, string reason)
    {
        if (!logAbilityMana || controlsPredator)
        {
            return;
        }

        Debug.Log("[AbilityMana] Survivor slot " + slotNumber + " " + GetResolvedAbilityName(slotNumber)
            + " blocked: " + reason + " " + value.ToString("0.0") + "s");
    }

    private void LogAbilityManaBlocked(int slotNumber, float currentMana, float cost, string reason)
    {
        if (!logAbilityMana || controlsPredator)
        {
            return;
        }

        Debug.Log("[AbilityMana] Survivor slot " + slotNumber + " " + GetResolvedAbilityName(slotNumber)
            + " blocked: " + reason + " current=" + currentMana.ToString("0") + " cost=" + cost.ToString("0"));
    }

    private void LogAbilityManaSpend(int slotNumber, float cost, float before, float after)
    {
        if (controlsPredator)
        {
            return;
        }

        if (logAbilityMana || logAbilityFlow)
        {
            Debug.Log("[AbilityMana] Survivor spent " + cost.ToString("0") + " mana on Slot " + slotNumber
                + " (before=" + before.ToString("0") + ", after=" + after.ToString("0") + ")");
        }
    }

    private void DisableLegacyComponentsOnHost()
    {
        if (controlsPredator)
        {
            MonsterRoarAbility roar = GetComponent<MonsterRoarAbility>();
            if (roar != null && roar.enabled)
            {
                roar.enabled = false;
                Debug.Log("[AbilityController] Disabled legacy component: MonsterRoarAbility");
            }

            MonsterStompAbility stomp = GetComponent<MonsterStompAbility>();
            if (stomp != null && stomp.enabled)
            {
                stomp.enabled = false;
                Debug.Log("[AbilityController] Disabled legacy component: MonsterStompAbility");
            }
            return;
        }

        SurvivorSpiritBoltAbility spiritBolt = GetComponent<SurvivorSpiritBoltAbility>();
        if (spiritBolt != null && spiritBolt.enabled)
        {
            spiritBolt.enabled = false;
            Debug.Log("[AbilityController] Disabled legacy component: SurvivorSpiritBoltAbility");
        }

        SurvivorBlinkAbility blink = GetComponent<SurvivorBlinkAbility>();
        if (blink != null && blink.enabled)
        {
            blink.enabled = false;
            Debug.Log("[AbilityController] Disabled legacy component: SurvivorBlinkAbility");
        }

        SurvivorSeedGuardAbility seedGuard = GetComponent<SurvivorSeedGuardAbility>();
        if (seedGuard != null && seedGuard.enabled)
        {
            seedGuard.enabled = false;
            Debug.Log("[AbilityController] Disabled legacy component: SurvivorSeedGuardAbility");
        }

        SurvivorSoulwoodAvatarAbility avatarAbility = GetComponent<SurvivorSoulwoodAvatarAbility>();
        if (avatarAbility != null && avatarAbility.enabled)
        {
            avatarAbility.enabled = false;
            Debug.Log("[AbilityController] Disabled legacy component: SurvivorSoulwoodAvatarAbility");
        }
    }
}
