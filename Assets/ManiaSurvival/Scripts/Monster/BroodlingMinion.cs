using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Short-lived brood minion that chases survivors and deals light contact chip damage.
/// Killable by survivor abilities via UnitHealth.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(UnitHealth))]
public class BroodlingMinion : MonoBehaviour
{
    private static readonly System.Type[] LegacyDamageComponentTypes =
    {
        typeof(TrackingMinion),
        typeof(MonsterAttack)
    };

    public float moveSpeed = 2.6f;
    public float contactRadius = 0.95f;
    public int contactDamage = 2;
    public float lifetime = 20f;
    public float hatchDuration = 0.7f;
    public float contactInterval = 1.6f;
    public float postHatchBiteDelay = 0.35f;
    public int maxHealth = 5;
    public float fadeOutDuration = 0.35f;
    public float wanderStrength = 0.12f;

    private PredatorClassManager owner;
    private UnitHealth unitHealth;
    private LayerMask targetLayers;
    private string survivorTag;
    private float lifeRemaining;
    private float hatchRemaining;
    private float spawnTime;
    private float globalBiteUnlockTime;
    private bool isHatched;
    private bool isExpiring;
    private bool deathHandled;
    private bool loggedHatchBiteSkip;
    private Renderer cachedRenderer;
    private readonly Dictionary<UnitHealth, float> nextBiteByTarget = new Dictionary<UnitHealth, float>();
    private Color activeTint = new Color(0.35f, 0.95f, 0.22f, 1f);
    private Color hatchTint = new Color(0.25f, 0.55f, 0.15f, 1f);

    public bool IsHatched => isHatched;
    public bool IsExpiring => isExpiring;
    public UnitHealth Health => unitHealth;
    public float MaxLifetime => lifetime;
    public float RemainingLifetime => lifeRemaining;
    public float Lifetime01 => lifetime > 0f ? Mathf.Clamp01(lifeRemaining / lifetime) : 0f;

    public void Initialize(
        PredatorClassManager predatorOwner,
        LayerMask layers,
        string tag,
        float lifeSeconds,
        int damage,
        float speed,
        float hatchSeconds,
        float contactCooldown,
        int health,
        float fadeSeconds,
        int spawnIndex,
        float biteDelayAfterHatch = 0.35f)
    {
        owner = predatorOwner;
        targetLayers = layers;
        survivorTag = tag;
        lifetime = Mathf.Max(1f, lifeSeconds);
        lifeRemaining = lifetime;
        contactDamage = Mathf.Clamp(damage, 1, 2);
        moveSpeed = Mathf.Clamp(speed, 2f, 3.5f);
        hatchDuration = Mathf.Max(0f, hatchSeconds);
        hatchRemaining = hatchDuration;
        contactInterval = contactCooldown > 0f ? contactCooldown : 1.6f;
        contactInterval = Mathf.Clamp(contactInterval, 1.6f, 2.5f);
        postHatchBiteDelay = Mathf.Clamp(biteDelayAfterHatch, 0.1f, 1f);
        maxHealth = Mathf.Max(1, health);
        fadeOutDuration = Mathf.Max(0f, fadeSeconds);
        spawnTime = Time.time;
        globalBiteUnlockTime = spawnTime + hatchDuration + postHatchBiteDelay;
        isHatched = hatchDuration <= 0f;
        isExpiring = false;
        loggedHatchBiteSkip = false;

        SanitizeLegacyComponents(gameObject);
        gameObject.name = "Broodling_" + Mathf.Max(1, spawnIndex).ToString("00");
        EnsureHealth();
        EnsureHealthBar();
        EnsureBroodlingPhysics(gameObject);
        cachedRenderer = GetComponentInChildren<Renderer>();
        ApplyHatchVisual();
        owner?.RegisterBroodling(this);
    }

    public static void SanitizeLegacyComponents(GameObject minionObject)
    {
        if (minionObject == null)
        {
            return;
        }

        int removedCount = 0;
        for (int typeIndex = 0; typeIndex < LegacyDamageComponentTypes.Length; typeIndex++)
        {
            removedCount += RemoveLegacyComponentsOfType(minionObject, LegacyDamageComponentTypes[typeIndex]);
        }

        removedCount += RemoveLegacyComponentsByName(minionObject, "DamageOnTouch");
        removedCount += RemoveLegacyComponentsByName(minionObject, "MonsterAttackDamage");
        removedCount += RemoveLegacyComponentsByName(minionObject, "ContactDamage");

        if (removedCount <= 0)
        {
            Debug.Log("[SwarmSanitize] No legacy damage components found on " + minionObject.name + ".");
        }
    }

