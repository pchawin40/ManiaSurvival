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
    [Header("Class")]
    public SurvivorClass activeClass = SurvivorClass.Medic;

    [Header("Common")]
    public string survivorTag = "Survivor";
    public string[] predatorTags = { "Monster", "Predator" };
    public LayerMask targetLayers = ~0;
    public bool showDebugLogs = true;

    [Header("Animator Triggers")]
    public string primaryTrigger = "Attack";
    public string ability2Trigger = "Ability2";
    public string ability3Trigger = "Ability3";
    public string ultimateTrigger = "Ultimate";

    [Header("Medic - Biotic Dart")]
    public GameObject bioticDartProjectilePrefab;
    public float bioticDartSpeed = 18f;
    public float bioticDartLifetime = 2f;
    public float bioticDartRange = 20f;
    public int bioticDartAllyHeal = 18;
    public int bioticDartPredatorDamage = 6;

    [Header("Medic - Regen Burst")]
    public float regenBurstDuration = 5f;
    public float regenBurstRadius = 6f;
    public int regenBurstHealPerTick = 4;
    public float regenBurstTickInterval = 0.5f;

    [Header("Medic - Guardian Angel")]
    public float guardianAngelRange = 18f;
    public float guardianAngelDashDuration = 0.2f;
    public float guardianAngelStopDistance = 1.25f;

    [Header("Medic - Immortality Field")]
    public GameObject immortalityFieldPrefab;
    public float immortalityFieldDuration = 6f;
    public float immortalityFieldRadius = 5f;
    [Range(0.01f, 0.5f)] public float immortalityMinHealthPercent = 0.10f;

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
    public float ampItUpMultiplier = 1.4f;
    public float ampItUpDuration = 3f;

    [Header("Warden - Sound Barrier")]
    public float soundBarrierRadius = 8f;
    public int soundBarrierBonusHealth = 35;
    public float soundBarrierDuration = 4f;

    [Header("Weaver - Healing Blossom")]
    public GameObject healingBlossomProjectilePrefab;
    public float healingBlossomSpeed = 12f;
    public float healingBlossomTurnSpeed = 10f;
    public float healingBlossomLifetime = 3f;
    public int healingBlossomHealAmount = 20;
    public float healingBlossomTargetRange = 16f;

    [Header("Weaver - Life Grip")]
    public float lifeGripRange = 18f;
    public float lifeGripPullDuration = 0.25f;
    public float lifeGripEndOffset = 1.3f;
    public float lifeGripBubbleDuration = 1.0f;
    public GameObject lifeGripBubblePrefab;

    [Header("Weaver - Swift Step")]
    public float swiftStepDistance = 5f;
    public LayerMask swiftStepBlockerLayers = ~0;

    [Header("Weaver - Protection Suzu")]
    public GameObject suzuProjectilePrefab;
    public float suzuProjectileSpeed = 14f;
    public float suzuProjectileLifetime = 2.5f;
    public float suzuRadius = 4f;
    public float suzuInvulnerabilityDuration = 1f;

    private UnitHealth unitHealth;
    private CharacterController characterController;
    private Animator animator;

    private void Awake()
    {
        unitHealth = GetComponent<UnitHealth>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    public void ExecutePrimary()
    {
        Debug.Log($"[SurvivorClassManager] {activeClass} executing Primary Ability!");

        if (!CanExecute())
        {
            return;
        }

        TriggerAnimation(primaryTrigger);

        switch (activeClass)
        {
            case SurvivorClass.Medic:
                ExecuteMedicPrimary();
                break;
            case SurvivorClass.Warden:
                ExecuteWardenPrimary();
                break;
            case SurvivorClass.Weaver:
                ExecuteWeaverPrimary();
                break;
        }
    }

    public void ExecuteAbility2()
    {
        Debug.Log($"[SurvivorClassManager] {activeClass} executing Ability2!");

        if (!CanExecute())
        {
            return;
        }

        TriggerAnimation(ability2Trigger);

        switch (activeClass)
        {
            case SurvivorClass.Medic:
                StartCoroutine(MedicRegenBurstCoroutine());
                break;
            case SurvivorClass.Warden:
                StartCoroutine(WardenShieldBashCoroutine());
                break;
            case SurvivorClass.Weaver:
                StartCoroutine(WeaverLifeGripCoroutine());
                break;
        }
    }

    public void ExecuteAbility3()
    {
        Debug.Log($"[SurvivorClassManager] {activeClass} executing Ability3!");

        if (!CanExecute())
        {
            return;
        }

        TriggerAnimation(ability3Trigger);

        switch (activeClass)
        {
            case SurvivorClass.Medic:
                StartCoroutine(MedicGuardianAngelCoroutine());
                break;
            case SurvivorClass.Warden:
                ExecuteWardenAmpItUp();
                break;
            case SurvivorClass.Weaver:
                ExecuteWeaverSwiftStep();
                break;
        }
    }

    public void ExecuteUltimate()
    {
        Debug.Log($"[SurvivorClassManager] {activeClass} executing Ultimate Ability!");

        if (!CanExecute())
        {
            return;
        }

        TriggerAnimation(ultimateTrigger);

        switch (activeClass)
        {
            case SurvivorClass.Medic:
                ExecuteMedicImmortalityField();
                break;
            case SurvivorClass.Warden:
                ExecuteWardenSoundBarrier();
                break;
            case SurvivorClass.Weaver:
                ExecuteWeaverProtectionSuzu();
                break;
        }
    }

    private bool CanExecute()
    {
        if (unitHealth == null)
        {
            unitHealth = GetComponent<UnitHealth>();
        }

        return unitHealth != null && !unitHealth.IsDead;
    }

    private void TriggerAnimation(string triggerName)
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator != null && !string.IsNullOrEmpty(triggerName))
        {
            animator.SetTrigger(triggerName);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Medic
    // ═════════════════════════════════════════════════════════════════════════

    private void ExecuteMedicPrimary()
    {
        Vector3 direction = GetForwardDirection();
        Vector3 spawn = GetProjectileSpawnPosition();

        if (bioticDartProjectilePrefab != null)
        {
            GameObject obj = Instantiate(bioticDartProjectilePrefab, spawn, Quaternion.LookRotation(direction));
            BioticDartProjectile projectile = obj.GetComponent<BioticDartProjectile>();
            if (projectile == null)
            {
                projectile = obj.AddComponent<BioticDartProjectile>();
            }

            projectile.Initialize(this, direction, bioticDartSpeed, bioticDartLifetime, bioticDartRange);
            return;
        }

        // Fallback: line-of-sight ray.
        if (Physics.Raycast(spawn, direction, out RaycastHit hit, bioticDartRange, targetLayers, QueryTriggerInteraction.Ignore))
        {
            UnitHealth target = hit.collider.GetComponentInParent<UnitHealth>();
            ApplyBioticDartEffect(target);
        }
    }

    private IEnumerator MedicRegenBurstCoroutine()
    {
        float elapsed = 0f;
        float tick = Mathf.Max(0.1f, regenBurstTickInterval);

        while (elapsed < regenBurstDuration)
        {
            UnitHealth[] allies = FindAlliesInRange(transform.position, regenBurstRadius);
            for (int i = 0; i < allies.Length; i++)
            {
                if (allies[i] != null && !allies[i].IsDead)
                {
                    allies[i].Heal(regenBurstHealPerTick);
                }
            }

            yield return new WaitForSeconds(tick);
            elapsed += tick;
        }
    }

    private IEnumerator MedicGuardianAngelCoroutine()
    {
        UnitHealth ally = FindClosestAlly(transform.position, guardianAngelRange, requireDamaged: false);
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

    private void ExecuteMedicImmortalityField()
    {
        if (immortalityFieldPrefab == null)
        {
            return;
        }

        GameObject field = Instantiate(immortalityFieldPrefab, transform.position, Quaternion.identity);
        ImmortalityFieldZone zone = field.GetComponent<ImmortalityFieldZone>();
        if (zone == null)
        {
            zone = field.AddComponent<ImmortalityFieldZone>();
        }

        zone.Initialize(immortalityFieldRadius, immortalityFieldDuration, immortalityMinHealthPercent, survivorTag);
    }

    private void ApplyBioticDartEffect(UnitHealth target)
    {
        if (target == null || target.IsDead)
        {
            return;
        }

        if (IsAlly(target))
        {
            target.Heal(bioticDartAllyHeal);
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
        UnitHealth ally = FindClosestAlly(transform.position, healingBlossomTargetRange, requireDamaged: true);
        if (ally == null)
        {
            return;
        }

        Vector3 spawn = GetProjectileSpawnPosition();

        if (healingBlossomProjectilePrefab != null)
        {
            GameObject obj = Instantiate(healingBlossomProjectilePrefab, spawn, Quaternion.identity);
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

        TempInvulnerability inv = ally.gameObject.GetComponent<TempInvulnerability>();
        if (inv == null)
        {
            inv = ally.gameObject.AddComponent<TempInvulnerability>();
        }
        inv.Activate(lifeGripBubbleDuration);

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

    private void ExecuteWeaverSwiftStep()
    {
        Vector3 direction = characterController != null ? characterController.velocity : Vector3.zero;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = GetForwardDirection();
        }

        direction = direction.normalized;
        Vector3 start = transform.position;
        float radius = Mathf.Max(0.1f, characterController != null ? characterController.radius : 0.4f);

        if (Physics.SphereCast(start, radius, direction, out RaycastHit hit, swiftStepDistance, swiftStepBlockerLayers, QueryTriggerInteraction.Ignore))
        {
            float safeDistance = Mathf.Max(0f, hit.distance - radius);
            transform.position = start + direction * safeDistance;
            return;
        }

        transform.position = start + direction * swiftStepDistance;
    }

    private void ExecuteWeaverProtectionSuzu()
    {
        Vector3 spawn = GetProjectileSpawnPosition();
        Vector3 dir = GetForwardDirection();

        if (suzuProjectilePrefab != null)
        {
            GameObject obj = Instantiate(suzuProjectilePrefab, spawn, Quaternion.LookRotation(dir));
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

            TempInvulnerability inv = ally.gameObject.GetComponent<TempInvulnerability>();
            if (inv == null)
            {
                inv = ally.gameObject.AddComponent<TempInvulnerability>();
            }
            inv.Activate(suzuInvulnerabilityDuration);
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

            if (!result.Contains(health))
            {
                result.Add(health);
            }
        }

        if (!result.Contains(unitHealth))
        {
            result.Add(unitHealth);
        }

        return result.ToArray();
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

        public void Initialize(SurvivorClassManager manager, Vector3 dir, float moveSpeed, float life, float maxRange)
        {
            owner = manager;
            direction = dir.normalized;
            speed = Mathf.Max(1f, moveSpeed);
            lifetime = Mathf.Max(0.1f, life);
            range = Mathf.Max(1f, maxRange);
            distanceTravelled = 0f;
        }

        private void Update()
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
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

    private class ImmortalityFieldZone : MonoBehaviour
    {
        private float radius;
        private float duration;
        private float minPercent;
        private string allyTag;

        public void Initialize(float zoneRadius, float zoneDuration, float minimumHealthPercent, string allySurvivorTag)
        {
            radius = Mathf.Max(0.5f, zoneRadius);
            duration = Mathf.Max(0.1f, zoneDuration);
            minPercent = Mathf.Clamp(minimumHealthPercent, 0.01f, 0.5f);
            allyTag = allySurvivorTag;
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

                    int minHealth = Mathf.Max(1, Mathf.RoundToInt(health.maxHealth * minPercent));
                    if (health.currentHealth < minHealth)
                    {
                        health.currentHealth = minHealth;
                    }
                }

                elapsed += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            Destroy(gameObject);
        }
    }

    private class TempInvulnerability : MonoBehaviour
    {
        private UnitHealth health;
        private float endTime;
        private int lastHealth;

        private void Awake()
        {
            health = GetComponent<UnitHealth>();
            lastHealth = health != null ? health.currentHealth : 0;
        }

        public void Activate(float duration)
        {
            endTime = Mathf.Max(endTime, Time.time + Mathf.Max(0.05f, duration));
            if (health != null)
            {
                lastHealth = health.currentHealth;
            }
        }

        private void LateUpdate()
        {
            if (health == null || health.IsDead)
            {
                return;
            }

            if (Time.time <= endTime)
            {
                if (health.currentHealth < lastHealth)
                {
                    health.currentHealth = lastHealth;
                }
                else
                {
                    lastHealth = health.currentHealth;
                }

                return;
            }

            lastHealth = health.currentHealth;
        }
    }
}
