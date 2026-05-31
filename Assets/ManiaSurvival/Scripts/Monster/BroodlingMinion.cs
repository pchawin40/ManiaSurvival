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

    public float moveSpeed = 3f;
    public float contactRadius = 0.95f;
    public int contactDamage = 2;
    public float lifetime = 9f;
    public float hatchDuration = 0.7f;
    public float contactInterval = 1.5f;
    public float postHatchBiteDelay = 0.2f;
    public int maxHealth = 5;
    public float fadeOutDuration = 0.35f;
    public float wanderStrength = 0.18f;

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
    private Renderer cachedRenderer;
    private WorldHealthBar healthBar;
    private readonly Dictionary<UnitHealth, float> nextBiteByTarget = new Dictionary<UnitHealth, float>();
    private Color activeTint = new Color(0.4f, 0.9f, 0.25f, 1f);
    private Color hatchTint = new Color(0.25f, 0.45f, 0.18f, 1f);

    public bool IsHatched => isHatched;
    public UnitHealth Health => unitHealth;

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
        float biteDelayAfterHatch = 0.2f)
    {
        owner = predatorOwner;
        targetLayers = layers;
        survivorTag = tag;
        lifetime = Mathf.Max(1f, lifeSeconds);
        lifeRemaining = lifetime;
        contactDamage = Mathf.Clamp(damage, 1, 2);
        moveSpeed = Mathf.Clamp(speed, 2.5f, 3.2f);
        hatchDuration = Mathf.Max(0f, hatchSeconds);
        hatchRemaining = hatchDuration;
        contactInterval = Mathf.Clamp(contactCooldown, 1.25f, 2f);
        postHatchBiteDelay = Mathf.Clamp(biteDelayAfterHatch, 0.15f, 0.35f);
        maxHealth = Mathf.Max(1, health);
        fadeOutDuration = Mathf.Max(0f, fadeSeconds);
        spawnTime = Time.time;
        globalBiteUnlockTime = spawnTime + hatchDuration + postHatchBiteDelay;
        isHatched = hatchDuration <= 0f;
        isExpiring = false;

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

        for (int typeIndex = 0; typeIndex < LegacyDamageComponentTypes.Length; typeIndex++)
        {
            RemoveLegacyComponentsOfType(minionObject, LegacyDamageComponentTypes[typeIndex]);
        }

        RemoveLegacyComponentsByName(minionObject, "DamageOnTouch");
        RemoveLegacyComponentsByName(minionObject, "MonsterAttackDamage");
        RemoveLegacyComponentsByName(minionObject, "ContactDamage");
    }

    private static void RemoveLegacyComponentsOfType(GameObject minionObject, System.Type componentType)
    {
        if (componentType == null)
        {
            return;
        }

        Component[] components = minionObject.GetComponentsInChildren(componentType, true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] is BroodlingMinion)
            {
                continue;
            }

            DisableAndDestroyLegacyComponent(components[i]);
        }
    }

    private static void RemoveLegacyComponentsByName(GameObject minionObject, string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return;
        }

        MonoBehaviour[] behaviours = minionObject.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour is BroodlingMinion)
            {
                continue;
            }

            string behaviourTypeName = behaviour.GetType().Name;
            if (behaviourTypeName != typeName)
            {
                continue;
            }

            DisableAndDestroyLegacyComponent(behaviour);
        }
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

        Debug.Log("[SwarmDebug] Removed legacy damage component: " + component.GetType().Name
            + " from " + component.gameObject.name);
        Object.Destroy(component);
    }

    public static void EnsureBroodlingPhysics(GameObject minionObject)
    {
        if (minionObject == null)
        {
            return;
        }

        Collider[] colliders = minionObject.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].isTrigger = true;
        }

        Rigidbody body = minionObject.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }
    }

    public static void LogSpawnDiagnostics(GameObject minionObject, BroodlingMinion broodling, string sourceAbility, bool usedPrefab)
    {
        if (minionObject == null || broodling == null)
        {
            return;
        }

        StringBuilder componentList = new StringBuilder();
        MonoBehaviour[] behaviours = minionObject.GetComponentsInChildren<MonoBehaviour>(true);
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

        Debug.Log("[SwarmDebug] Spawned broodling " + minionObject.name + " via " + sourceAbility
            + " (prefab=" + (usedPrefab ? "yes" : "runtime") + ")");
        Debug.Log("[SwarmDebug] Components on " + minionObject.name + ": " + componentList);
        Debug.Log("[SwarmDebug] Damage settings: BroodlingMinion.damage=" + broodling.contactDamage
            + ", interval=" + broodling.contactInterval.ToString("0.00")
            + "s, speed=" + broodling.moveSpeed.ToString("0.0")
            + ", postHatchBiteDelay=" + broodling.postHatchBiteDelay.ToString("0.00") + "s");
    }

    private void EnsureHealth()
    {
        unitHealth = GetComponent<UnitHealth>();
        if (unitHealth == null)
        {
            unitHealth = gameObject.AddComponent<UnitHealth>();
        }

        unitHealth.maxHealth = maxHealth;
        unitHealth.currentHealth = maxHealth;
        unitHealth.destroyOnDeath = true;
        unitHealth.disableOnDeath = false;
        unitHealth.showFloatingCombatText = true;
        unitHealth.logSurvivorDamage = false;
        unitHealth.logAbilityEffects = false;
        unitHealth.enableHitDeathAnimations = false;
        unitHealth.onDeath.AddListener(HandleBroodlingDeath);
    }

    private void EnsureHealthBar()
    {
        healthBar = MiniWorldHealthBarBuilder.Attach(unitHealth, 0.52f, 0.75f);
    }

    private void HandleBroodlingDeath()
    {
        if (deathHandled)
        {
            return;
        }

        deathHandled = true;
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

        lifeRemaining -= Time.deltaTime;
        if (lifeRemaining <= 0f)
        {
            BeginExpire("lifetime");
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
        if (target == null || target.IsDead || !isHatched || isExpiring)
        {
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
        GameObject source = owner != null ? owner.gameObject : gameObject;
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
            return;
        }

        Material mat = cachedRenderer.material;
        mat.color = isHatched ? activeTint : hatchTint;
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
        minion.SetActive(false);

        float safeScale = Mathf.Clamp(scale, 0.45f, 0.65f);
        minion.transform.localScale = new Vector3(safeScale, safeScale, safeScale);

        Collider col = minion.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        Renderer renderer = minion.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = tint;
            renderer.material = mat;
        }

        minion.AddComponent<BroodlingMinion>();
        SanitizeLegacyComponents(minion);
        EnsureBroodlingPhysics(minion);
        return minion;
    }
}
