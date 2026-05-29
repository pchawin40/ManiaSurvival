using System.Collections;
using UnityEngine;

public enum PredatorClass { SwarmOverlord, SubterraneanStalker, DoomShieldColossus, Juggernaut, CyberNinja, Vanguard, RelentlessHook }

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitHealth))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PredatorClassManager : MonoBehaviour
{
    private const float SharedGlobalCooldown = 0.25f;

    // Required cached hashes.
    private static readonly int MeleeAttackHash = Animator.StringToHash("MeleeAttack");
    private static readonly int Ability1Hash = Animator.StringToHash("Ability1");
    private static readonly int Ability2Hash = Animator.StringToHash("Ability2");
    private static readonly int Ability3Hash = Animator.StringToHash("Ability3");
    private static readonly int UltimateHash = Animator.StringToHash("Ultimate");

    [Header("Class")]
    public PredatorClass activeClass = PredatorClass.SwarmOverlord;

    [Header("Targets")]
    public string survivorTag = "Survivor";
    public LayerMask targetLayers = ~0;
    public float baseMeleeRange = 2f;
    public int baseMeleeDamage = 10;

    [Header("Shared")]
    public bool showDebugLogs = true;
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

    private void Awake()
    {
        unitHealth = GetComponent<UnitHealth>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        monsterMovement = GetComponent<MonsterPlayerMovement>();
        gameManager = ManiaGameManager.Instance;
        cachedHealthForReduction = unitHealth != null ? unitHealth.currentHealth : 0;
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

    // Slot 0: melee / primary.
    public void CastMeleeAttack()
    {
        if (!TryConsumeSharedGcd(MeleeAttackHash))
        {
            return;
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
    }

    // Slot 1
    public void CastAbility1()
    {
        if (!TryConsumeSharedGcd(Ability1Hash))
        {
            return;
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
    }

    // Slot 2
    public void CastAbility2()
    {
        if (!TryConsumeSharedGcd(Ability2Hash))
        {
            return;
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
    }

    // Slot 3
    public void CastAbility3()
    {
        if (!TryConsumeSharedGcd(Ability3Hash))
        {
            return;
        }

        switch (activeClass)
        {
            case PredatorClass.SwarmOverlord: CastSwarmOverlordMoltenCore(); break;
            case PredatorClass.SubterraneanStalker: CastSubterraneanTectonicFault(); break;
            case PredatorClass.DoomShieldColossus: CastDoomShieldCageMatch(); break;
            case PredatorClass.Juggernaut: StartCoroutine(CastJuggernautMeteorStrikeCoroutine()); break;
            case PredatorClass.CyberNinja: CastCyberNinjaDragonblade(); break;
            case PredatorClass.Vanguard: CastVanguardEarthshatter(); break;
            case PredatorClass.RelentlessHook: StartCoroutine(CastRelentlessWholeHogCoroutine()); break;
        }
    }

    // Optional explicit ultimate route: same as slot 3 map.
    public void CastUltimate()
    {
        if (!TryConsumeSharedGcd(UltimateHash))
        {
            return;
        }

        // Uses same blueprint mapping's fourth action.
        switch (activeClass)
        {
            case PredatorClass.SwarmOverlord: CastSwarmOverlordMoltenCore(); break;
            case PredatorClass.SubterraneanStalker: CastSubterraneanTectonicFault(); break;
            case PredatorClass.DoomShieldColossus: CastDoomShieldCageMatch(); break;
            case PredatorClass.Juggernaut: StartCoroutine(CastJuggernautMeteorStrikeCoroutine()); break;
            case PredatorClass.CyberNinja: CastCyberNinjaDragonblade(); break;
            case PredatorClass.Vanguard: CastVanguardEarthshatter(); break;
            case PredatorClass.RelentlessHook: StartCoroutine(CastRelentlessWholeHogCoroutine()); break;
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
            if (showDebugLogs)
            {
                float remaining = Mathf.Max(0f, nextGlobalCastTime - Time.time);
                Debug.Log("[PredatorClassManager] Cast rejected: Global Cooldown Active (" + remaining.ToString("0.00") + "s).");
            }

            return false;
        }

        if (animator != null)
        {
            animator.SetTrigger(triggerHash);
        }

        nextGlobalCastTime = Time.time + SharedGlobalCooldown;
        return true;
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
            characterController.Move(dir * speed * Time.deltaTime);
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
            characterController.Move(dir * speed * Time.deltaTime);
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
            CollisionFlags flags = characterController.Move(dir * speed * Time.deltaTime);
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
            characterController.Move(dir * speed * Time.deltaTime);
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
            characterController.Move(dir * speed * Time.deltaTime);
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
        const int pellets = 10;
        for (int i = 0; i < pellets; i++)
        {
            Vector3 dir = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(-20f, 20f), 0f) * transform.forward;
            if (Physics.Raycast(GetProjectileSpawnPosition(), dir, out RaycastHit hit, 8f, targetLayers, QueryTriggerInteraction.Ignore))
            {
                UnitHealth h = hit.collider.GetComponentInParent<UnitHealth>();
                if (h != null && h.CompareTag(survivorTag) && !h.IsDead)
                {
                    h.TakeDamage(3, gameObject);
                }
            }
        }
    }

    private IEnumerator CastRelentlessChainHookCoroutine()
    {
        Vector3 origin = GetProjectileSpawnPosition();
        Vector3 dir = transform.forward;
        if (hookProjectilePrefab != null)
        {
            Instantiate(hookProjectilePrefab, origin, Quaternion.LookRotation(dir));
        }

        if (Physics.Raycast(origin, dir, out RaycastHit hit, 14f, targetLayers, QueryTriggerInteraction.Ignore))
        {
            UnitHealth target = hit.collider.GetComponentInParent<UnitHealth>();
            if (target != null && target.CompareTag(survivorTag) && !target.IsDead)
            {
                float duration = 0.2f;
                float elapsed = 0f;
                Vector3 start = target.transform.position;
                Vector3 end = transform.position + transform.forward * 1.0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    Vector3 p = Vector3.Lerp(start, end, elapsed / duration);
                    MoveUnitToPosition(target, p);
                    yield return null;
                }
            }
        }
    }

    private IEnumerator CastRelentlessTakeABreatherCoroutine()
    {
        float duration = 1.8f;
        float end = Time.time + duration;
        int localLast = unitHealth != null ? unitHealth.currentHealth : 0;
        while (Time.time < end && unitHealth != null && !unitHealth.IsDead)
        {
            unitHealth.Heal(2);

            // 50% DR approximation: restore half of incoming damage each frame.
            if (unitHealth.currentHealth < localLast)
            {
                int lost = localLast - unitHealth.currentHealth;
                unitHealth.Heal(Mathf.Max(1, lost / 2));
            }
            localLast = unitHealth.currentHealth;
            yield return new WaitForSeconds(0.12f);
        }
    }

    private IEnumerator CastRelentlessWholeHogCoroutine()
    {
        float duration = 2.8f;
        float fireStep = 0.08f;
        float end = Time.time + duration;
        while (Time.time < end)
        {
            UnitHealth[] hits = GetSurvivorsInRange(transform.position + transform.forward * 2.5f, 3.5f);
            for (int i = 0; i < hits.Length; i++)
            {
                hits[i].TakeDamage(2, gameObject);
                ApplyKnockback(hits[i], (hits[i].transform.position - transform.position).normalized, 0.7f);
            }
            yield return new WaitForSeconds(fireStep);
        }
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
        Collider[] hits = Physics.OverlapSphere(center, Mathf.Max(0.1f, radius), targetLayers, QueryTriggerInteraction.Ignore);
        System.Collections.Generic.List<UnitHealth> result = new System.Collections.Generic.List<UnitHealth>();
        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth h = hits[i].GetComponentInParent<UnitHealth>();
            if (h == null || h.IsDead || !h.CompareTag(survivorTag))
            {
                continue;
            }

            if (!result.Contains(h))
            {
                result.Add(h);
            }
        }

        return result.ToArray();
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
        if (cc != null && cc.enabled)
        {
            cc.Move(Vector3.up * amount);
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
        if (target == null || target.IsDead || dir.sqrMagnitude <= 0.001f)
        {
            return;
        }

        CharacterController cc = target.GetComponent<CharacterController>();
        if (cc != null && cc.enabled)
        {
            cc.Move(dir.normalized * distance);
            return;
        }

        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.AddForce(dir.normalized * distance, ForceMode.Impulse);
            return;
        }

        target.transform.position += dir.normalized * distance;
    }

    private void MoveUnitToPosition(UnitHealth target, Vector3 position)
    {
        if (target == null)
        {
            return;
        }

        CharacterController cc = target.GetComponent<CharacterController>();
        if (cc != null && cc.enabled)
        {
            cc.Move(position - target.transform.position);
        }
        else
        {
            target.transform.position = position;
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