    private static int RemoveLegacyComponentsOfType(GameObject minionObject, System.Type componentType)
    {
        if (componentType == null)
        {
            return 0;
        }

        int removed = 0;
        Component[] components = minionObject.GetComponentsInChildren(componentType, true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] is BroodlingMinion)
            {
                continue;
            }

            DisableAndDestroyLegacyComponent(components[i]);
            removed++;
        }

        return removed;
    }

    private static int RemoveLegacyComponentsByName(GameObject minionObject, string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return 0;
        }

        int removed = 0;
        MonoBehaviour[] behaviours = minionObject.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour is BroodlingMinion)
            {
                continue;
            }

            if (behaviour.GetType().Name != typeName)
            {
                continue;
            }

            DisableAndDestroyLegacyComponent(behaviour);
            removed++;
        }

        return removed;
    }

    private static void DisableAndDestroyLegacyComponent(Component component)
    {
        if (component == null)
        {
            return;
        }

        if (component is Behaviour behaviour)
        {
            behaviour.enabled = false;
        }

        Debug.Log("[SwarmSanitize] Removed legacy component " + component.GetType().Name
            + " from " + component.gameObject.name + ".");
        Object.Destroy(component);
    }

    public static void EnsureBroodlingPhysics(GameObject minionObject)
    {
        if (minionObject == null)
        {
            return;
        }

        Collider[] colliders = minionObject.GetComponentsInChildren<Collider>(true);
        if (colliders.Length > 1)
        {
            Debug.LogWarning("[SwarmSpawn] Multiple colliders on " + minionObject.name + " — keeping one trigger collider.");
        }

        Collider primary = null;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null)
            {
                continue;
            }

            if (primary == null)
            {
                primary = colliders[i];
                primary.isTrigger = false;
                continue;
            }

            Object.Destroy(colliders[i]);
        }

        if (primary == null)
        {
            SphereCollider added = minionObject.AddComponent<SphereCollider>();
            added.isTrigger = false;
            added.radius = 0.45f;
        }

        Rigidbody body = minionObject.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }
    }

    public static Material CreateBroodlingMaterial(Color tint)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material mat = new Material(shader);
        mat.color = tint;
        return mat;
    }

    private void EnsureHealth()
    {
        Debug.Log("[SwarmHealth] Ensuring health for " + gameObject.name + ".");

        unitHealth = GetComponent<UnitHealth>();
        if (unitHealth == null)
        {
            unitHealth = gameObject.AddComponent<UnitHealth>();
            Debug.Log("[SwarmHealth] Added UnitHealth " + maxHealth + "/" + maxHealth + " to " + gameObject.name + ".");
        }

        if (unitHealth == null)
        {
            Debug.LogWarning("[SwarmBarsERROR] Missing UnitHealth on " + gameObject.name + ", health setup skipped.");
            return;
        }

        unitHealth.maxHealth = maxHealth;
        unitHealth.currentHealth = maxHealth;
        unitHealth.destroyOnDeath = true;
        unitHealth.disableOnDeath = false;
        unitHealth.showFloatingCombatText = true;
        unitHealth.logSurvivorDamage = false;
        unitHealth.logAbilityEffects = false;
        unitHealth.enableHitDeathAnimations = false;

        if (unitHealth.onDeath == null)
        {
            unitHealth.onDeath = new UnityEngine.Events.UnityEvent();
        }

        unitHealth.onDeath.RemoveListener(HandleBroodlingDeath);
        unitHealth.onDeath.AddListener(HandleBroodlingDeath);

        Debug.Log("[SwarmSpawn] Broodling HP initialized: " + unitHealth.currentHealth + "/" + unitHealth.maxHealth
            + " on " + gameObject.name);
    }

    private void EnsureHealthBar()
    {
        if (owner != null && !owner.showBroodlingHealthBars)
        {
            return;
        }

        if (unitHealth == null)
        {
            Debug.LogWarning("[SwarmBarsERROR] Missing UnitHealth on " + gameObject.name + ", bars skipped.");
            return;
        }

        BroodlingOverheadBars.Attach(this, unitHealth, owner);
    }

    private void HandleBroodlingDeath()
    {
        if (deathHandled)
        {
            return;
        }

        deathHandled = true;
        owner?.UnregisterBroodling(this);
        Debug.Log("[SwarmBrood] " + gameObject.name + " destroyed.");
        SurvivorAbilityFeelVfx.SpawnBroodlingDeathPop(transform.position);
    }

    private void OnDestroy()
    {
        if (unitHealth != null)
        {
            unitHealth.onDeath.RemoveListener(HandleBroodlingDeath);
        }

        owner?.UnregisterBroodling(this);
    }

    private void Update()
    {
        if (unitHealth != null && unitHealth.IsDead)
        {
            return;
        }

        if (isExpiring)
        {
            return;
        }

        if (!isHatched)
        {
            hatchRemaining -= Time.deltaTime;
            if (hatchRemaining <= 0f)
            {
                isHatched = true;
                globalBiteUnlockTime = Time.time + postHatchBiteDelay;
                ApplyHatchVisual();
            }

            return;
        }

        lifeRemaining -= Time.deltaTime;
        if (lifeRemaining <= 0f)
        {
            BeginExpire("lifetime");
            return;
        }

        UnitHealth target = FindClosestSurvivor();
        if (target == null)
        {
            return;
        }

        Vector3 dir = target.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            Vector3 wander = new Vector3(
                Mathf.Sin(Time.time * 2.4f + spawnTime) * wanderStrength,
                0f,
                Mathf.Cos(Time.time * 2.1f + spawnTime) * wanderStrength);
            Vector3 moveDir = (dir.normalized + wander).normalized;
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }

        if (dir.sqrMagnitude <= contactRadius * contactRadius)
        {
            TryApplyContactDamage(target);
        }
    }

    private void TryApplyContactDamage(UnitHealth target)
    {
        if (target == null || target.IsDead || isExpiring)
        {
            return;
        }

        if (!isHatched)
        {
            if (!loggedHatchBiteSkip)
            {
                loggedHatchBiteSkip = true;
                Debug.Log("[SwarmBrood] Bite skipped: still hatching (" + gameObject.name + ").");
            }

            return;
        }

        if (Time.time < globalBiteUnlockTime)
        {
            return;
        }

        if (nextBiteByTarget.TryGetValue(target, out float nextAllowed) && Time.time < nextAllowed)
        {
            return;
        }

        nextBiteByTarget[target] = Time.time + contactInterval;
        GameObject source = gameObject;
        target.TakeDamage(contactDamage, source);
        Debug.Log("[SwarmBrood] " + gameObject.name + " bit " + target.name + " for " + contactDamage
            + " damage. Target HP: " + target.currentHealth + "/" + target.maxHealth
            + ". Next bite in " + contactInterval.ToString("0.00") + "s.");
    }

    private void BeginExpire(string reason)
    {
        if (isExpiring)
        {
            return;
        }

        isExpiring = true;
        owner?.UnregisterBroodling(this);
        float lived = Time.time - spawnTime;
        Debug.Log("[SwarmBrood] Broodling expired after " + lived.ToString("0.0") + " seconds (" + reason + ")");

        if (fadeOutDuration <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        StartCoroutine(FadeOutAndDestroyRoutine());
    }

    private IEnumerator FadeOutAndDestroyRoutine()
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            transform.localScale = startScale * (1f - t);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void ApplyHatchVisual()
    {
        if (cachedRenderer == null)
        {
            cachedRenderer = GetComponentInChildren<Renderer>();
        }

        if (cachedRenderer == null)
        {
            return;
        }

        cachedRenderer.sharedMaterial = CreateBroodlingMaterial(isHatched ? activeTint : hatchTint);
    }

    private UnitHealth FindClosestSurvivor()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 10f, targetLayers, QueryTriggerInteraction.Ignore);
        UnitHealth best = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth health = hits[i].GetComponentInParent<UnitHealth>();
            if (health == null || health.IsDead || !health.CompareTag(survivorTag))
            {
                continue;
            }

            if (owner != null && health.gameObject == owner.gameObject)
            {
                continue;
            }

            float sqr = (health.transform.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                best = health;
                bestSqr = sqr;
            }
        }

        return best;
    }

    public static bool IsBroodlingUnit(UnitHealth health)
    {
        return health != null && health.GetComponent<BroodlingMinion>() != null;
    }

    public static GameObject SpawnRuntimeCapsule(Vector3 position, Color tint, float scale)
    {
        GameObject minion = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        minion.name = "Broodling_Runtime";
        minion.transform.position = position;

        float safeScale = Mathf.Clamp(scale, 0.45f, 0.65f);
        minion.transform.localScale = new Vector3(safeScale, safeScale, safeScale);

        Collider col = minion.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false;
        }

        Renderer renderer = minion.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateBroodlingMaterial(tint);
        }

        return minion;
    }
}
