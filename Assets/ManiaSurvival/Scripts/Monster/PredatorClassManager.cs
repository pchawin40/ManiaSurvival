using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PredatorClass { SwarmOverlord, SubterraneanStalker, DoomShieldColossus, Juggernaut, CyberNinja, Vanguard, RelentlessHook }

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitHealth))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PredatorClassManager : MonoBehaviour
{
    private const float SharedGlobalCooldown = 0.25f;

    // Required cached hashes (Primary = MeleeAttack, slot2 = Ability1, slot3 = Ability2).
    private static readonly int MeleeAttackHash = UnitAnimationHelper.Predator.Primary;
    private static readonly int Ability1Hash = UnitAnimationHelper.Predator.Ability2;
    private static readonly int Ability2Hash = UnitAnimationHelper.Predator.Ability3;
    private static readonly int Ability3Hash = Animator.StringToHash("Ability3");
    private static readonly int UltimateHash = UnitAnimationHelper.Predator.Ultimate;

    [Header("Class")]
    public PredatorClass activeClass = PredatorClass.SwarmOverlord;

    [Header("Targets")]
    public string survivorTag = "Survivor";
    public LayerMask targetLayers = ~0;
    public float baseMeleeRange = 2f;
    public int baseMeleeDamage = 10;

    [Header("Shared")]
    public bool showDebugLogs = true;
    public bool useExternalCooldownAuthority = false;
    public Transform projectileSpawn;

    [Header("Prefabs")]
    public GameObject sentryTurretPrefab;
    public GameObject minionPrefab;
    public GameObject moltenHazardPrefab;
    public GameObject ringBarrierPrefab;
    public GameObject shurikenProjectilePrefab;
    public GameObject fireStrikeProjectilePrefab;
    public GameObject hookProjectilePrefab;
    public GameObject tremorShockwavePrefab;

    [Header("Durations / Scalars")]
    public float burrowMoveBonusMultiplier = 1.5f;
    public float burrowDamageReduction = 1f; // 1 = full immunity while burrowed
    public float deflectDuration = 1.5f;
    public float dragonbladeDuration = 6f;
    public float dragonbladePrimaryMultiplier = 1.8f;

    private UnitHealth unitHealth;
    private CharacterController characterController;
    private Animator animator;
    private MonsterPlayerMovement monsterMovement;
    private ManiaGameManager gameManager;

    private float nextGlobalCastTime;
    private bool isBurrowed;
    private bool isDeflecting;
    private float dragonbladeEndTime;
    private int cachedHealthForReduction;

    // CyberNinja: cooldown reset on kill.
    public float cyberSwiftStrikeCooldown = 8f;
    private float nextCyberSwiftStrikeTime;
    [Header("Relentless Hook - Spray (basic cone poke)")]
    [Tooltip("Short-range cone poke. Softens survivors between Hook setups; keep damage modest.")]
    public float sprayRange = 8f;
    public float sprayHalfAngle = 45f;
    public int sprayDamage = 16;
    public float sprayKnockback = 6.5f;
    public float sprayCloseRangeForgiveness = 2f;
    public float sprayCloseRangeMaxAngle = 140f;

    [Header("Relentless Hook - Spray VFX")]
    [Tooltip("Fan mesh segment count for Spray cone visual.")]
    public int sprayVfxSegments = 14;
    [Tooltip("Forward offset so the fan starts in front of the predator body.")]
    public float sprayVfxForwardOffset = 1f;
    [Tooltip("Height above the ground for the Spray fan mesh.")]
    public float sprayVfxVerticalOffset = 0.1f;
    [Tooltip("How long the Spray fan visual stays visible.")]
    public float sprayVfxLifetime = 0.45f;
    [Range(0f, 1f)]
    public float sprayVfxAlpha = 0.35f;

    [Header("Relentless Hook - Hook (pick / catch tool)")]
    [Tooltip("Skill shot pull. Lower burst damage — the threat is repositioning, not instant melt.")]
    public float hookRange = 24f;
    public int hookDamage = 6;
    public float hookPullForwardDistance = 1.75f;
    public float hookPullDuration = 0.25f;

    [Header("Relentless Hook - Tonic (self-heal + danger gas)")]
    [Tooltip("Self-heal plus toxic gas. Predator slows while active — survivors get time to reposition.")]
    public int tonicHealAmount = 35;
    public float tonicDuration = 2.5f;
    [Tooltip("Movement multiplier while Tonic is active. Values below 1 slow the Predator.")]
    public float tonicSelfSpeedMultiplier = 0.55f;
    public float tonicGasRadius = 4.5f;
    public float tonicGasDamagePerSecond = 5f;
    public float tonicGasPropDamagePerSecond = 5f;
    [Tooltip("Survivor move speed multiplier while poisoned by Tonic gas (lower = slower).")]
    public float tonicGasSlowMultiplier = 0.275f;
    [Tooltip("How long the poison slow lasts after touching Tonic gas.")]
    public float tonicGasSlowDuration = 2.5f;

    [Header("Relentless Hook - Barrage (ultimate / big burst)")]
    [Tooltip("Telegraphed ultimate. Warning strip + long cooldown — scary, but survivors can react.")]
    public float barrageDuration = 3.5f;
    public float barragePulseInterval = 0.7f;
    public float barrageRange = 20f;
    [Tooltip("Half-width of Barrage cone in degrees. Full cone = 2x this value.")]
    public float barrageHalfAngle = 17.5f;
    public int barrageDamagePerPulse = 9;
    public float barrageKnockbackMin = 2.8f;
    public float barrageKnockbackMax = 4f;
    [Tooltip("Forward offset for Barrage cone VFX.")]
    public float barrageVfxForwardOffset = 1f;
    [Tooltip("Ground height offset for Barrage cone VFX.")]
    public float barrageVfxVerticalOffset = 0.1f;
    [Range(0f, 1f)] public float barrageWarningAlpha = 0.28f;
    [Range(0f, 1f)] public float barragePulseAlpha = 0.42f;
    [Tooltip("Fan mesh segment count for Barrage warning/pulse VFX.")]
    public int barrageVfxSegments = 14;

    [Header("Relentless Hook - Cast Presentation")]
    public float sprayCastWindup = 0.12f;
    public float hookCastWindup = 0.25f;
    public float tonicCastWindup = 0.2f;
    public float barrageCastWindup = 0.35f;
    public bool logCastPresentation = true;

    [Header("Relentless Hook - Feel")]
    public bool enableAbilityFeel = true;
    public bool logBarrageVfx = false;
    [Tooltip("Seconds survivors see the warning strip before Barrage pulses begin.")]
    public float barrageWarningDuration = 0.75f;
    public float barrageCraterDuration = 10f;
    public float barrageBombPropRadius = 2.5f;
    public int barragePropDamage = 8;

    [Header("Relentless Hook - Audio")]
    public AudioSource abilityAudioSource;
    [Range(0f, 1f)] public float abilitySfxVolume = 0.85f;
    public bool usePlaceholderAbilityAudio = true;
    public AudioClip spraySound;
    public AudioClip hookFireSound;
    public AudioClip hookHitSound;
    public AudioClip tonicSound;
    public AudioClip barrageStartSound;
    public AudioClip barrageBombSound;
    public AudioClip barrageEndSound;

    [Header("Relentless Hook - Ability Identity")]
    public string relentlessHookDisplayName = "Relentless Hook";
    [TextArea(2, 4)]
    public string relentlessHookClassSummary = "Relentless Hook — Catch predator. Pull survivors, zone them, punish bad positioning.";
    public AbilityDetail relentlessSlot1Detail;
    public AbilityDetail relentlessSlot2Detail;
    public AbilityDetail relentlessSlot3Detail;
    public AbilityDetail relentlessSlot4Detail;

    private Coroutine relentlessBarrageRoutine;
    private bool isBarrageActive;
    private readonly List<GameObject> barragePulseVfx = new List<GameObject>();
    private Coroutine tonicEffectRoutine;
    private GameObject tonicGasVfx;
    private GameObject activeHookChain;
    private readonly Dictionary<UnitHealth, float> tonicGasPendingDamage = new Dictionary<UnitHealth, float>();
    private readonly Dictionary<DestructiblePropHealth, float> tonicGasPendingPropDamage = new Dictionary<DestructiblePropHealth, float>();

    private void Awake()
    {
        unitHealth = GetComponent<UnitHealth>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        monsterMovement = GetComponent<MonsterPlayerMovement>();
        gameManager = ManiaGameManager.Instance;
        cachedHealthForReduction = unitHealth != null ? unitHealth.currentHealth : 0;

        if (abilityAudioSource == null)
        {
            abilityAudioSource = GetComponent<AudioSource>();
        }

        EnsureRelentlessAbilityDetails();
    }

    public string GetClassDisplayName()
    {
        return activeClass == PredatorClass.RelentlessHook ? relentlessHookDisplayName : activeClass.ToString();
    }

    public string GetClassSummary()
    {
        if (activeClass == PredatorClass.RelentlessHook)
        {
            return relentlessHookClassSummary;
        }

        return GetClassDisplayName();
    }

    public AbilityDetail GetAbilityDetail(int slotNumber)
    {
        EnsureRelentlessAbilityDetails();

        switch (slotNumber)
        {
            case 1: return relentlessSlot1Detail;
            case 2: return relentlessSlot2Detail;
            case 3: return relentlessSlot3Detail;
            case 4: return relentlessSlot4Detail;
            default: return AbilityDetail.CreateFallback(slotNumber, relentlessHookDisplayName);
        }
    }

    private void EnsureRelentlessAbilityDetails()
    {
        if (!relentlessSlot1Detail.IsConfigured)
        {
            relentlessSlot1Detail = CreateDefaultRelentlessSlot1Detail();
        }

        if (!relentlessSlot2Detail.IsConfigured)
        {
            relentlessSlot2Detail = CreateDefaultRelentlessSlot2Detail();
        }

        if (!relentlessSlot3Detail.IsConfigured)
        {
            relentlessSlot3Detail = CreateDefaultRelentlessSlot3Detail();
        }

        if (!relentlessSlot4Detail.IsConfigured)
        {
            relentlessSlot4Detail = CreateDefaultRelentlessSlot4Detail();
        }

        SyncRelentlessPresentationHooks();
    }

    private void SyncRelentlessPresentationHooks()
    {
        if (relentlessSlot1Detail.castSound == null)
        {
            relentlessSlot1Detail.castSound = spraySound;
        }

        if (relentlessSlot2Detail.castSound == null)
        {
            relentlessSlot2Detail.castSound = hookFireSound;
        }

        if (relentlessSlot2Detail.hitSound == null)
        {
            relentlessSlot2Detail.hitSound = hookHitSound;
        }

        if (relentlessSlot3Detail.castSound == null)
        {
            relentlessSlot3Detail.castSound = tonicSound;
        }

        if (relentlessSlot4Detail.castSound == null)
        {
            relentlessSlot4Detail.castSound = barrageStartSound;
        }
    }

    private static AbilityDetail CreateDefaultRelentlessSlot1Detail()
    {
        return new AbilityDetail
        {
            displayName = "Razor Spray",
            buttonLabel = "Spray",
            shortDescription = "Fire a short cone blast that damages and knocks survivors back.",
            flavorText = "A brutal fan of shrapnel meant to break formations.",
            roleTag = "Cone / Poke",
            themeColor = new Color(1f, 0.45f, 0.25f, 1f)
        };
    }

    private static AbilityDetail CreateDefaultRelentlessSlot2Detail()
    {
        return new AbilityDetail
        {
            displayName = "Harpoon Hook",
            buttonLabel = "Hook",
            shortDescription = "Pull a survivor out of position.",
            flavorText = "One bad step and the predator drags you into danger.",
            roleTag = "Catch / Control",
            themeColor = new Color(0.85f, 0.55f, 0.25f, 1f)
        };
    }

    private static AbilityDetail CreateDefaultRelentlessSlot3Detail()
    {
        return new AbilityDetail
        {
            displayName = "Toxic Tonic",
            buttonLabel = "Tonic",
            shortDescription = "Heal yourself and release a slowing danger cloud.",
            flavorText = "The predator recovers while the air turns poisonous.",
            roleTag = "Self Heal / Zone",
            themeColor = new Color(0.35f, 0.95f, 0.45f, 1f)
        };
    }

    private static AbilityDetail CreateDefaultRelentlessSlot4Detail()
    {
        return new AbilityDetail
        {
            displayName = "Barrage",
            buttonLabel = "Barrage",
            shortDescription = "Warn, then fire repeated cone blasts in front of you.",
            flavorText = "A telegraphed storm of violence. Move or get shredded.",
            roleTag = "Ultimate / Burst",
            themeColor = new Color(1f, 0.3f, 0.2f, 1f)
        };
    }

    private void Update()
    {
        if (unitHealth != null && !unitHealth.IsDead)
        {
            cachedHealthForReduction = Mathf.Max(cachedHealthForReduction, unitHealth.currentHealth);
        }

        if (dragonbladeEndTime > 0f && Time.time >= dragonbladeEndTime)
        {
            dragonbladeEndTime = 0f;
        }
    }

    private void OnDisable()
    {
        StopRelentlessBarrage("disabled");
        StopTonicEffects("disabled");
    }

    // Slot 0: melee / primary.
    public bool CastMeleeAttack()
    {
        Debug.Log("[PredatorClassManager] Executing ability: " + activeClass + "/Primary");
        if (!TryConsumeSharedGcd(MeleeAttackHash))
        {
            return false;
        }

        switch (activeClass)
        {
            case PredatorClass.SwarmOverlord: CastSwarmOverlordForgeHammer(); break;
            case PredatorClass.SubterraneanStalker: CastSubterraneanDrillGauntlets(); break;
            case PredatorClass.DoomShieldColossus: CastDoomShieldGauntletPummel(); break;
            case PredatorClass.Juggernaut: CastJuggernautHandCannon(); break;
            case PredatorClass.CyberNinja: CastCyberNinjaShurikenFan(); break;
            case PredatorClass.Vanguard: CastVanguardRocketHammer(); break;
            case PredatorClass.RelentlessHook: CastRelentlessHookScrapGun(); break;
        }

        return true;
    }

    // Slot 1
    public bool CastAbility1()
    {
        Debug.Log("[PredatorClassManager] Executing ability: " + activeClass + "/Slot2");
        if (!TryConsumeSharedGcd(Ability1Hash))
        {
            return false;
        }

        switch (activeClass)
        {
            case PredatorClass.SwarmOverlord: CastSwarmOverlordDeploySentryTurret(); break;
            case PredatorClass.SubterraneanStalker: CastSubterraneanBurrowUnderground(); break;
            case PredatorClass.DoomShieldColossus: CastDoomShieldKineticGrasp(); break;
            case PredatorClass.Juggernaut: StartCoroutine(CastJuggernautRocketPunchCoroutine()); break;
            case PredatorClass.CyberNinja: StartCoroutine(CastCyberNinjaSwiftStrikeCoroutine()); break;
            case PredatorClass.Vanguard: StartCoroutine(CastVanguardChargeCoroutine()); break;
            case PredatorClass.RelentlessHook: StartCoroutine(CastRelentlessChainHookCoroutine()); break;
        }

        return true;
    }

    // Slot 2
    public bool CastAbility2()
    {
        Debug.Log("[PredatorClassManager] Executing ability: " + activeClass + "/Slot3");
        if (!TryConsumeSharedGcd(Ability2Hash))
        {
            return false;
        }

        switch (activeClass)
        {
            case PredatorClass.SwarmOverlord: CastSwarmOverlordCallMinionSwarm(); break;
            case PredatorClass.SubterraneanStalker: StartCoroutine(CastSubterraneanEruptionDashCoroutine()); break;
            case PredatorClass.DoomShieldColossus: StartCoroutine(CastDoomShieldCarnageOverrunCoroutine()); break;
            case PredatorClass.Juggernaut: StartCoroutine(CastJuggernautSeismicSlamCoroutine()); break;
            case PredatorClass.CyberNinja: StartCoroutine(CastCyberNinjaDeflectCoroutine()); break;
            case PredatorClass.Vanguard: CastVanguardFireStrike(); break;
            case PredatorClass.RelentlessHook: StartCoroutine(CastRelentlessTakeABreatherCoroutine()); break;
        }

        return true;
    }

    // Slot 3
    public bool CastAbility3()
    {
        Debug.Log("[PredatorClassManager] Executing ability: " + activeClass + "/Slot4");
        if (activeClass == PredatorClass.RelentlessHook && IsRelentlessBarrageActive())
        {
            LogPredatorAbility("Barrage rejected: already active.");
            return false;
        }

        if (!TryConsumeSharedGcd(Ability3Hash))
        {
            return false;
        }

        switch (activeClass)
        {
            case PredatorClass.SwarmOverlord: CastSwarmOverlordMoltenCore(); break;
            case PredatorClass.SubterraneanStalker: CastSubterraneanTectonicFault(); break;
            case PredatorClass.DoomShieldColossus: CastDoomShieldCageMatch(); break;
            case PredatorClass.Juggernaut: StartCoroutine(CastJuggernautMeteorStrikeCoroutine()); break;
            case PredatorClass.CyberNinja: CastCyberNinjaDragonblade(); break;
            case PredatorClass.Vanguard: CastVanguardEarthshatter(); break;
            case PredatorClass.RelentlessHook: return StartRelentlessBarrage();
        }

        return true;
    }

    // Slot 4 / ultimate.
    public bool CastUltimate()
    {
        Debug.Log("[PredatorClassManager] Executing ability: " + activeClass + "/Ultimate");
        if (activeClass == PredatorClass.RelentlessHook && IsRelentlessBarrageActive())
        {
            LogPredatorAbility("Barrage rejected: already active.");
            return false;
        }

        if (!TryConsumeSharedGcd(UltimateHash))
        {
            return false;
        }

        switch (activeClass)
        {
            case PredatorClass.SwarmOverlord: CastSwarmOverlordMoltenCore(); break;
            case PredatorClass.SubterraneanStalker: CastSubterraneanTectonicFault(); break;
            case PredatorClass.DoomShieldColossus: CastDoomShieldCageMatch(); break;
            case PredatorClass.Juggernaut: StartCoroutine(CastJuggernautMeteorStrikeCoroutine()); break;
            case PredatorClass.CyberNinja: CastCyberNinjaDragonblade(); break;
            case PredatorClass.Vanguard: CastVanguardEarthshatter(); break;
            case PredatorClass.RelentlessHook: return StartRelentlessBarrage();
        }

        return true;
    }

    private bool StartRelentlessBarrage()
    {
        if (IsRelentlessBarrageActive())
        {
            LogPredatorAbility("Barrage rejected: already active.");
            return false;
        }

        if (unitHealth == null || unitHealth.IsDead)
        {
            LogPredatorAbility("Barrage rejected: predator dead.");
            return false;
        }

        if (!IsRoundActive())
        {
            LogPredatorAbility("Barrage rejected: round not active.");
            return false;
        }

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            LogPredatorAbility("Barrage rejected: predator inactive.");
            return false;
        }

        relentlessBarrageRoutine = StartCoroutine(CastRelentlessBarrageCoroutine());
        return relentlessBarrageRoutine != null;
    }

    private bool IsRelentlessBarrageActive()
    {
        return isBarrageActive || relentlessBarrageRoutine != null;
    }

    private void StopRelentlessBarrage(string reason)
    {
        if (relentlessBarrageRoutine != null)
        {
            StopCoroutine(relentlessBarrageRoutine);
            relentlessBarrageRoutine = null;
        }

        if (isBarrageActive)
        {
            isBarrageActive = false;
            ClearBarragePulseVfx();
            LogPredatorAbility("Barrage ended (" + reason + ")");
        }
    }

    private bool TryConsumeSharedGcd(int triggerHash)
    {
        if (unitHealth == null)
        {
            if (showDebugLogs)
            {
                Debug.Log("[PredatorClassManager] Cast rejected: UnitHealth reference is missing.");
            }

            return false;
        }

        if (unitHealth.IsDead)
        {
            if (showDebugLogs)
            {
                Debug.Log("[PredatorClassManager] Cast rejected: Unit is dead.");
            }

            return false;
        }

        if (gameManager != null && gameManager.State != ManiaGameState.Playing)
        {
            if (showDebugLogs)
            {
                Debug.Log("[PredatorClassManager] Cast rejected: Game state is not Playing (" + gameManager.State + ").");
            }

            return false;
        }

        if (Time.time < nextGlobalCastTime)
        {
            if (useExternalCooldownAuthority)
            {
                TriggerPredatorAnimation(triggerHash);
                return true;
            }

            if (showDebugLogs)
            {
                float remaining = Mathf.Max(0f, nextGlobalCastTime - Time.time);
                Debug.Log("[PredatorClassManager] Cast rejected: Global Cooldown Active (" + remaining.ToString("0.00") + "s).");
            }

            return false;
        }

        TriggerPredatorAnimation(triggerHash);

        nextGlobalCastTime = Time.time + SharedGlobalCooldown;
        return true;
    }

    private void TriggerPredatorAnimation(int triggerHash)
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        UnitAnimationHelper.TrySetAnimatorTrigger(animator, triggerHash, null, logCastPresentation, this);
    }

    private void PlayRelentlessCastSound(int slotNumber)
    {
        AbilityDetail detail = GetAbilityDetail(slotNumber);
        if (detail.castSound == null)
        {
            return;
        }

        if (abilityAudioSource == null)
        {
            abilityAudioSource = GetComponent<AudioSource>();
        }

        if (abilityAudioSource != null)
        {
            abilityAudioSource.PlayOneShot(detail.castSound, abilitySfxVolume);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SwarmOverlord
    // ──────────────────────────────────────────────────────────────────────────

    private void CastSwarmOverlordForgeHammer()
    {
        UnitHealth[] survivors = GetSurvivorsInRange(transform.position, 2.5f);
        for (int i = 0; i < survivors.Length; i++)
        {
            survivors[i].TakeDamage(baseMeleeDamage, gameObject);
        }

        AutoSentryTurret sentry = FindClosestOwnedSentry();
        if (sentry != null)
        {
            sentry.Repair(10);
        }
    }

    private void CastSwarmOverlordDeploySentryTurret()
    {
        if (sentryTurretPrefab == null)
        {
            return;
        }

        Vector3 spawnPos = transform.position + transform.forward * 2f;
        GameObject turretObj = Instantiate(sentryTurretPrefab, spawnPos, Quaternion.identity);
        AutoSentryTurret turret = turretObj.GetComponent<AutoSentryTurret>();
        if (turret == null)
        {
            turret = turretObj.AddComponent<AutoSentryTurret>();
        }
        turret.Initialize(this, targetLayers, survivorTag);
    }

    private void CastSwarmOverlordCallMinionSwarm()
    {
        if (minionPrefab == null)
        {
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            Vector3 offset = Quaternion.Euler(0f, -20f + (i * 20f), 0f) * transform.forward * 1.5f;
            GameObject minionObj = Instantiate(minionPrefab, transform.position + offset, Quaternion.identity);
            TrackingMinion minion = minionObj.GetComponent<TrackingMinion>();
            if (minion == null)
            {
                minion = minionObj.AddComponent<TrackingMinion>();
            }
            minion.Initialize(targetLayers, survivorTag);
        }
    }

    private void CastSwarmOverlordMoltenCore()
    {
        for (int i = 0; i < 3; i++)
        {
            Vector3 pos = transform.position + transform.forward * (2f + i * 1.5f);
            if (moltenHazardPrefab != null)
            {
                Instantiate(moltenHazardPrefab, pos, Quaternion.identity);
            }
            else
            {
                GameObject hazard = new GameObject("MoltenCoreHazard");
                hazard.transform.position = pos;
                PersistentGroundHazard h = hazard.AddComponent<PersistentGroundHazard>();
                h.Configure(targetLayers, survivorTag, 5f, 2.2f, 3);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SubterraneanStalker
    // ──────────────────────────────────────────────────────────────────────────

    private void CastSubterraneanDrillGauntlets()
    {
        UnitHealth target = FindClosestSurvivor(transform.position, 2.2f);
        if (target != null)
        {
            target.TakeDamage(12, gameObject);
        }
    }

    private void CastSubterraneanBurrowUnderground()
    {
        isBurrowed = !isBurrowed;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = !isBurrowed;
        }

        if (characterController != null)
        {
            characterController.detectCollisions = !isBurrowed;
        }

        if (monsterMovement != null)
        {
            monsterMovement.moveSpeed *= isBurrowed ? burrowMoveBonusMultiplier : 1f / burrowMoveBonusMultiplier;
        }
    }

    private IEnumerator CastSubterraneanEruptionDashCoroutine()
    {
        if (!isBurrowed)
        {
            yield break;
        }

        CastSubterraneanBurrowUnderground(); // unburrow
        Vector3 dir = transform.forward;
        float duration = 0.22f;
        float speed = 10f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            TryMoveSelf(dir * speed * Time.deltaTime);
            yield return null;
        }

        UnitHealth[] victims = GetSurvivorsInRange(transform.position, 2.4f);
        for (int i = 0; i < victims.Length; i++)
        {
            victims[i].TakeDamage(10, gameObject);
            LaunchUp(victims[i], 2.2f);
        }
    }

    private void CastSubterraneanTectonicFault()
    {
        StartCoroutine(TectonicFaultCoroutine());
    }

    private IEnumerator TectonicFaultCoroutine()
    {
        Vector3 start = transform.position;
        Vector3 dir = transform.forward;
        float maxDistance = 12f;
        float step = 1.2f;

        for (float d = 1f; d <= maxDistance; d += step)
        {
            Vector3 p = start + dir * d;
            if (tremorShockwavePrefab != null)
            {
                GameObject wave = Instantiate(tremorShockwavePrefab, p, Quaternion.LookRotation(dir));
                Destroy(wave, 0.35f);
            }

            UnitHealth[] hits = GetSurvivorsInRange(p, 1.2f);
            for (int i = 0; i < hits.Length; i++)
            {
                StartCoroutine(ApplySurvivorControlLock(hits[i], 1.0f));
                hits[i].TakeDamage(8, gameObject);
            }
            yield return new WaitForSeconds(0.03f);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DoomShieldColossus
    // ──────────────────────────────────────────────────────────────────────────

    private void CastDoomShieldGauntletPummel()
    {
        UnitHealth[] hits = GetSurvivorsInRange(transform.position + transform.forward * 1.1f, 3f);
        int hitCount = 0;
        for (int i = 0; i < hits.Length; i++)
        {
            hits[i].TakeDamage(11, gameObject);
            hitCount++;
        }

        if (hitCount > 0 && unitHealth != null)
        {
            unitHealth.currentHealth = Mathf.Min(unitHealth.maxHealth, unitHealth.currentHealth + (hitCount * 6));
        }
    }

    private void CastDoomShieldKineticGrasp()
    {
        StartCoroutine(KineticGraspCoroutine());
    }

    private IEnumerator KineticGraspCoroutine()
    {
        float endTime = Time.time + 1.3f;
        while (Time.time < endTime)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 2.2f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                SpiritBoltProjectile bolt = hits[i].GetComponentInParent<SpiritBoltProjectile>();
                if (bolt != null)
                {
                    Vector3 toProj = bolt.transform.position - transform.position;
                    if (Vector3.Angle(transform.forward, toProj) <= 70f)
                    {
                        if (unitHealth != null)
                        {
                            unitHealth.currentHealth = Mathf.Min(unitHealth.maxHealth + 40, unitHealth.currentHealth + 8);
                        }
                        Destroy(bolt.gameObject);
                    }
                }
            }
            yield return null;
        }
    }

    private IEnumerator CastDoomShieldCarnageOverrunCoroutine()
    {
        Vector3 dir = transform.forward;
        float duration = 0.7f;
        float speed = 9f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            TryMoveSelf(dir * speed * Time.deltaTime);
            yield return null;
        }

        UnitHealth[] victims = GetSurvivorsInRange(transform.position, 3f);
        for (int i = 0; i < victims.Length; i++)
        {
            StartCoroutine(ApplyBleed(victims[i], 3f, 2));
            victims[i].TakeDamage(10, gameObject);
        }
    }

    private void CastDoomShieldCageMatch()
    {
        if (ringBarrierPrefab != null)
        {
            Instantiate(ringBarrierPrefab, transform.position, Quaternion.identity);
            return;
        }

        // Compile-safe fallback.
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "CageMatchRing";
        ring.transform.position = transform.position;
        ring.transform.localScale = new Vector3(8f, 1f, 8f);
        Destroy(ring, 5f);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Juggernaut
    // ──────────────────────────────────────────────────────────────────────────

    private void CastJuggernautHandCannon()
    {
        const int pellets = 8;
        for (int i = 0; i < pellets; i++)
        {
            Vector3 dir = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(-12f, 12f), 0f) * transform.forward;
            if (Physics.Raycast(GetProjectileSpawnPosition(), dir, out RaycastHit hit, 6f, targetLayers, QueryTriggerInteraction.Ignore))
            {
                UnitHealth h = hit.collider.GetComponentInParent<UnitHealth>();
                if (h != null && h.CompareTag(survivorTag) && !h.IsDead)
                {
                    h.TakeDamage(4, gameObject);
                }
            }
        }
    }

    private IEnumerator CastJuggernautRocketPunchCoroutine()
    {
        Vector3 dir = transform.forward;
        float duration = 0.25f;
        float speed = 13f;
        float elapsed = 0f;
        UnitHealth pinned = null;
        bool slammedOnWall = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            CollisionFlags flags = TryMoveSelf(dir * speed * Time.deltaTime);
            if ((flags & CollisionFlags.Sides) != 0)
            {
                slammedOnWall = true;
            }

            if (pinned == null)
            {
                pinned = FindClosestSurvivor(transform.position, 1.3f);
            }
            yield return null;
        }

        if (pinned != null)
        {
            pinned.TakeDamage(14, gameObject);
            if (slammedOnWall)
            {
                StartCoroutine(ApplySurvivorControlLock(pinned, 1.2f));
            }
        }
    }

    private IEnumerator CastJuggernautSeismicSlamCoroutine()
    {
        Vector3 start = transform.position;
        Vector3 peak = start + Vector3.up * 3f + transform.forward * 2f;
        float upDuration = 0.2f;
        float downDuration = 0.2f;
        float t = 0f;
        while (t < upDuration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, peak, t / upDuration);
            yield return null;
        }

        t = 0f;
        while (t < downDuration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(peak, start + transform.forward * 2f, t / downDuration);
            yield return null;
        }

        Vector3 center = transform.position;
        UnitHealth[] hits = GetSurvivorsInRange(center, 4f);
        for (int i = 0; i < hits.Length; i++)
        {
            Vector3 pullDir = (center - hits[i].transform.position);
            pullDir.y = 0f;
            ApplyKnockback(hits[i], pullDir.normalized, 1.2f);
            hits[i].TakeDamage(9, gameObject);
        }
    }

    private IEnumerator CastJuggernautMeteorStrikeCoroutine()
    {
        Vector3 start = transform.position;
        Vector3 sky = start + Vector3.up * 12f;
        float rise = 0.35f;
        float fall = 0.35f;
        bool prevCollisions = characterController.detectCollisions;
        characterController.detectCollisions = false;

        float t = 0f;
        while (t < rise)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, sky, t / rise);
            yield return null;
        }

        t = 0f;
        while (t < fall)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(sky, start, t / fall);
            yield return null;
        }

        characterController.detectCollisions = prevCollisions;
        UnitHealth[] hits = GetSurvivorsInRange(transform.position, 5f);
        for (int i = 0; i < hits.Length; i++)
        {
            hits[i].TakeDamage(18, gameObject);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CyberNinja
    // ──────────────────────────────────────────────────────────────────────────

    private void CastCyberNinjaShurikenFan()
    {
        for (int i = -1; i <= 1; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, i * 8f, 0f) * transform.forward;
            if (shurikenProjectilePrefab != null)
            {
                Instantiate(shurikenProjectilePrefab, GetProjectileSpawnPosition(), Quaternion.LookRotation(dir));
            }
            else
            {
                if (Physics.Raycast(GetProjectileSpawnPosition(), dir, out RaycastHit hit, 12f, targetLayers, QueryTriggerInteraction.Ignore))
                {
                    UnitHealth h = hit.collider.GetComponentInParent<UnitHealth>();
                    if (h != null && h.CompareTag(survivorTag) && !h.IsDead)
                    {
                        h.TakeDamage(6, gameObject);
                    }
                }
            }
        }
    }

    private IEnumerator CastCyberNinjaSwiftStrikeCoroutine()
    {
        if (Time.time < nextCyberSwiftStrikeTime)
        {
            yield break;
        }

        nextCyberSwiftStrikeTime = Time.time + cyberSwiftStrikeCooldown;
        Vector3 dir = transform.forward;
        float duration = 0.18f;
        float speed = 14f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            TryMoveSelf(dir * speed * Time.deltaTime);
            yield return null;
        }

        UnitHealth hit = FindClosestSurvivor(transform.position, 1.8f);
        if (hit != null)
        {
            int before = hit.currentHealth;
            hit.TakeDamage(12, gameObject);
            if (before > 0 && hit.IsDead)
            {
                nextCyberSwiftStrikeTime = Time.time; // reset on kill
            }
        }
    }

    private IEnumerator CastCyberNinjaDeflectCoroutine()
    {
        isDeflecting = true;
        float end = Time.time + deflectDuration;
        while (Time.time < end)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 1.2f, 2f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                SpiritBoltProjectile bolt = hits[i].GetComponentInParent<SpiritBoltProjectile>();
                if (bolt != null)
                {
                    Destroy(bolt.gameObject);
                    // reflected damage ping in front
                    if (Physics.Raycast(transform.position, transform.forward, out RaycastHit rh, 8f, targetLayers, QueryTriggerInteraction.Ignore))
                    {
                        UnitHealth h = rh.collider.GetComponentInParent<UnitHealth>();
                        if (h != null && h.CompareTag(survivorTag))
                        {
                            h.TakeDamage(8, gameObject);
                        }
                    }
                }
            }
            yield return null;
        }
        isDeflecting = false;
    }

    private void CastCyberNinjaDragonblade()
    {
        dragonbladeEndTime = Time.time + dragonbladeDuration;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Vanguard
    // ──────────────────────────────────────────────────────────────────────────

    private void CastVanguardRocketHammer()
    {
        UnitHealth[] hits = GetSurvivorsInRange(transform.position + transform.forward * 1.3f, 3f);
        int dmg = dragonbladeEndTime > Time.time ? Mathf.RoundToInt(baseMeleeDamage * dragonbladePrimaryMultiplier) : baseMeleeDamage + 3;
        for (int i = 0; i < hits.Length; i++)
        {
            hits[i].TakeDamage(dmg, gameObject);
        }
    }

    private IEnumerator CastVanguardChargeCoroutine()
    {
        Vector3 dir = transform.forward;
        float duration = 0.55f;
        float speed = 11f;
        float elapsed = 0f;
        UnitHealth pinned = null;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            TryMoveSelf(dir * speed * Time.deltaTime);
            if (pinned == null)
            {
                pinned = FindClosestSurvivor(transform.position, 1.1f);
            }
            yield return null;
        }

        if (pinned != null)
        {
            pinned.TakeDamage(13, gameObject);
            StartCoroutine(ApplySurvivorControlLock(pinned, 1f));
            Vector3 feet = transform.position + transform.forward * 0.9f;
            MoveUnitToPosition(pinned, feet);
        }
    }

    private void CastVanguardFireStrike()
    {
        Vector3 dir = transform.forward;
        if (fireStrikeProjectilePrefab != null)
        {
            Instantiate(fireStrikeProjectilePrefab, GetProjectileSpawnPosition(), Quaternion.LookRotation(dir));
            return;
        }

        // Piercing fallback: hit all survivors in line.
        UnitHealth[] all = GetSurvivorsInRange(transform.position + dir * 6f, 7f);
        for (int i = 0; i < all.Length; i++)
        {
            Vector3 to = all[i].transform.position - transform.position;
            float angle = Vector3.Angle(dir, to.normalized);
            if (angle <= 12f)
            {
                all[i].TakeDamage(9, gameObject);
            }
        }
    }

    private void CastVanguardEarthshatter()
    {
        UnitHealth[] all = GetSurvivorsInRange(transform.position, 8f);
        for (int i = 0; i < all.Length; i++)
        {
            Vector3 to = all[i].transform.position - transform.position;
            float angle = Vector3.Angle(transform.forward, to.normalized);
            if (angle <= 40f)
            {
                all[i].TakeDamage(10, gameObject);
                StartCoroutine(ApplySurvivorControlLock(all[i], 1.4f));
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // RelentlessHook
    // ──────────────────────────────────────────────────────────────────────────

    private void CastRelentlessHookScrapGun()
    {
        float range = Mathf.Max(0.5f, sprayRange);
        float halfAngle = Mathf.Max(1f, sprayHalfAngle);
        int damage = Mathf.Max(1, sprayDamage);
        Vector3 origin = GetChestCastOrigin();
        Vector3 forward = GetFlatForward(logAim: true);

        PlayRelentlessSfx(spraySound, AbilityPlaceholderSound.Spray, Random.Range(0.94f, 1.06f));
        PlayRelentlessCastSound(1);

        LogPredatorAbility("Spray direction=" + forward);
        LogPredatorAbility("Spray cast origin=" + origin + " forward=" + forward
            + " range=" + range.ToString("0.0") + " angle=" + halfAngle.ToString("0.0"));

        UnitHealth[] candidates = GetSurvivorsInRange(transform.position, range + 1f);
        List<UnitHealth> hits = new List<UnitHealth>();

        for (int i = 0; i < candidates.Length; i++)
        {
            UnitHealth candidate = candidates[i];
            float distance;
            float angleToTarget;
            string reason;
            bool valid = TryEvaluateSprayTarget(
                candidate,
                origin,
                forward,
                range,
                halfAngle,
                out distance,
                out angleToTarget,
                out reason);

            if (showDebugLogs)
            {
                string tag = candidate != null ? candidate.tag : "none";
                bool hasHealth = candidate != null;
                bool alive = candidate != null && !candidate.IsDead;
                Debug.Log("[PredatorAbility] Spray found candidate "
                    + (candidate != null ? candidate.name : "null")
                    + " dist=" + distance.ToString("0.00")
                    + " angle=" + angleToTarget.ToString("0.0")
                    + " hasUnitHealth=" + (hasHealth ? "yes" : "no")
                    + " tag=" + tag
                    + " alive=" + (alive ? "yes" : "no")
                    + " valid=" + (valid ? "yes" : "no")
                    + " reason=" + reason);
            }

            if (!valid && showDebugLogs && distance <= range + 1f
                && (reason == "outside-cone" || reason == "out-of-range"))
            {
                Debug.Log("[PredatorAbility] Spray candidate miss: distance "
                    + distance.ToString("0.00") + " angle " + angleToTarget.ToString("0.0")
                    + " reason " + reason);
            }

            if (valid)
            {
                hits.Add(candidate);
            }
        }

        for (int i = 0; i < hits.Count; i++)
        {
            UnitHealth target = hits[i];
            int oldHp = target.currentHealth;
            target.TakeDamage(damage, gameObject);
            int newHp = target.currentHealth;

            Vector3 knockDir = target.transform.position - transform.position;
            knockDir.y = 0f;
            if (knockDir.sqrMagnitude <= 0.001f)
            {
                knockDir = forward;
            }

            ApplyKnockback(target, knockDir.normalized, sprayKnockback);
            SpawnHitMarkerVfx(target.transform.position + Vector3.up * 1f, 0.35f);

            LogPredatorAbility("Spray damaged " + target.name + " for " + damage
                + " " + oldHp + " -> " + newHp);
            LogPredatorAbility("Spray knockback " + target.name);
        }

        if (showDebugLogs)
        {
            Debug.DrawRay(origin, forward * range, Color.red, 1.2f);
            Debug.DrawRay(origin, Quaternion.Euler(0f, halfAngle, 0f) * forward * range, Color.red, 1.2f);
            Debug.DrawRay(origin, Quaternion.Euler(0f, -halfAngle, 0f) * forward * range, Color.red, 1.2f);
        }

        QueueSprayPresentationVfx(origin, forward, range, halfAngle);

        LogPredatorAbility("Spray hit " + hits.Count + " survivors");
    }

    private void QueueSprayPresentationVfx(Vector3 origin, Vector3 forward, float range, float halfAngle)
    {
        if (sprayCastWindup <= 0f)
        {
            SpawnSprayConeVfx(origin, forward, range, halfAngle);
            if (enableAbilityFeel)
            {
                PredatorAbilityFeelVfx.SpawnSprayPellets(origin, forward, range, halfAngle, 7, 0.22f);
            }

            return;
        }

        StartCoroutine(DelayedSprayPresentationVfx(origin, forward, range, halfAngle, sprayCastWindup));
    }

    private IEnumerator DelayedSprayPresentationVfx(
        Vector3 origin,
        Vector3 forward,
        float range,
        float halfAngle,
        float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));
        SpawnSprayConeVfx(origin, forward, range, halfAngle);
        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnSprayPellets(origin, forward, range, halfAngle, 7, 0.22f);
        }
    }

    private bool TryEvaluateSprayTarget(
        UnitHealth candidate,
        Vector3 origin,
        Vector3 forward,
        float range,
        float halfAngle,
        out float distance,
        out float angleToTarget,
        out string reason,
        bool allowCloseRangeForgiveness = true)
    {
        distance = 0f;
        angleToTarget = 0f;
        reason = "none";

        if (candidate == null)
        {
            reason = "null-target";
            return false;
        }

        if (candidate.IsDead)
        {
            reason = "dead";
            return false;
        }

        if (candidate.gameObject == gameObject || IsPredatorOwned(candidate.gameObject))
        {
            reason = "predator-self";
            return false;
        }

        if (!candidate.CompareTag(survivorTag))
        {
            reason = "wrong-tag";
            return false;
        }

        Vector3 toTarget = candidate.transform.position - origin;
        toTarget.y = 0f;
        distance = toTarget.magnitude;

        if (distance <= 0.05f)
        {
            reason = "valid-point-blank";
            return true;
        }

        if (distance > range)
        {
            reason = "out-of-range";
            return false;
        }

        angleToTarget = Vector3.Angle(forward, toTarget.normalized);

        if (allowCloseRangeForgiveness
            && distance <= sprayCloseRangeForgiveness
            && angleToTarget <= sprayCloseRangeMaxAngle)
        {
            reason = "valid-close-forgiveness";
            return true;
        }

        if (angleToTarget <= halfAngle)
        {
            reason = "valid-cone";
            return true;
        }

        reason = "outside-cone";
        return false;
    }

    private void SpawnSprayConeVfx(Vector3 origin, Vector3 forward, float range, float halfAngle)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.001f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        forward.Normalize();
        int segments = Mathf.Max(4, sprayVfxSegments);
        float lifetime = Mathf.Max(0.1f, sprayVfxLifetime);
        float forwardOffset = Mathf.Max(0.2f, sprayVfxForwardOffset);
        float verticalOffset = Mathf.Clamp(sprayVfxVerticalOffset, 0.05f, 0.2f);

        Vector3 groundAnchor = new Vector3(transform.position.x, transform.position.y + verticalOffset, transform.position.z);
        Vector3 vfxPosition = groundAnchor + forward * forwardOffset;

        GameObject vfx = new GameObject("SprayConeVFX");
        vfx.transform.position = vfxPosition;
        vfx.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        MeshFilter meshFilter = vfx.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = BuildSprayFanMesh(range, halfAngle, segments);

        MeshRenderer renderer = vfx.AddComponent<MeshRenderer>();
        Color tint = new Color(0.95f, 0.35f, 0.1f, sprayVfxAlpha);
        renderer.sharedMaterial = CreateSprayFanMaterial(tint);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        Destroy(vfx, lifetime);
    }

    private static Material CreateSprayFanMaterial(Color tint)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader != null ? shader : Shader.Find("Hidden/InternalErrorShader"));
        string shaderName = material.shader != null ? material.shader.name : string.Empty;

        if (shaderName.Contains("Universal Render Pipeline/Unlit"))
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 2f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tint);
            }
        }
        else if (shaderName.Contains("Sprites/Default"))
        {
            material.color = tint;
            material.renderQueue = 3000;
        }
        else
        {
            material.color = tint;
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = 3000;
        }

        return material;
    }

    private static Mesh BuildSprayFanMesh(float range, float halfAngleDegrees, int segments)
    {
        range = Mathf.Max(0.5f, range);
        halfAngleDegrees = Mathf.Max(1f, halfAngleDegrees);
        segments = Mathf.Max(3, segments);

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];
        vertices[0] = Vector3.zero;

        float startAngle = -halfAngleDegrees;
        float step = (halfAngleDegrees * 2f) / segments;
        for (int i = 0; i <= segments; i++)
        {
            float radians = (startAngle + step * i) * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(Mathf.Sin(radians) * range, 0f, Mathf.Cos(radians) * range);
        }

        for (int i = 0; i < segments; i++)
        {
            int tri = i * 3;
            triangles[tri] = 0;
            triangles[tri + 1] = i + 1;
            triangles[tri + 2] = i + 2;
        }

        Mesh mesh = new Mesh { name = "SprayFanMesh" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private IEnumerator CastRelentlessChainHookCoroutine()
    {
        PlayRelentlessCastSound(2);
        if (hookCastWindup > 0f)
        {
            yield return new WaitForSeconds(hookCastWindup);
        }

        float range = Mathf.Max(1f, hookRange);
        float pullForwardDistance = Mathf.Max(1f, hookPullForwardDistance);
        float pullDuration = Mathf.Max(0.05f, hookPullDuration);

        Vector3 origin = GetChestCastOrigin();
        Vector3 dir = GetFlatForward(logAim: true);
        string blockReason;
        UnitHealth target = ResolveHookTarget(origin, dir, range, out blockReason);

        PlayRelentlessSfx(hookFireSound, AbilityPlaceholderSound.HookFire);

        if (hookProjectilePrefab != null)
        {
            GameObject hook = Instantiate(hookProjectilePrefab, origin, Quaternion.LookRotation(dir));
            Destroy(hook, 0.6f);
        }

        Vector3 chainEnd = target != null
            ? target.transform.position + Vector3.up * 1f
            : origin + dir * range;
        ClearActiveHookChain();
        activeHookChain = PredatorAbilityFeelVfx.SpawnHookChainLine(
            origin,
            chainEnd,
            target != null ? new Color(0.95f, 0.75f, 0.2f, 0.95f) : new Color(0.75f, 0.75f, 0.75f, 0.65f),
            0.1f);

        if (target == null)
        {
            yield return new WaitForSeconds(0.45f);
            ClearActiveHookChain();
            yield break;
        }

        int damage = Mathf.Max(0, hookDamage);
        if (damage > 0)
        {
            int oldHp = target.currentHealth;
            target.TakeDamage(damage, gameObject);
            LogPredatorAbility("Hook damaged " + target.name + " for " + damage
                + " " + oldHp + " -> " + target.currentHealth);
        }

        PlayRelentlessSfx(hookHitSound, AbilityPlaceholderSound.HookHit);
        SpawnHitMarkerVfx(target.transform.position + Vector3.up * 1.2f, 0.4f);

        Vector3 start = target.transform.position;
        Vector3 end = GetHookPullDestination(start, pullForwardDistance);
        Debug.Log("[PredatorAbility] Hook pull start: " + start);
        Debug.Log("[PredatorAbility] Hook pull destination: " + end);

        LineRenderer hookLine = activeHookChain != null ? activeHookChain.GetComponent<LineRenderer>() : null;
        yield return PullSurvivorWithControlSuspend(target, end, pullDuration, hookLine);
        ClearActiveHookChain();

        if (target != null && !target.IsDead)
        {
            Debug.Log("[PredatorAbility] Hook pull complete: " + target.transform.position);
        }
    }

    private IEnumerator CastRelentlessTakeABreatherCoroutine()
    {
        if (unitHealth == null || unitHealth.IsDead)
        {
            yield break;
        }

        PlayRelentlessCastSound(3);
        if (tonicCastWindup > 0f)
        {
            yield return new WaitForSeconds(tonicCastWindup);
        }

        PlayRelentlessSfx(tonicSound, AbilityPlaceholderSound.Tonic);

        int before = unitHealth.currentHealth;
        if (before < unitHealth.maxHealth)
        {
            unitHealth.Heal(tonicHealAmount);
        }

        int after = unitHealth.currentHealth;
        LogPredatorAbility("Tonic healed " + (after - before));

        ApplyTonicEffects();
        SpawnSelfBurstVfx(new Color(0.35f, 0.95f, 0.45f, 0.75f), 0.55f);
        if (enableAbilityFeel)
        {
            PredatorAbilityFeelVfx.SpawnTonicChannelRing(transform, tonicGasRadius, tonicDuration);
        }

        yield break;
    }

    private void ApplyTonicEffects()
    {
        StopTonicEffects("refresh");

        if (monsterMovement != null)
        {
            monsterMovement.SetAbilitySpeedMultiplier(tonicSelfSpeedMultiplier);
        }

        SpawnTonicGasVfx();
        tonicEffectRoutine = StartCoroutine(TonicEffectRoutine());
        LogPredatorAbility("Tonic slow started x" + tonicSelfSpeedMultiplier.ToString("0.00")
            + " for " + tonicDuration.ToString("0.0") + "s");
    }

    private IEnumerator TonicEffectRoutine()
    {
        float endTime = Time.time + Mathf.Max(0.1f, tonicDuration);
        tonicGasPendingDamage.Clear();
        tonicGasPendingPropDamage.Clear();

        while (Time.time < endTime)
        {
            if (!isActiveAndEnabled || unitHealth == null || unitHealth.IsDead || !IsRoundActive())
            {
                StopTonicEffects("interrupted");
                yield break;
            }

            TickTonicGas(Time.deltaTime);
            yield return null;
        }

        StopTonicEffects("duration-ended");
    }

    private void TickTonicGas(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        float radius = Mathf.Max(0.1f, tonicGasRadius);
        TickTonicSurvivorGas(radius, deltaTime);
        TickTonicPropGas(radius, deltaTime);
    }

    private void TickTonicSurvivorGas(float radius, float deltaTime)
    {
        float dps = Mathf.Max(0f, tonicGasDamagePerSecond);

        UnitHealth[] candidates = GetSurvivorsInRange(transform.position, radius + 0.5f);
        HashSet<UnitHealth> insideRadius = new HashSet<UnitHealth>();

        for (int i = 0; i < candidates.Length; i++)
        {
            UnitHealth survivor = candidates[i];
            if (survivor == null || survivor.IsDead || survivor.gameObject == gameObject
                || IsPredatorOwned(survivor.gameObject) || !survivor.CompareTag(survivorTag))
            {
                continue;
            }

            Vector3 offset = survivor.transform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > radius * radius)
            {
                continue;
            }

            insideRadius.Add(survivor);
            ApplyTonicGasSlowToSurvivor(survivor);

            if (dps <= 0f)
            {
                continue;
            }

            float pending = 0f;
            tonicGasPendingDamage.TryGetValue(survivor, out pending);
            pending += dps * deltaTime;

            while (pending >= 1f)
            {
                int damage = Mathf.FloorToInt(pending);
                pending -= damage;
                survivor.TakeDamage(damage, gameObject);
                LogPredatorAbility("Tonic gas damaged " + survivor.name + " for " + damage);
            }

            tonicGasPendingDamage[survivor] = pending;
        }

        ClearStalePendingDamage(insideRadius);
    }

    private void ApplyTonicGasSlowToSurvivor(UnitHealth survivor)
    {
        if (survivor == null || survivor.IsDead || tonicGasSlowDuration <= 0f)
        {
            return;
        }

        SurvivorMovement movement = survivor.GetComponent<SurvivorMovement>();
        if (movement != null)
        {
            movement.ApplyTemporarySpeedMultiplier(tonicGasSlowMultiplier, tonicGasSlowDuration);
        }

        OfflineSurvivorBotAI bot = survivor.GetComponent<OfflineSurvivorBotAI>();
        if (bot != null)
        {
            bot.ApplyTemporarySpeedMultiplier(tonicGasSlowMultiplier, tonicGasSlowDuration);
        }
    }

    private void TickTonicPropGas(float radius, float deltaTime)
    {
        float propDps = Mathf.Max(0f, tonicGasPropDamagePerSecond);
        if (propDps <= 0f)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Ignore);
        HashSet<DestructiblePropHealth> insideRadius = new HashSet<DestructiblePropHealth>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            DestructiblePropHealth prop = hit.GetComponentInParent<DestructiblePropHealth>();
            if (prop == null || !prop.IsAlive)
            {
                continue;
            }

            Vector3 propPoint = prop.transform.position;
            Vector3 offset = propPoint - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > radius * radius)
            {
                continue;
            }

            insideRadius.Add(prop);

            float pending = 0f;
            tonicGasPendingPropDamage.TryGetValue(prop, out pending);
            pending += propDps * deltaTime;

            while (pending >= 1f)
            {
                int damage = Mathf.FloorToInt(pending);
                pending -= damage;
                prop.TakeDamage(damage, gameObject);
                LogPredatorAbility("Tonic gas damaged prop " + prop.name + " for " + damage);
            }

            tonicGasPendingPropDamage[prop] = pending;
        }

        if (tonicGasPendingPropDamage.Count == 0)
        {
            return;
        }

        List<DestructiblePropHealth> staleProps = null;
        foreach (KeyValuePair<DestructiblePropHealth, float> entry in tonicGasPendingPropDamage)
        {
            if (entry.Key == null || !entry.Key.IsAlive || !insideRadius.Contains(entry.Key))
            {
                if (staleProps == null)
                {
                    staleProps = new List<DestructiblePropHealth>();
                }

                staleProps.Add(entry.Key);
            }
        }

        if (staleProps == null)
        {
            return;
        }

        for (int i = 0; i < staleProps.Count; i++)
        {
            tonicGasPendingPropDamage.Remove(staleProps[i]);
        }
    }

    private void ClearStalePendingDamage(HashSet<UnitHealth> insideRadius)
    {
        if (tonicGasPendingDamage.Count == 0)
        {
            return;
        }

        List<UnitHealth> staleKeys = null;
        foreach (KeyValuePair<UnitHealth, float> entry in tonicGasPendingDamage)
        {
            if (entry.Key == null || entry.Key.IsDead || !insideRadius.Contains(entry.Key))
            {
                if (staleKeys == null)
                {
                    staleKeys = new List<UnitHealth>();
                }

                staleKeys.Add(entry.Key);
            }
        }

        if (staleKeys == null)
        {
            return;
        }

        for (int i = 0; i < staleKeys.Count; i++)
        {
            tonicGasPendingDamage.Remove(staleKeys[i]);
        }
    }

    private void StopTonicEffects(string reason)
    {
        if (tonicEffectRoutine != null)
        {
            StopCoroutine(tonicEffectRoutine);
            tonicEffectRoutine = null;
        }

        if (monsterMovement != null)
        {
            monsterMovement.ClearAbilitySpeedMultiplier();
        }

        if (tonicGasVfx != null)
        {
            Destroy(tonicGasVfx);
            tonicGasVfx = null;
        }

        tonicGasPendingDamage.Clear();
        tonicGasPendingPropDamage.Clear();

        if (showDebugLogs && reason != "refresh")
        {
            LogPredatorAbility("Tonic ended, movement restored (" + reason + ")");
        }
    }

    private void SpawnTonicGasVfx()
    {
        if (tonicGasVfx != null)
        {
            Destroy(tonicGasVfx);
        }

        float radius = Mathf.Max(0.5f, tonicGasRadius);
        GameObject vfx = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        vfx.name = "TonicGasVFX";
        Collider col = vfx.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        vfx.transform.SetParent(transform, false);
        vfx.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        vfx.transform.localScale = new Vector3(radius * 2f, 0.12f, radius * 2f);
        ApplySimpleVfxColor(vfx, new Color(0.18f, 0.82f, 0.28f, 0.48f));
        tonicGasVfx = vfx;
    }

    private IEnumerator CastRelentlessBarrageCoroutine()
    {
        isBarrageActive = true;
        LogPredatorAbility("Barrage started");

        PlayRelentlessCastSound(4);
        if (barrageCastWindup > 0f)
        {
            yield return new WaitForSeconds(barrageCastWindup);
        }

        Vector3 origin = GetChestCastOrigin();
        Vector3 forward = GetFlatForward(logAim: true);
        PlayRelentlessSfx(barrageStartSound, AbilityPlaceholderSound.BarrageStart);

        LogPredatorAbility("Barrage warning started");
        SpawnBarrageConeVfx(
            forward,
            barrageRange,
            barrageHalfAngle,
            barrageWarningDuration,
            new Color(1f, 0.92f, 0.2f, barrageWarningAlpha));

        if (showDebugLogs)
        {
            Debug.DrawRay(origin, forward * barrageRange, Color.yellow, barrageWarningDuration + 0.5f);
            Debug.DrawRay(origin, Quaternion.Euler(0f, barrageHalfAngle, 0f) * forward * barrageRange, Color.yellow, barrageWarningDuration + 0.5f);
            Debug.DrawRay(origin, Quaternion.Euler(0f, -barrageHalfAngle, 0f) * forward * barrageRange, Color.yellow, barrageWarningDuration + 0.5f);
        }

        yield return new WaitForSeconds(Mathf.Max(0.05f, barrageWarningDuration));

        LogPredatorAbility("Barrage damage started");

        float duration = Mathf.Max(0.1f, barrageDuration);
        float pulseInterval = Mathf.Max(0.05f, barragePulseInterval);
        float end = Time.time + duration;
        int pulseNumber = 0;

        while (Time.time < end)
        {
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy || unitHealth == null || unitHealth.IsDead || !IsRoundActive())
            {
                break;
            }

            pulseNumber++;
            int hitCount = FireRelentlessBarragePulse(pulseNumber, forward);
            LogPredatorAbility("Barrage pulse " + pulseNumber + " hit " + hitCount + " targets");

            if (Time.time + pulseInterval >= end)
            {
                break;
            }

            forward = GetFlatForward(logAim: false);
            yield return new WaitForSeconds(pulseInterval);
        }

        isBarrageActive = false;
        relentlessBarrageRoutine = null;
        ClearBarragePulseVfx();
        PlayRelentlessSfx(barrageEndSound, AbilityPlaceholderSound.BarrageEnd);
        LogPredatorAbility("Barrage ended");
    }

    private int FireRelentlessBarragePulse(int pulseNumber, Vector3 forward)
    {
        Vector3 origin = GetChestCastOrigin();
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.001f)
        {
            forward = GetFlatForward(logAim: false);
        }

        forward.Normalize();
        float range = Mathf.Max(0.5f, barrageRange);
        float halfAngle = Mathf.Max(1f, barrageHalfAngle);

        SpawnBarrageConeVfx(
            forward,
            range,
            halfAngle,
            Mathf.Max(0.15f, barragePulseInterval * 0.85f),
            new Color(0.95f, 0.35f, 0.1f, barragePulseAlpha));

        PlayRelentlessSfx(barrageBombSound, AbilityPlaceholderSound.BarrageBomb, Random.Range(0.92f, 1.08f));

        UnitHealth[] candidates = GetSurvivorsInRange(transform.position, range + 1f);
        int hitCount = 0;

        for (int i = 0; i < candidates.Length; i++)
        {
            UnitHealth target = candidates[i];
            float distance;
            float angleToTarget;
            string reason;
            if (!TryEvaluateSprayTarget(
                target,
                origin,
                forward,
                range,
                halfAngle,
                out distance,
                out angleToTarget,
                out reason,
                allowCloseRangeForgiveness: false))
            {
                continue;
            }

            int oldHp = target.currentHealth;
            target.TakeDamage(barrageDamagePerPulse, gameObject);
            Vector3 knockDir = target.transform.position - transform.position;
            knockDir.y = 0f;
            if (knockDir.sqrMagnitude <= 0.001f)
            {
                knockDir = forward;
            }

            float knockDistance = Random.Range(barrageKnockbackMin, barrageKnockbackMax);
            ApplyKnockback(target, knockDir.normalized, knockDistance);
            LogPredatorAbility("Barrage pulse " + pulseNumber + " damaged " + target.name + " for " + barrageDamagePerPulse
                + " " + oldHp + " -> " + target.currentHealth);
            hitCount++;
        }

        DamageBarragePropsInCone(origin, forward, range, halfAngle, pulseNumber);

        return hitCount;
    }

    private void DamageBarragePropsInCone(Vector3 origin, Vector3 forward, float range, float halfAngle, int pulseNumber)
    {
        int damage = barragePropDamage;
        if (damage <= 0)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(origin, range, ~0, QueryTriggerInteraction.Ignore);
        HashSet<DestructiblePropHealth> damagedProps = new HashSet<DestructiblePropHealth>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            DestructiblePropHealth prop = hit.GetComponentInParent<DestructiblePropHealth>();
            if (prop == null || !prop.IsAlive || !damagedProps.Add(prop))
            {
                continue;
            }

            Vector3 toProp = prop.transform.position - origin;
            toProp.y = 0f;
            float distance = toProp.magnitude;
            if (distance <= 0.05f || distance > range)
            {
                continue;
            }

            if (Vector3.Angle(forward, toProp.normalized) > halfAngle)
            {
                continue;
            }

            prop.TakeDamage(damage, gameObject);
            LogPredatorAbility("Barrage pulse " + pulseNumber + " damaged prop " + prop.name + " for " + damage);
        }
    }

    private void SpawnBarrageConeVfx(Vector3 forward, float range, float halfAngle, float lifetime, Color tint)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.001f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        forward.Normalize();
        float forwardOffset = Mathf.Max(0.2f, barrageVfxForwardOffset);
        float verticalOffset = Mathf.Clamp(barrageVfxVerticalOffset, 0.05f, 0.2f);
        Vector3 groundAnchor = new Vector3(transform.position.x, transform.position.y + verticalOffset, transform.position.z);
        Vector3 vfxPosition = groundAnchor + forward * forwardOffset;

        GameObject vfx = new GameObject("BarrageConeVFX");
        vfx.transform.position = vfxPosition;
        vfx.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        MeshFilter meshFilter = vfx.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = BuildSprayFanMesh(range, halfAngle, Mathf.Max(4, barrageVfxSegments));

        MeshRenderer renderer = vfx.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CreateSprayFanMaterial(tint);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        barragePulseVfx.Add(vfx);
        Destroy(vfx, Mathf.Max(0.1f, lifetime));
    }

    private void SpawnBarragePulseVfx(Vector3 origin, Vector3 forward, float range, float lifetime)
    {
        GameObject vfx = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vfx.name = "BarragePulseVFX";
        Collider col = vfx.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        forward.y = 0f;
        forward.Normalize();
        vfx.transform.position = origin + forward * (range * 0.5f) + Vector3.up * 0.45f;
        vfx.transform.rotation = Quaternion.LookRotation(forward);
        vfx.transform.localScale = new Vector3(2.8f, 0.65f, range);
        ApplySimpleVfxColor(vfx, new Color(1f, 0.35f, 0.08f, 0.8f));
        barragePulseVfx.Add(vfx);
        Destroy(vfx, Mathf.Max(0.1f, lifetime));
    }

    private void ClearBarragePulseVfx()
    {
        for (int i = barragePulseVfx.Count - 1; i >= 0; i--)
        {
            if (barragePulseVfx[i] != null)
            {
                Destroy(barragePulseVfx[i]);
            }
        }

        barragePulseVfx.Clear();
    }

    private void LogPredatorAbility(string message)
    {
        if (!showDebugLogs)
        {
            return;
        }

        Debug.Log("[PredatorAbility] " + message);
    }

    private void PlayRelentlessSfx(AudioClip clip, AbilityPlaceholderSound fallback, float pitch = 1f)
    {
        if (!enableAbilityFeel)
        {
            return;
        }

        if (abilityAudioSource == null)
        {
            abilityAudioSource = GetComponent<AudioSource>();
        }

        AbilityPlaceholderAudio.Play(
            abilityAudioSource,
            clip,
            fallback,
            abilitySfxVolume,
            usePlaceholderAbilityAudio,
            pitch);
    }

    private void ClearActiveHookChain()
    {
        if (activeHookChain != null)
        {
            Destroy(activeHookChain);
            activeHookChain = null;
        }
    }

    private void UpdateHookChain(LineRenderer hookChain, UnitHealth target)
    {
        if (hookChain == null)
        {
            return;
        }

        hookChain.SetPosition(0, GetChestCastOrigin());
        Vector3 end = target != null
            ? target.transform.position + Vector3.up * 1f
            : hookChain.GetPosition(1);
        hookChain.SetPosition(1, end);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private Vector3 GetProjectileSpawnPosition()
    {
        if (projectileSpawn != null)
        {
            return projectileSpawn.position;
        }

        return transform.position + Vector3.up * 1.2f + transform.forward * 1f;
    }

    private Vector3 GetChestCastOrigin()
    {
        return transform.position + Vector3.up * 1.4f;
    }

    private Vector3 GetFlatForward(bool logAim = false)
    {
        string source;
        Vector3 forward;

        if (monsterMovement == null)
        {
            monsterMovement = GetComponent<MonsterPlayerMovement>();
        }

        if (monsterMovement != null)
        {
            forward = monsterMovement.GetGameplayAimDirection();
            source = monsterMovement.HasStoredAim
                ? "MonsterPlayerMovement.lastAimDirection"
                : "MonsterPlayerMovement.transformFallback";
        }
        else
        {
            forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.001f)
            {
                forward = Vector3.forward;
            }

            forward = forward.normalized;
            source = "transform.forward";
        }

        if (logAim && showDebugLogs)
        {
            Vector3 loggedAim = monsterMovement != null ? monsterMovement.lastAimDirection : forward;
            Debug.Log("[PredatorAim] source=" + source + " lastAimDirection=" + loggedAim);
            Debug.Log("[PredatorAim] final ability direction=" + forward);
        }

        return forward;
    }

    private bool IsRoundActive()
    {
        if (gameManager == null)
        {
            gameManager = ManiaGameManager.Instance;
        }

        return gameManager == null || gameManager.IsPlaying;
    }

    private UnitHealth ResolveHookTarget(Vector3 origin, Vector3 direction, float range, out string blockReason)
    {
        blockReason = "none";
        direction = direction.sqrMagnitude <= 0.001f ? GetFlatForward() : direction.normalized;

        Debug.Log("[PredatorAbility] Hook cast origin: " + origin);
        Debug.Log("[PredatorAbility] Hook cast forward: " + direction);
        Debug.Log("[PredatorAbility] Hook ray length: " + range.ToString("0.0"));
        Debug.DrawLine(origin, origin + direction * range, Color.cyan, 1f);

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, range, targetLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        UnitHealth rayTarget = null;
        string firstHitName = "none";
        string firstHitLayer = "none";
        string firstHitTag = "none";

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || ShouldIgnoreForHookRay(hitCollider, direction, origin))
            {
                continue;
            }

            if (firstHitName == "none")
            {
                firstHitName = hitCollider.name;
                firstHitLayer = LayerMask.LayerToName(hitCollider.gameObject.layer);
                firstHitTag = hitCollider.tag;
            }

            UnitHealth health = hitCollider.GetComponentInParent<UnitHealth>();
            if (health != null)
            {
                if (IsPredatorOwned(health.gameObject))
                {
                    continue;
                }

                bool validSurvivor = health.CompareTag(survivorTag) && !health.IsDead;
                Debug.Log("[PredatorAbility] Hook ray hit: " + hitCollider.name
                    + " layer=" + LayerMask.LayerToName(hitCollider.gameObject.layer)
                    + " tag=" + hitCollider.tag);
                Debug.Log("[PredatorAbility] Hook valid survivor target? "
                    + (validSurvivor ? "yes" : "no")
                    + ", reason=" + (validSurvivor ? "survivor in line" : "not a live survivor"));

                if (validSurvivor)
                {
                    rayTarget = health;
                }
                else
                {
                    blockReason = "blocking unit '" + health.name + "'";
                    Debug.Log("[PredatorAbility] Hook line of sight blocked by: " + health.name);
                }

                break;
            }

            blockReason = "solid collider '" + hitCollider.name + "'";
            Debug.Log("[PredatorAbility] Hook ray hit: " + hitCollider.name
                + " layer=" + LayerMask.LayerToName(hitCollider.gameObject.layer)
                + " tag=" + hitCollider.tag);
            Debug.Log("[PredatorAbility] Hook valid survivor target? no, reason=blocked by geometry");
            Debug.Log("[PredatorAbility] Hook line of sight blocked by: " + hitCollider.name);
            break;
        }

        if (rayTarget == null && firstHitName == "none")
        {
            Debug.Log("[PredatorAbility] Hook ray hit: none");
            Debug.Log("[PredatorAbility] Hook valid survivor target? no, reason=no collider hit");
        }

        if (rayTarget != null)
        {
            Debug.Log("[PredatorAbility] Hook hit " + rayTarget.name);
            return rayTarget;
        }

        UnitHealth fallback = FindClosestSurvivorInForwardArc(origin, direction, range, 20f);
        if (fallback != null)
        {
            if (HasClearHookLineOfSight(origin, fallback, out string losBlocker))
            {
                Debug.Log("[PredatorAbility] Hook valid survivor target? yes, reason=fallback arc search on " + fallback.name);
                Debug.Log("[PredatorAbility] Hook hit " + fallback.name);
                return fallback;
            }

            blockReason = "line of sight blocked by '" + losBlocker + "'";
            Debug.Log("[PredatorAbility] Hook line of sight blocked by: " + losBlocker);
        }

        Debug.Log("[PredatorAbility] Hook missed (" + blockReason + ")");
        return null;
    }

    private UnitHealth FindClosestSurvivorInForwardArc(Vector3 origin, Vector3 direction, float range, float halfAngleDegrees)
    {
        UnitHealth[] candidates = GetSurvivorsInRange(origin, range);
        UnitHealth best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < candidates.Length; i++)
        {
            UnitHealth candidate = candidates[i];
            if (candidate == null || candidate.IsDead || IsPredatorOwned(candidate.gameObject))
            {
                continue;
            }

            Vector3 toTarget = candidate.transform.position - origin;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance <= 0.05f || distance > range)
            {
                continue;
            }

            float angle = Vector3.Angle(direction, toTarget.normalized);
            if (angle > halfAngleDegrees)
            {
                continue;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best;
    }

    private bool HasClearHookLineOfSight(Vector3 origin, UnitHealth target, out string blockerName)
    {
        blockerName = "none";
        if (target == null)
        {
            return false;
        }

        Vector3 targetPoint = target.transform.position + Vector3.up * 1f;
        Vector3 delta = targetPoint - origin;
        float distance = delta.magnitude;
        if (distance <= 0.05f)
        {
            return true;
        }

        RaycastHit[] hits = Physics.RaycastAll(origin, delta.normalized, distance, targetLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || ShouldIgnoreForHookRay(hitCollider, delta.normalized, origin))
            {
                continue;
            }

            UnitHealth health = hitCollider.GetComponentInParent<UnitHealth>();
            if (health == target)
            {
                return true;
            }

            if (health != null && IsPredatorOwned(health.gameObject))
            {
                continue;
            }

            blockerName = hitCollider.name;
            return false;
        }

        return true;
    }

    private bool ShouldIgnoreForHookRay(Collider collider, Vector3 direction, Vector3 origin)
    {
        if (collider == null || collider.isTrigger)
        {
            return true;
        }

        if (IsPredatorOwned(collider.gameObject))
        {
            return true;
        }

        string objectName = collider.gameObject.name;
        if (objectName.Contains("VFX")
            || objectName.Contains("TemporaryGroundEffect")
            || objectName.Contains("SprayBurst")
            || objectName.Contains("SprayCone")
            || objectName.Contains("BarragePulse")
            || objectName.Contains("TonicGas")
            || objectName.Contains("HookHit")
            || objectName.Contains("HookMiss")
            || objectName.Contains("TonicSelf")
            || objectName.Contains("HitMarker"))
        {
            return true;
        }

        if (Mathf.Abs(direction.y) < 0.35f && IsLikelyGroundCollider(collider, origin))
        {
            return true;
        }

        return false;
    }

    private bool IsLikelyGroundCollider(Collider collider, Vector3 origin)
    {
        if (collider.bounds.max.y < origin.y - 0.25f)
        {
            return true;
        }

        string objectName = collider.gameObject.name;
        return objectName.IndexOf("Ground", System.StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("Floor", System.StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("Map", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool IsPredatorOwned(GameObject candidate)
    {
        return candidate == gameObject || candidate.transform.IsChildOf(transform);
    }

    private IEnumerator PullSurvivorWithControlSuspend(UnitHealth target, Vector3 end, float duration, LineRenderer hookChain = null)
    {
        if (target == null || target.IsDead)
        {
            yield break;
        }

        OfflineSurvivorBotAI botAi = target.GetComponent<OfflineSurvivorBotAI>();
        SurvivorMovement survivorMovement = target.GetComponent<SurvivorMovement>();
        CharacterController targetController = target.GetComponent<CharacterController>();

        bool botWasEnabled = botAi != null && botAi.enabled;
        bool moveWasEnabled = survivorMovement != null && survivorMovement.enabled;
        bool controllerWasEnabled = targetController != null && targetController.enabled;

        if (botAi != null)
        {
            botAi.enabled = false;
        }

        if (survivorMovement != null)
        {
            survivorMovement.enabled = false;
        }

        if (targetController != null)
        {
            targetController.enabled = false;
        }

        Vector3 start = target.transform.position;
        float elapsed = 0f;
        while (elapsed < duration && target != null && !target.IsDead)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, duration));
            Vector3 pullPoint = Vector3.Lerp(start, end, t);
            pullPoint.y = start.y;

            if (ArenaBounds.Instance != null)
            {
                pullPoint = ArenaBounds.Instance.ClampPosition(pullPoint);
            }

            target.transform.position = pullPoint;
            UpdateHookChain(hookChain, target);
            yield return null;
        }

        UpdateHookChain(hookChain, target);

        if (targetController != null && controllerWasEnabled && target.gameObject.activeInHierarchy)
        {
            targetController.enabled = true;
        }

        if (survivorMovement != null)
        {
            survivorMovement.enabled = moveWasEnabled;
        }

        if (botAi != null)
        {
            botAi.enabled = botWasEnabled;
        }
    }

    private static bool CanUseCharacterController(CharacterController controller, UnitHealth health)
    {
        if (controller == null || !controller.enabled || !controller.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (health != null && health.IsDead)
        {
            return false;
        }

        return true;
    }

    private CollisionFlags TryMoveSelf(Vector3 motion)
    {
        if (!CanUseCharacterController(characterController, unitHealth))
        {
            return CollisionFlags.None;
        }

        return characterController.Move(motion);
    }

    private bool TryMoveCharacterController(CharacterController controller, UnitHealth health, Vector3 motion)
    {
        if (!CanUseCharacterController(controller, health))
        {
            return false;
        }

        controller.Move(motion);
        return true;
    }

    private UnitHealth FindClosestSurvivor(Vector3 center, float radius)
    {
        UnitHealth[] all = GetSurvivorsInRange(center, radius);
        UnitHealth best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < all.Length; i++)
        {
            float sqr = (all[i].transform.position - center).sqrMagnitude;
            if (sqr < bestSqr)
            {
                best = all[i];
                bestSqr = sqr;
            }
        }
        return best;
    }

    private UnitHealth[] GetSurvivorsInRange(Vector3 center, float radius)
    {
        float safeRadius = Mathf.Max(0.1f, radius);
        float radiusSqr = safeRadius * safeRadius;
        HashSet<UnitHealth> result = new HashSet<UnitHealth>();

        UnitHealth[] allUnits = FindObjectsByType<UnitHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < allUnits.Length; i++)
        {
            UnitHealth health = allUnits[i];
            if (!IsValidSurvivorTarget(health))
            {
                continue;
            }

            Vector3 sample = health.transform.position + Vector3.up * 0.75f;
            if ((sample - center).sqrMagnitude <= radiusSqr)
            {
                result.Add(health);
            }
        }

        Collider[] hits = Physics.OverlapSphere(center, safeRadius, targetLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth health = hits[i].GetComponentInParent<UnitHealth>();
            if (IsValidSurvivorTarget(health))
            {
                result.Add(health);
            }
        }

        UnitHealth[] survivors = new UnitHealth[result.Count];
        result.CopyTo(survivors);
        return survivors;
    }

    private bool IsValidSurvivorTarget(UnitHealth health)
    {
        if (health == null || health.IsDead)
        {
            return false;
        }

        if (!health.CompareTag(survivorTag))
        {
            return false;
        }

        if (health.gameObject == gameObject || IsPredatorOwned(health.gameObject))
        {
            return false;
        }

        return true;
    }

    private UnitHealth[] GetSurvivorsInCone(Vector3 origin, Vector3 forward, float range, float halfAngleDegrees)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.001f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        forward.Normalize();
        UnitHealth[] candidates = GetSurvivorsInRange(origin + forward * (range * 0.5f), range + 1f);
        System.Collections.Generic.List<UnitHealth> result = new System.Collections.Generic.List<UnitHealth>();

        for (int i = 0; i < candidates.Length; i++)
        {
            UnitHealth candidate = candidates[i];
            if (candidate == null || candidate.IsDead || IsPredatorOwned(candidate.gameObject))
            {
                continue;
            }

            Vector3 toTarget = candidate.transform.position - origin;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance <= 0.05f || distance > range)
            {
                continue;
            }

            float angle = Vector3.Angle(forward, toTarget.normalized);
            if (angle <= halfAngleDegrees)
            {
                result.Add(candidate);
            }
        }

        return result.ToArray();
    }

    private Vector3 GetHookPullDestination(Vector3 targetStart, float pullForwardDistance)
    {
        Vector3 desired = transform.position + GetFlatForward() * pullForwardDistance;
        desired.y = targetStart.y;

        if (ArenaBounds.Instance != null)
        {
            desired = ArenaBounds.Instance.ClampPosition(desired);
        }

        Vector3 delta = desired - targetStart;
        float distance = delta.magnitude;
        if (distance <= 0.05f)
        {
            return desired;
        }

        Vector3 rayOrigin = targetStart + Vector3.up * 0.6f;
        if (Physics.Raycast(rayOrigin, delta.normalized, out RaycastHit hit, distance, targetLayers, QueryTriggerInteraction.Ignore)
            && !ShouldIgnoreForHookRay(hit.collider, delta.normalized, rayOrigin))
        {
            UnitHealth hitUnit = hit.collider.GetComponentInParent<UnitHealth>();
            if (hitUnit == null)
            {
                desired = hit.point - delta.normalized * 0.35f;
                desired.y = targetStart.y;
            }
        }

        if (ArenaBounds.Instance != null)
        {
            desired = ArenaBounds.Instance.ClampPosition(desired);
        }

        return desired;
    }

    private void SpawnForwardBurstVfx(Vector3 origin, Vector3 forward, float range, float lifetime, Color color)
    {
        GameObject vfx = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vfx.name = "SprayBurstVFX";
        Collider col = vfx.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        forward.y = 0f;
        forward.Normalize();
        vfx.transform.position = origin + forward * (range * 0.5f) + Vector3.up * 0.55f;
        vfx.transform.rotation = Quaternion.LookRotation(forward);
        vfx.transform.localScale = new Vector3(2.2f, 0.9f, range);
        ApplySimpleVfxColor(vfx, color);
        Destroy(vfx, Mathf.Max(0.1f, lifetime));
    }

    private void SpawnHookLineVfx(Vector3 origin, Vector3 direction, float range, bool hitSomething, float lifetime)
    {
        GameObject vfx = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vfx.name = hitSomething ? "HookHitVFX" : "HookMissVFX";
        Collider col = vfx.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        direction.y = 0f;
        direction.Normalize();
        vfx.transform.position = origin + direction * (range * 0.5f) + Vector3.up * 0.8f;
        vfx.transform.rotation = Quaternion.LookRotation(direction);
        vfx.transform.localScale = new Vector3(0.15f, 0.15f, range);
        ApplySimpleVfxColor(vfx, hitSomething ? new Color(0.95f, 0.75f, 0.2f, 0.85f) : new Color(0.7f, 0.7f, 0.7f, 0.55f));
        Destroy(vfx, Mathf.Max(0.1f, lifetime));
    }

    private void SpawnHitMarkerVfx(Vector3 position, float lifetime)
    {
        GameObject vfx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        vfx.name = "HitMarkerVFX";
        Collider col = vfx.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        vfx.transform.position = position;
        vfx.transform.localScale = Vector3.one * 0.45f;
        ApplySimpleVfxColor(vfx, new Color(1f, 0.35f, 0.35f, 0.85f));
        Destroy(vfx, Mathf.Max(0.1f, lifetime));
    }

    private void SpawnSelfBurstVfx(Color color, float lifetime)
    {
        GameObject vfx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        vfx.name = "TonicSelfVFX";
        Collider col = vfx.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        vfx.transform.position = transform.position + Vector3.up * 1.1f;
        vfx.transform.localScale = Vector3.one * 1.6f;
        ApplySimpleVfxColor(vfx, color);
        Destroy(vfx, Mathf.Max(0.1f, lifetime));
    }

    private void ApplySimpleVfxColor(GameObject vfx, Color color)
    {
        Renderer renderer = vfx.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        renderer.material = mat;
    }

    private AutoSentryTurret FindClosestOwnedSentry()
    {
        AutoSentryTurret[] turrets = FindObjectsByType<AutoSentryTurret>(FindObjectsSortMode.None);
        AutoSentryTurret best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < turrets.Length; i++)
        {
            if (turrets[i] == null || turrets[i].Owner != this)
            {
                continue;
            }
            float sqr = (turrets[i].transform.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                best = turrets[i];
                bestSqr = sqr;
            }
        }
        return best;
    }

    private IEnumerator ApplySurvivorControlLock(UnitHealth target, float duration)
    {
        if (target == null || target.IsDead)
        {
            yield break;
        }

        SurvivorMovement move = target.GetComponent<SurvivorMovement>();
        bool wasEnabled = move != null && move.enabled;
        if (move != null)
        {
            move.enabled = false;
        }

        yield return new WaitForSeconds(Mathf.Max(0.05f, duration));

        if (move != null)
        {
            move.enabled = wasEnabled;
        }
    }

    private IEnumerator ApplyBleed(UnitHealth target, float duration, int tickDamage)
    {
        if (target == null || target.IsDead)
        {
            yield break;
        }

        float end = Time.time + duration;
        while (Time.time < end && target != null && !target.IsDead)
        {
            target.TakeDamage(tickDamage, gameObject);
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void LaunchUp(UnitHealth target, float amount)
    {
        CharacterController cc = target.GetComponent<CharacterController>();
        if (TryMoveCharacterController(cc, target, Vector3.up * amount))
        {
            return;
        }

        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.AddForce(Vector3.up * amount, ForceMode.Impulse);
            return;
        }

        target.transform.position += Vector3.up * amount;
    }

    private void ApplyKnockback(UnitHealth target, Vector3 dir, float distance)
    {
        if (target == null || target.IsDead || dir.sqrMagnitude <= 0.001f || distance <= 0f)
        {
            return;
        }

        Vector3 flatDir = dir;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude <= 0.001f)
        {
            flatDir = transform.forward;
            flatDir.y = 0f;
        }

        if (flatDir.sqrMagnitude <= 0.001f)
        {
            return;
        }

        flatDir.Normalize();
        Vector3 start = target.transform.position;
        float travelDistance = ResolveKnockbackTravelDistance(target, start, flatDir, distance);
        Vector3 destination = start + flatDir * travelDistance;
        destination.y = start.y;

        if (ArenaBounds.Instance != null)
        {
            destination = ArenaBounds.Instance.ClampPosition(destination);
        }

        ApplyForcedKnockbackDisplacement(target, destination);
    }

    private float ResolveKnockbackTravelDistance(UnitHealth target, Vector3 start, Vector3 direction, float maxDistance)
    {
        CharacterController cc = target.GetComponent<CharacterController>();
        float radius = cc != null ? cc.radius : 0.4f;
        float probeHeight = cc != null ? Mathf.Max(0.5f, cc.height * 0.5f) : 1f;
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
            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.transform == target.transform || hitCollider.transform.IsChildOf(target.transform))
            {
                continue;
            }

            UnitHealth otherUnit = hitCollider.GetComponentInParent<UnitHealth>();
            if (otherUnit != null && otherUnit != target && !otherUnit.IsDead)
            {
                continue;
            }

            bestDistance = Mathf.Min(bestDistance, Mathf.Max(0f, hits[i].distance - radius * 0.35f));
            break;
        }

        return bestDistance;
    }

    private void ApplyForcedKnockbackDisplacement(UnitHealth target, Vector3 destination)
    {
        OfflineSurvivorBotAI botAi = target.GetComponent<OfflineSurvivorBotAI>();
        SurvivorMovement survivorMovement = target.GetComponent<SurvivorMovement>();
        bool botWasEnabled = botAi != null && botAi.enabled;
        bool moveWasEnabled = survivorMovement != null && survivorMovement.enabled;

        if (botAi != null)
        {
            botAi.enabled = false;
        }

        if (survivorMovement != null)
        {
            survivorMovement.enabled = false;
        }

        MoveUnitToPosition(target, destination);

        if (survivorMovement != null)
        {
            survivorMovement.enabled = moveWasEnabled;
            survivorMovement.ApplyExternalMovementLock(0.22f);
        }

        if (botAi != null)
        {
            botAi.enabled = botWasEnabled;
            botAi.ApplyKnockbackLock(0.3f);
        }
    }

    private void MoveUnitToPosition(UnitHealth target, Vector3 position)
    {
        if (target == null)
        {
            return;
        }

        CharacterController cc = target.GetComponent<CharacterController>();
        bool controllerWasEnabled = cc != null && cc.enabled;
        if (controllerWasEnabled)
        {
            cc.enabled = false;
        }

        target.transform.position = position;
        if (ArenaBounds.Instance != null)
        {
            ArenaBounds.Instance.ClampUnitTransform(target.transform, "MoveUnitToPosition");
        }

        if (cc != null && controllerWasEnabled && target.gameObject.activeInHierarchy)
        {
            cc.enabled = true;
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Runtime helper components used by PredatorClassManager
// ─────────────────────────────────────────────────────────────────────────────

public class AutoSentryTurret : MonoBehaviour
{
    public PredatorClassManager Owner { get; private set; }
    private LayerMask targetLayers;
    private string survivorTag;
    private int hp = 40;
    private float nextShotTime;

    public void Initialize(PredatorClassManager owner, LayerMask layers, string tag)
    {
        Owner = owner;
        targetLayers = layers;
        survivorTag = tag;
    }

    public void Repair(int amount)
    {
        hp = Mathf.Min(60, hp + Mathf.Max(0, amount));
    }

    private void Update()
    {
        if (Time.time < nextShotTime)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, 10f, targetLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth h = hits[i].GetComponentInParent<UnitHealth>();
            if (h != null && h.CompareTag(survivorTag) && !h.IsDead)
            {
                h.TakeDamage(3, gameObject);
                nextShotTime = Time.time + 0.35f;
                return;
            }
        }
    }
}

public class TrackingMinion : MonoBehaviour
{
    private LayerMask targetLayers;
    private string survivorTag;
    private float life = 8f;

    public void Initialize(LayerMask layers, string tag)
    {
        targetLayers = layers;
        survivorTag = tag;
    }

    private void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, 10f, targetLayers, QueryTriggerInteraction.Ignore);
        UnitHealth best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth h = hits[i].GetComponentInParent<UnitHealth>();
            if (h == null || h.IsDead || !h.CompareTag(survivorTag))
            {
                continue;
            }
            float sqr = (h.transform.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                best = h;
                bestSqr = sqr;
            }
        }

        if (best == null)
        {
            return;
        }

        Vector3 dir = (best.transform.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            transform.position += dir.normalized * 6f * Time.deltaTime;
        }

        if (bestSqr <= 1.2f * 1.2f)
        {
            best.TakeDamage(5, gameObject);
            Destroy(gameObject);
        }
    }
}

public class PersistentGroundHazard : MonoBehaviour
{
    private LayerMask targetLayers;
    private string survivorTag;
    private float duration;
    private float radius;
    private int tickDamage;
    private float nextTick;

    public void Configure(LayerMask layers, string tag, float life, float aoeRadius, int damage)
    {
        targetLayers = layers;
        survivorTag = tag;
        duration = life;
        radius = aoeRadius;
        tickDamage = damage;
    }

    private void Update()
    {
        duration -= Time.deltaTime;
        if (duration <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (Time.time < nextTick)
        {
            return;
        }

        nextTick = Time.time + 0.5f;
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, targetLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth h = hits[i].GetComponentInParent<UnitHealth>();
            if (h != null && h.CompareTag(survivorTag) && !h.IsDead)
            {
                h.TakeDamage(tickDamage, gameObject);
            }
        }
    }
}
