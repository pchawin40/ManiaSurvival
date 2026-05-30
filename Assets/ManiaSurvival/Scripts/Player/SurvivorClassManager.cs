using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SurvivorClass
{
    Medic,
    Warden,
    Weaver
}

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitHealth))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class SurvivorClassManager : MonoBehaviour
{
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int Ability2Hash = Animator.StringToHash("Ability2");
    private static readonly int Ability3Hash = Animator.StringToHash("Ability3");
    private static readonly int UltimateHash = Animator.StringToHash("Ultimate");

    [Header("Class")]
    public SurvivorClass activeClass = SurvivorClass.Medic;

    [Header("Common")]
    public string survivorTag = "Survivor";
    public string[] predatorTags = { "Monster", "Predator" };
    public LayerMask targetLayers = ~0;
    public bool showDebugLogs = true;
    public bool useExternalCooldownAuthority = true;

    [Header("Global Cooldowns (30% Nerf Applied)")]
    [Tooltip("All base cooldowns are multiplied by this value.")]
    public float globalCooldownMultiplier = 1.3f;
    public float primaryCooldown = 2.5f;
    public float ability2Cooldown = 8f;
    public float ability3Cooldown = 10f;
    public float ultimateCooldown = 20f;

    [Header("Medic - Heal Dart")]
    public GameObject bioticDartProjectilePrefab;
    public float bioticDartSpeed = 18f;
    public float bioticDartLifetime = 2f;
    [Tooltip("Max range for Heal Dart ally search and hit validation.")]
    public float bioticDartRange = 12f;
    public int healDartAmount = 3;
    public int bioticDartPredatorDamage = 6;

    [Header("Medic - Heal Pulse")]
    [Tooltip("Area heal radius — keep local, not map-wide.")]
    public float healPulseRadius = 7f;
    public int healPulseAmount = 2;

    [Header("Medic - Guardian Angel")]
    [Tooltip("Max range to dash toward an ally (Tether).")]
    public float guardianAngelRange = 14f;
    public float guardianAngelDashDuration = 0.2f;
    public float guardianAngelStopDistance = 1.25f;

    [Header("Medic - Sanctuary")]
    public GameObject immortalityFieldPrefab;
    public float sanctuaryDuration = 4f;
    [Tooltip("Healing zone radius — should not cover the whole map.")]
    public float sanctuaryRadius = 8f;
    public int sanctuaryHealPerSecond = 1;
    [Tooltip("Seconds between sanctuary heal ticks.")]
    public float sanctuaryTickInterval = 1.75f;

    [Header("Warden - Rocket Flail")]
    public float rocketFlailRange = 2.6f;
    public float rocketFlailArc = 100f;
    public int rocketFlailDamage = 8;
    public float rocketFlailKnockbackDistance = 1.4f;

    [Header("Warden - Shield Bash")]
    public float shieldBashDistance = 4f;
    public float shieldBashDuration = 0.2f;
    public int shieldBashDamage = 6;
    public float shieldBashStunDuration = 1.0f;

    [Header("Warden - Amp It Up")]
    public float ampItUpRadius = 7f;
    public float ampItUpMultiplier = 1.2f;
    public float ampItUpDuration = 2.5f;

    [Header("Warden - Sound Barrier")]
    public float soundBarrierRadius = 8f;
    public int soundBarrierBonusHealth = 35;
    public float soundBarrierDuration = 4f;
    public GameObject echoingShieldZonePrefab;

    [Header("Weaver - Healing Blossom")]
    public GameObject healingBlossomProjectilePrefab;
    public float healingBlossomSpeed = 12f;
    public float healingBlossomTurnSpeed = 10f;
    public float healingBlossomLifetime = 3f;
    public int healingBlossomHealAmount = 10;
    public float healingBlossomTargetRange = 16f;

    [Header("Weaver - Life Grip")]
    public float lifeGripRange = 12f;
    public float lifeGripPullDuration = 0.25f;
    public float lifeGripEndOffset = 1.3f;
    public float lifeGripBubbleDuration = 1.0f;
    public GameObject lifeGripBubblePrefab;

    [Header("Weaver - Swift Step")]
    public float swiftStepDistance = 5f;
    public LayerMask swiftStepBlockerLayers = ~0;
    public float swiftStepWallBuffer = 0.5f;

    [Header("Weaver - Protection Suzu")]
    public GameObject suzuProjectilePrefab;
    public float suzuProjectileSpeed = 14f;
    public float suzuProjectileLifetime = 2.5f;
    public float suzuRadius = 4f;
    public float suzuInvulnerabilityDuration = 1f;
    [Tooltip("Damage reduction while Suzu protection is active (not full invulnerability).")]
    [Range(0f, 0.75f)] public float suzuDamageReduction = 0.30f;
    [Tooltip("Damage reduction during Life Grip bubble.")]
    [Range(0f, 0.75f)] public float lifeGripDamageReduction = 0.35f;

    private UnitHealth unitHealth;
    private CharacterController characterController;
    private Animator animator;
    private float nextPrimaryReadyAt;
    private float nextAbility2ReadyAt;
    private float nextAbility3ReadyAt;
    private float nextUltimateReadyAt;
    private float battleCadenceGizmoUntil;
    private Vector3 battleCadenceGizmoCenter;

    private void Awake()
    {
        unitHealth = GetComponent<UnitHealth>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    public bool ExecutePrimary()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[SurvivorClassManager] Executing ability: {activeClass}/Primary");
        }

        if (!CanExecute())
        {
            return false;
        }

        if (!TryConsumeCooldown("Primary", primaryCooldown, ref nextPrimaryReadyAt))
        {
            return false;
        }

        TriggerAnimation(AttackHash);

        switch (activeClass)
        {
            case SurvivorClass.Medic:
                return ExecuteMedicPrimary();
            case SurvivorClass.Warden:
                ExecuteWardenPrimary();
                break;
            case SurvivorClass.Weaver:
                ExecuteWeaverPrimary();
                break;
        }

        return true;
    }

    public bool ExecuteAbility2()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[SurvivorClassManager] Executing ability: {activeClass}/Ability2");
        }

        if (!CanExecute())
        {
            return false;
        }

        if (!TryConsumeCooldown("Ability2", ability2Cooldown, ref nextAbility2ReadyAt))
        {
            return false;
        }

        TriggerAnimation(Ability2Hash);

        switch (activeClass)
        {
            case SurvivorClass.Medic:
                return ExecuteMedicHealPulse();
            case SurvivorClass.Warden:
                StartCoroutine(WardenShieldBashCoroutine());
                break;
            case SurvivorClass.Weaver:
                StartCoroutine(WeaverLifeGripCoroutine());
                break;
        }

        return true;
    }

    public bool ExecuteAbility3()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[SurvivorClassManager] Executing ability: {activeClass}/Ability3");
        }

        if (!CanExecute())
        {
            return false;
        }

        if (!TryConsumeCooldown("Ability3", ability3Cooldown, ref nextAbility3ReadyAt))
        {
            return false;
        }

        TriggerAnimation(Ability3Hash);

        switch (activeClass)
        {
            case SurvivorClass.Medic:
                if (TryStartMedicTether())
                {
                    return true;
                }

                return TrySurvivorBlink();
            case SurvivorClass.Warden:
                TriggerAnimation(Ability3Hash);
                Debug.Log("Warden activated Battle Cadence!");
                ExecuteWardenAmpItUp();
                break;
            case SurvivorClass.Weaver:
                ExecuteWeaverSwiftStep();
                break;
        }

        return true;
    }

    public bool ExecuteUltimate()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[SurvivorClassManager] Executing ability: {activeClass}/Ultimate");
        }

        if (!CanExecute())
        {
            return false;
        }

        if (!TryConsumeCooldown("Ultimate", ultimateCooldown, ref nextUltimateReadyAt))
        {
            return false;
        }

        TriggerAnimation(UltimateHash);

        switch (activeClass)
        {
            case SurvivorClass.Medic:
                return ExecuteMedicSanctuary();
            case SurvivorClass.Warden:
                ExecuteWardenSoundBarrier();
                break;
            case SurvivorClass.Weaver:
                ExecuteWeaverProtectionSuzu();
                break;
        }

        return true;
    }

    private bool CanExecute()
    {
        if (unitHealth == null)
        {
            unitHealth = GetComponent<UnitHealth>();
        }

        return unitHealth != null && !unitHealth.IsDead;
    }

    private void TriggerAnimation(int triggerHash)
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator != null)
        {
            animator.SetTrigger(triggerHash);
        }
    }

    private bool TryConsumeCooldown(string abilityName, float baseCooldown, ref float nextReadyAt)
    {
        if (useExternalCooldownAuthority)
        {
            return true;
        }

        float now = Time.time;
        if (now < nextReadyAt)
        {
            if (showDebugLogs)
            {
                float remaining = nextReadyAt - now;
                Debug.Log($"[SurvivorClassManager] {activeClass} {abilityName} on cooldown ({remaining:0.00}s remaining).");
            }

            return false;
        }

        float scaledCooldown = Mathf.Max(0f, baseCooldown) * Mathf.Max(0f, globalCooldownMultiplier);
        nextReadyAt = now + scaledCooldown;
        return true;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Medic
    // ═════════════════════════════════════════════════════════════════════════

    private bool ExecuteMedicPrimary()
    {
        UnitHealth target = FindClosestAlly(transform.position, bioticDartRange, requireDamaged: true);
        if (target == null && unitHealth != null && !unitHealth.IsDead && unitHealth.currentHealth < unitHealth.maxHealth)
        {
            target = unitHealth;
        }

        if (target == null)
        {
            Debug.Log("[AbilityBlock] Heal blocked: no wounded ally in range");
            return false;
        }

        if (target.currentHealth >= target.maxHealth)
        {
            Debug.Log("[AbilityBlock] Heal blocked: target already full HP");
            return false;
        }

        if (!IsUnitWithinFlatRange(target, transform.position, bioticDartRange))
        {
            Debug.Log("[AbilityBlock] Heal blocked: target outside heal range");
            return false;
        }

        Vector3 spawn = GetProjectileSpawnPosition();
        Vector3 direction = target.transform.position - spawn;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = GetForwardDirection();
        }
        else
        {
            direction.Normalize();
        }

        if (bioticDartProjectilePrefab != null)
        {
            GameObject obj = Instantiate(bioticDartProjectilePrefab, spawn, Quaternion.LookRotation(direction));
            Debug.Log("[AbilityVFX] Spawned VFX: " + obj.name + ", position=" + obj.transform.position + ", duration=" + bioticDartLifetime.ToString("0.00") + "s");
            BioticDartProjectile projectile = obj.GetComponent<BioticDartProjectile>();
            if (projectile == null)
            {
                projectile = obj.AddComponent<BioticDartProjectile>();
            }

            projectile.Initialize(this, direction, bioticDartSpeed, bioticDartLifetime, bioticDartRange, target);
            return true;
        }

        ApplyBioticDartEffect(target);
        return true;
    }

    private bool ExecuteMedicHealPulse()
    {
        UnitHealth[] allies = FindAlliesInRange(transform.position, healPulseRadius);
        int healedCount = 0;

        for (int i = 0; i < allies.Length; i++)
        {
            UnitHealth ally = allies[i];
            if (ally == null || ally.IsDead || ally.currentHealth >= ally.maxHealth)
            {
                continue;
            }

            if (ally.Heal(healPulseAmount) > 0)
            {
                healedCount++;
            }
        }

        if (healedCount > 0 && showDebugLogs)
        {
            Debug.Log("[SurvivorAbility] Heal Pulse healed " + healedCount + " wounded allies");
        }
        else if (healedCount == 0)
        {
            Debug.Log("[AbilityBlock] Heal blocked: no wounded ally in range");
        }

        return healedCount > 0;
    }

    private bool TryStartMedicTether()
    {
        UnitHealth ally = FindClosestAlly(transform.position, guardianAngelRange, requireDamaged: false);
        if (ally == null)
        {
            Debug.Log("[AbilityBlock] Tether blocked: no ally in range");
            return false;
        }

        if (!IsUnitWithinFlatRange(ally, transform.position, guardianAngelRange))
        {
            Debug.Log("[AbilityBlock] Tether blocked: target outside range");
            return false;
        }

        StartCoroutine(MedicGuardianAngelCoroutine(ally));
        return true;
    }

    private IEnumerator MedicGuardianAngelCoroutine(UnitHealth ally)
    {
        if (ally == null)
        {
            yield break;
        }

        Vector3 start = transform.position;
        Vector3 toAlly = ally.transform.position - start;
        toAlly.y = 0f;
        Vector3 target = ally.transform.position - toAlly.normalized * guardianAngelStopDistance;
        target.y = transform.position.y;

        float duration = Mathf.Max(0.05f, guardianAngelDashDuration);
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            Vector3 desired = Vector3.Lerp(start, target, Mathf.Clamp01(t / duration));
            MoveCharacterControllerToward(desired);
            yield return null;
        }
    }

    private bool ExecuteMedicSanctuary()
    {
        if (immortalityFieldPrefab == null)
        {
            if (showDebugLogs)
            {
                Debug.Log("[SurvivorAbility] Sanctuary missing field prefab.");
            }

            return false;
        }

        GameObject field = Instantiate(immortalityFieldPrefab, transform.position, Quaternion.identity);
        Debug.Log("[AbilityVFX] Spawned VFX: " + field.name + ", position=" + field.transform.position + ", duration=" + sanctuaryDuration.ToString("0.00") + "s");
        SanctuaryHealZone zone = field.GetComponent<SanctuaryHealZone>();
        if (zone == null)
        {
            zone = field.AddComponent<SanctuaryHealZone>();
        }

        zone.Initialize(this, sanctuaryRadius, sanctuaryDuration, sanctuaryHealPerSecond, sanctuaryTickInterval, survivorTag, showDebugLogs);
        return true;
    }

    private void ApplyBioticDartEffect(UnitHealth target)
    {
        if (target == null || target.IsDead)
        {
            return;
        }

        if (IsAlly(target))
        {
            if (target.currentHealth >= target.maxHealth)
            {
                Debug.Log("[AbilityBlock] Heal blocked: target already full HP");
                return;
            }

            if (!IsUnitWithinFlatRange(target, transform.position, bioticDartRange))
            {
                Debug.Log("[AbilityBlock] Heal blocked: target outside heal range");
                return;
            }

            int healed = target.Heal(healDartAmount);
            if (healed > 0 && showDebugLogs)
            {
                Debug.Log("[SurvivorAbility] Heal Dart healed " + target.name + " for " + healed);
            }

            return;
        }

        if (IsPredator(target))
        {
            target.TakeDamage(bioticDartPredatorDamage, gameObject);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Warden
    // ═════════════════════════════════════════════════════════════════════════

    private void ExecuteWardenPrimary()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, rocketFlailRange, targetLayers, QueryTriggerInteraction.Ignore);
        Vector3 forward = GetForwardDirection();

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth target = hits[i].GetComponentInParent<UnitHealth>();
            if (target == null || target.IsDead || !IsPredator(target))
            {
                continue;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            float angle = Vector3.Angle(forward, toTarget.normalized);
            if (angle > rocketFlailArc * 0.5f)
            {
                continue;
            }

            target.TakeDamage(rocketFlailDamage, gameObject);
            ApplyKnockback(target, toTarget.normalized, rocketFlailKnockbackDistance);
        }
    }

    private IEnumerator WardenShieldBashCoroutine()
    {
        Vector3 dir = GetForwardDirection();
        float duration = Mathf.Max(0.05f, shieldBashDuration);
        float speed = shieldBashDistance / duration;
        float elapsed = 0f;
        UnitHealth hitPredator = null;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (characterController != null && characterController.enabled)
            {
                characterController.Move(dir * speed * Time.deltaTime);
            }
            else
            {
                transform.position += dir * speed * Time.deltaTime;
            }

            if (hitPredator == null)
            {
                hitPredator = FindClosestPredator(transform.position, 1.25f);
            }

            yield return null;
        }

        if (hitPredator != null)
        {
            hitPredator.TakeDamage(shieldBashDamage, gameObject);
            StartCoroutine(ApplyPredatorStunCoroutine(hitPredator, shieldBashStunDuration));
        }
    }

    private void ExecuteWardenAmpItUp()
    {
        battleCadenceGizmoCenter = transform.position;
        battleCadenceGizmoUntil = Time.time + 1f;

        UnitHealth[] allies = FindAlliesInRange(transform.position, ampItUpRadius);
        for (int i = 0; i < allies.Length; i++)
        {
            SurvivorMovement move = allies[i] != null ? allies[i].GetComponent<SurvivorMovement>() : null;
            if (move != null)
            {
                move.ApplySpeedBoost(ampItUpMultiplier, ampItUpDuration);
            }
        }
    }

    private void ExecuteWardenSoundBarrier()
    {
        if (echoingShieldZonePrefab != null)
        {
            GameObject zone = Instantiate(echoingShieldZonePrefab, transform.position, Quaternion.identity);
            Destroy(zone, Mathf.Max(0.1f, soundBarrierDuration));
        }

        UnitHealth[] allies = FindAlliesInRange(transform.position, soundBarrierRadius);
        for (int i = 0; i < allies.Length; i++)
        {
            if (allies[i] == null || allies[i].IsDead)
            {
                continue;
            }

            StartCoroutine(ApplyTemporaryOverhealthCoroutine(allies[i], soundBarrierBonusHealth, soundBarrierDuration));
        }
    }

    private IEnumerator ApplyPredatorStunCoroutine(UnitHealth predator, float duration)
    {
        if (predator == null || predator.IsDead)
        {
            yield break;
        }

        MonsterPlayerMovement movement = predator.GetComponent<MonsterPlayerMovement>();
        MonsterAI ai = predator.GetComponent<MonsterAI>();
        bool movementWasEnabled = movement != null && movement.enabled;
        bool aiWasEnabled = ai != null && ai.enabled;

        if (movement != null)
        {
            movement.enabled = false;
        }

        if (ai != null)
        {
            ai.enabled = false;
        }

        yield return new WaitForSeconds(Mathf.Max(0.05f, duration));

        if (movement != null)
        {
            movement.enabled = movementWasEnabled;
        }

        if (ai != null)
        {
            ai.enabled = aiWasEnabled;
        }
    }

    private IEnumerator ApplyTemporaryOverhealthCoroutine(UnitHealth ally, int bonusHealth, float duration)
    {
        if (ally == null || ally.IsDead || bonusHealth <= 0)
        {
            yield break;
        }

        int originalMax = ally.maxHealth;
        ally.maxHealth = originalMax + bonusHealth;
        ally.currentHealth = Mathf.Min(ally.maxHealth, ally.currentHealth + bonusHealth);

        yield return new WaitForSeconds(Mathf.Max(0.1f, duration));

        if (ally == null)
        {
            yield break;
        }

        ally.maxHealth = originalMax;
        ally.currentHealth = Mathf.Clamp(ally.currentHealth, 0, ally.maxHealth);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Weaver
    // ═════════════════════════════════════════════════════════════════════════

    private void ExecuteWeaverPrimary()
    {
        Debug.Log("[SurvivorClassManager] Weaver cast Healing Blossom.");
        TriggerAnimation(AttackHash);

        UnitHealth ally = FindClosestAlly(transform.position, healingBlossomTargetRange, requireDamaged: true);
        if (ally == null)
        {
            if (showDebugLogs)
            {
                Debug.Log("[SurvivorClassManager] Weaver Healing Blossom found no damaged ally target.");
            }
            return;
        }

        Vector3 spawn = GetProjectileSpawnPosition();

        GameObject obj = healingBlossomProjectilePrefab != null
            ? Instantiate(healingBlossomProjectilePrefab, spawn, Quaternion.identity)
            : CreateFallbackProjectileVisual("HealingBlossomFallback", Color.cyan, 0.24f, spawn, Quaternion.identity);

        if (obj != null)
        {
            HealingBlossomProjectile projectile = obj.GetComponent<HealingBlossomProjectile>();
            if (projectile == null)
            {
                projectile = obj.AddComponent<HealingBlossomProjectile>();
            }

            projectile.Initialize(ally, healingBlossomHealAmount, healingBlossomSpeed, healingBlossomTurnSpeed, healingBlossomLifetime);
            return;
        }

        ally.Heal(healingBlossomHealAmount);
    }

    private IEnumerator WeaverLifeGripCoroutine()
    {
        Debug.Log("[SurvivorClassManager] Weaver cast Spatial Extraction.");
        TriggerAnimation(Ability2Hash);

        UnitHealth ally = FindClosestAlly(transform.position, lifeGripRange, requireDamaged: false);
        if (ally == null)
        {
            yield break;
        }

        GameObject bubble = null;
        if (lifeGripBubblePrefab != null)
        {
            bubble = Instantiate(lifeGripBubblePrefab, ally.transform.position, Quaternion.identity, ally.transform);
            Destroy(bubble, lifeGripBubbleDuration);
        }

        ally.ApplyTemporaryDamageReduction(lifeGripDamageReduction, lifeGripBubbleDuration);

        Vector3 start = ally.transform.position;
        Vector3 end = transform.position - GetForwardDirection() * lifeGripEndOffset;
        end.y = start.y;
        float duration = Mathf.Max(0.05f, lifeGripPullDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 targetPos = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
            MoveUnitToward(ally, targetPos);
            yield return null;
        }

        if (bubble != null)
        {
            Destroy(bubble);
        }
    }

    private bool TrySurvivorBlink()
    {
        SurvivorBlinkAbility blink = GetComponent<SurvivorBlinkAbility>();
        if (blink == null)
        {
            blink = gameObject.AddComponent<SurvivorBlinkAbility>();
        }

        return blink.TryBlinkStep();
    }

    private void ExecuteWeaverSwiftStep()
    {
        Debug.Log("[SurvivorClassManager] Weaver activated Swift Step.");

        Vector3 direction = characterController != null ? characterController.velocity : Vector3.zero;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = GetForwardDirection();
        }

        direction = direction.normalized;
        Vector3 start = transform.position;
        float maxDistance = Mathf.Max(0.1f, swiftStepDistance);
        float travelDistance = maxDistance;
        Vector3 rayStart = start + Vector3.up * 0.25f;
        if (Physics.Raycast(rayStart, direction, out RaycastHit hit, maxDistance, swiftStepBlockerLayers, QueryTriggerInteraction.Ignore))
        {
            travelDistance = Mathf.Max(0f, hit.distance - Mathf.Max(0f, swiftStepWallBuffer));
            if (showDebugLogs)
            {
                Debug.Log($"[SurvivorClassManager] Swift Step truncated by blocker '{hit.collider.name}' at {travelDistance:0.00}m.");
            }
        }

        transform.position = start + direction * travelDistance;
    }

    private void ExecuteWeaverProtectionSuzu()
    {
        Debug.Log("[SurvivorClassManager] Weaver cast Protection Suzu.");
        TriggerAnimation(UltimateHash);

        Vector3 spawn = GetProjectileSpawnPosition();
        Vector3 dir = GetForwardDirection();

        GameObject obj = suzuProjectilePrefab != null
            ? Instantiate(suzuProjectilePrefab, spawn, Quaternion.LookRotation(dir))
            : CreateFallbackProjectileVisual("SuzuFallback", Color.yellow, 0.28f, spawn, Quaternion.LookRotation(dir));

        if (obj != null)
        {
            ProtectionSuzuProjectile projectile = obj.GetComponent<ProtectionSuzuProjectile>();
            if (projectile == null)
            {
                projectile = obj.AddComponent<ProtectionSuzuProjectile>();
            }

            projectile.Initialize(this, dir, suzuProjectileSpeed, suzuProjectileLifetime, suzuRadius, suzuInvulnerabilityDuration);
            return;
        }

        ApplySuzuAtPosition(transform.position + dir * 2f);
    }

    private GameObject CreateFallbackProjectileVisual(string objectName, Color tint, float scale, Vector3 position, Quaternion rotation)
    {
        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fallback.name = objectName;
        fallback.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        fallback.transform.SetPositionAndRotation(position, rotation);

        Collider fallbackCollider = fallback.GetComponent<Collider>();
        if (fallbackCollider != null)
        {
            Destroy(fallbackCollider);
        }

        MeshRenderer renderer = fallback.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = tint;
            renderer.material = material;
        }

        return fallback;
    }

    private void OnDrawGizmos()
    {
        if (Time.time > battleCadenceGizmoUntil || ampItUpRadius <= 0f)
        {
            return;
        }

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.65f);
        Gizmos.DrawWireSphere(battleCadenceGizmoCenter, ampItUpRadius);
    }

    public void ApplySuzuAtPosition(Vector3 position)
    {
        UnitHealth[] allies = FindAlliesInRange(position, suzuRadius);
        for (int i = 0; i < allies.Length; i++)
        {
            UnitHealth ally = allies[i];
            if (ally == null || ally.IsDead)
            {
                continue;
            }

            // Simple cleanse pass: if ally has movement disabled, re-enable it.
            SurvivorMovement move = ally.GetComponent<SurvivorMovement>();
            if (move != null && !move.enabled)
            {
                move.enabled = true;
            }

            ally.ApplyTemporaryDamageReduction(suzuDamageReduction, suzuInvulnerabilityDuration);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Shared helpers
    // ═════════════════════════════════════════════════════════════════════════

    private Vector3 GetForwardDirection()
    {
        Vector3 dir = transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.001f)
        {
            return Vector3.forward;
        }

        return dir.normalized;
    }

    private Vector3 GetProjectileSpawnPosition()
    {
        return transform.position + Vector3.up * 1.1f + GetForwardDirection() * 0.8f;
    }

    private UnitHealth[] FindAlliesInRange(Vector3 center, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(center, Mathf.Max(0.1f, radius), targetLayers, QueryTriggerInteraction.Ignore);
        List<UnitHealth> result = new List<UnitHealth>();

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth health = hits[i].GetComponentInParent<UnitHealth>();
            if (health == null || health.IsDead || !IsAlly(health))
            {
                continue;
            }

            if (!IsUnitWithinFlatRange(health, center, radius))
            {
                continue;
            }

            if (!result.Contains(health))
            {
                result.Add(health);
            }
        }

        if (!result.Contains(unitHealth) && unitHealth != null && !unitHealth.IsDead)
        {
            if (IsUnitWithinFlatRange(unitHealth, center, radius))
            {
                result.Add(unitHealth);
            }
        }

        return result.ToArray();
    }

    private bool IsUnitWithinFlatRange(UnitHealth unit, Vector3 center, float range)
    {
        if (unit == null)
        {
            return false;
        }

        Vector3 delta = unit.transform.position - center;
        delta.y = 0f;
        return delta.sqrMagnitude <= range * range;
    }

    private UnitHealth FindClosestAlly(Vector3 center, float radius, bool requireDamaged)
    {
        UnitHealth[] allies = FindAlliesInRange(center, radius);
        UnitHealth nearest = null;
        float nearestDist = float.MaxValue;

        for (int i = 0; i < allies.Length; i++)
        {
            UnitHealth ally = allies[i];
            if (ally == null || ally == unitHealth || ally.IsDead)
            {
                continue;
            }

            if (requireDamaged && ally.currentHealth >= ally.maxHealth)
            {
                continue;
            }

            float dist = (ally.transform.position - center).sqrMagnitude;
            if (dist < nearestDist)
            {
                nearest = ally;
                nearestDist = dist;
            }
        }

        return nearest;
    }

    private UnitHealth FindClosestPredator(Vector3 center, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(center, Mathf.Max(0.1f, radius), targetLayers, QueryTriggerInteraction.Ignore);
        UnitHealth nearest = null;
        float nearestDist = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth health = hits[i].GetComponentInParent<UnitHealth>();
            if (health == null || health.IsDead || !IsPredator(health))
            {
                continue;
            }

            float dist = (health.transform.position - center).sqrMagnitude;
            if (dist < nearestDist)
            {
                nearest = health;
                nearestDist = dist;
            }
        }

        return nearest;
    }

    private bool IsAlly(UnitHealth health)
    {
        return health != null && health.CompareTag(survivorTag);
    }

    private bool IsPredator(UnitHealth health)
    {
        if (health == null)
        {
            return false;
        }

        for (int i = 0; i < predatorTags.Length; i++)
        {
            if (!string.IsNullOrEmpty(predatorTags[i]) && health.CompareTag(predatorTags[i]))
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyKnockback(UnitHealth target, Vector3 direction, float distance)
    {
        if (target == null || direction.sqrMagnitude <= 0.001f || distance <= 0f)
        {
            return;
        }

        CharacterController cc = target.GetComponent<CharacterController>();
        if (cc != null && cc.enabled)
        {
            cc.Move(direction.normalized * distance);
            return;
        }

        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.AddForce(direction.normalized * distance, ForceMode.Impulse);
            return;
        }

        target.transform.position += direction.normalized * distance;
    }

    private void MoveCharacterControllerToward(Vector3 worldTarget)
    {
        if (characterController != null && characterController.enabled)
        {
            Vector3 delta = worldTarget - transform.position;
            characterController.Move(delta);
            return;
        }

        transform.position = worldTarget;
    }

    private void MoveUnitToward(UnitHealth unit, Vector3 worldTarget)
    {
        if (unit == null)
        {
            return;
        }

        CharacterController cc = unit.GetComponent<CharacterController>();
        if (cc != null && cc.enabled)
        {
            cc.Move(worldTarget - unit.transform.position);
            return;
        }

        unit.transform.position = worldTarget;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helper runtime components
    // ═════════════════════════════════════════════════════════════════════════

    private class BioticDartProjectile : MonoBehaviour
    {
        private SurvivorClassManager owner;
        private Vector3 direction;
        private float speed;
        private float lifetime;
        private float range;
        private float distanceTravelled;
        private UnitHealth homingTarget;

        public void Initialize(
            SurvivorClassManager manager,
            Vector3 dir,
            float moveSpeed,
            float life,
            float maxRange,
            UnitHealth target)
        {
            owner = manager;
            direction = dir.normalized;
            speed = Mathf.Max(1f, moveSpeed);
            lifetime = Mathf.Max(0.1f, life);
            range = Mathf.Max(1f, maxRange);
            distanceTravelled = 0f;
            homingTarget = target;
        }

        private void Update()
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }

            if (homingTarget != null && !homingTarget.IsDead)
            {
                Vector3 toTarget = homingTarget.transform.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.01f)
                {
                    direction = toTarget.normalized;
                }

                if (toTarget.sqrMagnitude <= 0.64f)
                {
                    owner.ApplyBioticDartEffect(homingTarget);
                    Destroy(gameObject);
                    return;
                }
            }

            float step = speed * Time.deltaTime;
            Vector3 start = transform.position;
            Vector3 end = start + direction * step;

            if (Physics.Linecast(start, end, out RaycastHit hit, owner.targetLayers, QueryTriggerInteraction.Ignore))
            {
                UnitHealth target = hit.collider.GetComponentInParent<UnitHealth>();
                owner.ApplyBioticDartEffect(target);
                Destroy(gameObject);
                return;
            }

            transform.position = end;
            distanceTravelled += step;
            lifetime -= Time.deltaTime;

            if (lifetime <= 0f || distanceTravelled >= range)
            {
                Destroy(gameObject);
            }
        }
    }

    private class HealingBlossomProjectile : MonoBehaviour
    {
        private UnitHealth target;
        private int healAmount;
        private float speed;
        private float turnSpeed;
        private float life;

        public void Initialize(UnitHealth healTarget, int amount, float projectileSpeed, float projectileTurnSpeed, float lifetime)
        {
            target = healTarget;
            healAmount = amount;
            speed = Mathf.Max(1f, projectileSpeed);
            turnSpeed = Mathf.Max(1f, projectileTurnSpeed);
            life = Mathf.Max(0.1f, lifetime);
        }

        private void Update()
        {
            if (target == null || target.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            life -= Time.deltaTime;
            if (life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            if (toTarget.sqrMagnitude <= 0.25f)
            {
                target.Heal(healAmount);
                Destroy(gameObject);
                return;
            }

            Vector3 desired = toTarget.normalized;
            transform.forward = Vector3.Slerp(transform.forward, desired, turnSpeed * Time.deltaTime);
            transform.position += transform.forward * speed * Time.deltaTime;
        }
    }

    private class ProtectionSuzuProjectile : MonoBehaviour
    {
        private SurvivorClassManager owner;
        private Vector3 direction;
        private float speed;
        private float life;
        private float radius;
        private float invulnDuration;

        public void Initialize(SurvivorClassManager manager, Vector3 dir, float projectileSpeed, float lifetime, float splashRadius, float duration)
        {
            owner = manager;
            direction = dir.normalized;
            speed = Mathf.Max(1f, projectileSpeed);
            life = Mathf.Max(0.1f, lifetime);
            radius = Mathf.Max(0.5f, splashRadius);
            invulnDuration = Mathf.Max(0.1f, duration);
        }

        private void Update()
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 start = transform.position;
            Vector3 end = start + direction * speed * Time.deltaTime;
            if (Physics.Linecast(start, end, out RaycastHit hit, owner.targetLayers, QueryTriggerInteraction.Ignore))
            {
                owner.suzuRadius = radius;
                owner.suzuInvulnerabilityDuration = invulnDuration;
                owner.ApplySuzuAtPosition(hit.point);
                Destroy(gameObject);
                return;
            }

            transform.position = end;
            life -= Time.deltaTime;
            if (life <= 0f)
            {
                owner.suzuRadius = radius;
                owner.suzuInvulnerabilityDuration = invulnDuration;
                owner.ApplySuzuAtPosition(transform.position);
                Destroy(gameObject);
            }
        }
    }

    private class SanctuaryHealZone : MonoBehaviour
    {
        private SurvivorClassManager owner;
        private float radius;
        private float duration;
        private int healPerSecond;
        private string allyTag;
        private bool logEvents;
        private float tick = 1.25f;
        private readonly HashSet<UnitHealth> healedAllies = new HashSet<UnitHealth>();

        public void Initialize(
            SurvivorClassManager zoneOwner,
            float zoneRadius,
            float zoneDuration,
            int healPerTick,
            float tickInterval,
            string allySurvivorTag,
            bool debugLogs)
        {
            owner = zoneOwner;
            radius = Mathf.Max(0.5f, zoneRadius);
            duration = Mathf.Max(0.1f, zoneDuration);
            healPerSecond = Mathf.Max(1, healPerTick);
            allyTag = allySurvivorTag;
            logEvents = debugLogs;
            tick = Mathf.Clamp(tickInterval, 1f, 1.5f);
            StartCoroutine(RunZoneCoroutine());
        }

        private IEnumerator RunZoneCoroutine()
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < hits.Length; i++)
                {
                    UnitHealth health = hits[i].GetComponentInParent<UnitHealth>();
                    if (health == null || health.IsDead || !health.CompareTag(allyTag))
                    {
                        continue;
                    }

                    if (health.currentHealth >= health.maxHealth)
                    {
                        continue;
                    }

                    Vector3 delta = health.transform.position - transform.position;
                    delta.y = 0f;
                    if (delta.sqrMagnitude > radius * radius)
                    {
                        continue;
                    }

                    int healed = health.Heal(healPerSecond);
                    if (healed > 0)
                    {
                        healedAllies.Add(health);
                    }
                }

                elapsed += tick;
                yield return new WaitForSeconds(tick);
            }

            if (logEvents)
            {
                Debug.Log("[SurvivorAbility] Sanctuary healed/protected " + healedAllies.Count + " allies");
            }

            Destroy(gameObject);
        }
    }
}
