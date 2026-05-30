using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DynamicTerrainPropType
{
    Wall,
    Ramp,
    Platform,
    Crater,
    FireZone,
    SlowZone,
    VineWall,
    Rock,
    JumpPad,
    Bridge
}

/// <summary>
/// Temporary spawned terrain prop: blockers, ramps, zones, and visual marks.
/// Uses primitive fallbacks when no custom mesh is assigned.
/// </summary>
[DisallowMultipleComponent]
public class DynamicTerrainProp : MonoBehaviour
{
    private const int ObstacleLayer = 7;

    [Header("Prop")]
    public DynamicTerrainPropType propType = DynamicTerrainPropType.Crater;

    [Header("Lifetime")]
    public float lifetime = 12f;
    public float warningDuration = 0.35f;
    public bool fadeOutOnExpire = true;

    [Header("Collision")]
    public bool blocksMovement = true;

    [Header("Effects")]
    public bool affectsSurvivors = true;
    public bool affectsPredator;
    public float damagePerSecond;
    public float slowMultiplier = 0.65f;
    public float knockbackForce;

    [Header("Debug")]
    public bool enableDebugLogs;

    private float spawnTime;
    private float activeTime;
    private bool isActive;
    private bool isExpiring;
    private Collider[] solidColliders;
    private Collider zoneTrigger;
    private Renderer[] renderers;
    private Material[] runtimeMaterials;
    private readonly Dictionary<UnitHealth, float> pendingDamage = new Dictionary<UnitHealth, float>();
    private GameObject warningVisual;
    private Coroutine warningRoutine;

    public DynamicTerrainPropType PropType => propType;
    public float SpawnTimestamp => spawnTime;

    public void Configure(
        DynamicTerrainPropType type,
        float life,
        float warning,
        bool blockMovement,
        bool survivorFx,
        bool predatorFx,
        float dps,
        float slowMult,
        float knockback,
        bool fadeOut,
        bool debugLogs)
    {
        propType = type;
        lifetime = life;
        warningDuration = warning;
        blocksMovement = blockMovement;
        affectsSurvivors = survivorFx;
        affectsPredator = predatorFx;
        damagePerSecond = dps;
        slowMultiplier = slowMult;
        knockbackForce = knockback;
        fadeOutOnExpire = fadeOut;
        enableDebugLogs = debugLogs;
    }

    private void Awake()
    {
        spawnTime = Time.time;
        CacheComponents();
        EnsureFallbackVisualIfNeeded();
        ApplyInitialCollisionState();
    }

    private void Start()
    {
        if (warningDuration > 0f && NeedsActivationDelay())
        {
            warningRoutine = StartCoroutine(WarningThenActivateRoutine());
            return;
        }

        ActivateProp();
    }

    private void Update()
    {
        if (!isActive || isExpiring)
        {
            return;
        }

        if (IsZoneType())
        {
            TickZone(Time.deltaTime);
        }

        if (lifetime > 0f && Time.time - activeTime >= lifetime)
        {
            BeginExpire();
        }
    }

    private void CacheComponents()
    {
        solidColliders = GetComponentsInChildren<Collider>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    private bool NeedsActivationDelay()
    {
        return blocksMovement || damagePerSecond > 0f || slowMultiplier < 0.99f || knockbackForce > 0f;
    }

    private void ApplyInitialCollisionState()
    {
        for (int i = 0; i < solidColliders.Length; i++)
        {
            Collider col = solidColliders[i];
            if (col == null)
            {
                continue;
            }

            if (IsZoneType())
            {
                col.isTrigger = true;
                zoneTrigger = col;
                col.enabled = false;
            }
            else if (blocksMovement)
            {
                col.isTrigger = false;
                col.enabled = false;
                col.gameObject.layer = ObstacleLayer;
            }
            else
            {
                col.enabled = false;
            }
        }
    }

    private IEnumerator WarningThenActivateRoutine()
    {
        SpawnWarningVisual();
        yield return new WaitForSeconds(Mathf.Max(0.05f, warningDuration));
        if (warningVisual != null)
        {
            Destroy(warningVisual);
            warningVisual = null;
        }

        ActivateProp();
    }

    private void SpawnWarningVisual()
    {
        warningVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        warningVisual.name = "TerrainWarning";
        Collider warningCol = warningVisual.GetComponent<Collider>();
        if (warningCol != null)
        {
            Destroy(warningCol);
        }

        warningVisual.transform.SetParent(transform, false);
        warningVisual.transform.localPosition = Vector3.up * 0.05f;
        warningVisual.transform.localRotation = Quaternion.identity;

        Vector3 scale = transform.localScale;
        if (IsZoneType())
        {
            warningVisual.transform.localScale = new Vector3(scale.x * 1.05f, 0.08f, scale.z * 1.05f);
        }
        else
        {
            warningVisual.transform.localScale = new Vector3(scale.x * 1.05f, scale.y * 1.05f, scale.z * 1.05f);
        }

        ApplyColor(warningVisual, new Color(1f, 0.92f, 0.2f, 0.35f));
    }

    private void ActivateProp()
    {
        if (blocksMovement && !IsZoneType())
        {
            if (!TryActivateSolidColliders())
            {
                return;
            }
        }
        else
        {
            EnableZoneTrigger();
        }

        isActive = true;
        activeTime = Time.time;

        if (enableDebugLogs)
        {
            Debug.Log("[Terrain] Activated " + propType + " at " + transform.position);
        }
    }

    private bool TryActivateSolidColliders()
    {
        if (solidColliders == null || solidColliders.Length == 0)
        {
            isActive = true;
            activeTime = Time.time;
            return true;
        }

        for (int i = 0; i < solidColliders.Length; i++)
        {
            Collider col = solidColliders[i];
            if (col == null || col.isTrigger)
            {
                continue;
            }

            if (IsPlayerOverlapping(col))
            {
                Vector3 offset = transform.forward * 0.85f + transform.right * Random.Range(-0.35f, 0.35f);
                transform.position += offset;
                if (ArenaBounds.Instance != null)
                {
                    transform.position = ArenaBounds.Instance.ClampPosition(transform.position);
                }

                if (enableDebugLogs)
                {
                    Debug.Log("[Terrain] Wall activation delayed: player overlap — offset spawn.");
                }
            }
        }

        for (int i = 0; i < solidColliders.Length; i++)
        {
            Collider col = solidColliders[i];
            if (col != null && !col.isTrigger && blocksMovement)
            {
                col.enabled = true;
            }
        }

        return true;
    }

    private void EnableZoneTrigger()
    {
        if (zoneTrigger == null && solidColliders != null)
        {
            for (int i = 0; i < solidColliders.Length; i++)
            {
                if (solidColliders[i] != null)
                {
                    zoneTrigger = solidColliders[i];
                    break;
                }
            }
        }

        if (zoneTrigger != null)
        {
            zoneTrigger.enabled = true;
        }
    }

    private static bool IsPlayerOverlapping(Collider col)
    {
        Bounds bounds = col.bounds;
        bounds.Expand(0.15f);
        Collider[] hits = Physics.OverlapBox(bounds.center, bounds.extents, col.transform.rotation, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth health = hits[i].GetComponentInParent<UnitHealth>();
            if (health == null || health.IsDead)
            {
                continue;
            }

            if (health.CompareTag("Survivor") || health.CompareTag("Monster") || health.CompareTag("Predator"))
            {
                return true;
            }
        }

        return false;
    }

    private void TickZone(float deltaTime)
    {
        if (zoneTrigger == null || deltaTime <= 0f)
        {
            return;
        }

        float radius = Mathf.Max(0.35f, Mathf.Max(zoneTrigger.bounds.extents.x, zoneTrigger.bounds.extents.z));
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Collide);
        HashSet<UnitHealth> inside = new HashSet<UnitHealth>();

        for (int i = 0; i < hits.Length; i++)
        {
            UnitHealth unit = hits[i].GetComponentInParent<UnitHealth>();
            if (!IsValidTarget(unit))
            {
                continue;
            }

            inside.Add(unit);
            ApplySlowToUnit(unit);

            if (damagePerSecond <= 0f)
            {
                continue;
            }

            float pending = 0f;
            pendingDamage.TryGetValue(unit, out pending);
            pending += damagePerSecond * deltaTime;

            while (pending >= 1f)
            {
                int damage = Mathf.FloorToInt(pending);
                pending -= damage;
                unit.TakeDamage(damage, gameObject);
            }

            pendingDamage[unit] = pending;
        }

        List<UnitHealth> stale = null;
        foreach (KeyValuePair<UnitHealth, float> pair in pendingDamage)
        {
            if (pair.Key == null || pair.Key.IsDead || !inside.Contains(pair.Key))
            {
                stale ??= new List<UnitHealth>();
                stale.Add(pair.Key);
            }
        }

        if (stale != null)
        {
            for (int i = 0; i < stale.Count; i++)
            {
                pendingDamage.Remove(stale[i]);
            }
        }
    }

    private bool IsValidTarget(UnitHealth unit)
    {
        if (unit == null || unit.IsDead)
        {
            return false;
        }

        if (unit.CompareTag("Survivor"))
        {
            return affectsSurvivors;
        }

        if (unit.CompareTag("Monster") || unit.CompareTag("Predator"))
        {
            return affectsPredator;
        }

        return false;
    }

    private void ApplySlowToUnit(UnitHealth unit)
    {
        if (slowMultiplier >= 0.99f)
        {
            return;
        }

        SurvivorMovement survivor = unit.GetComponent<SurvivorMovement>();
        if (survivor != null)
        {
            survivor.ApplyTemporarySpeedMultiplier(slowMultiplier, 0.75f);
        }

        OfflineSurvivorBotAI bot = unit.GetComponent<OfflineSurvivorBotAI>();
        if (bot != null)
        {
            bot.ApplyTemporarySpeedMultiplier(slowMultiplier, 0.75f);
        }

        MonsterPlayerMovement predatorMove = unit.GetComponent<MonsterPlayerMovement>();
        if (predatorMove != null)
        {
            predatorMove.SetAbilitySpeedMultiplier(slowMultiplier);
        }
    }

    private bool IsZoneType()
    {
        return propType == DynamicTerrainPropType.FireZone
            || propType == DynamicTerrainPropType.SlowZone
            || propType == DynamicTerrainPropType.Crater;
    }

    private void BeginExpire()
    {
        if (isExpiring)
        {
            return;
        }

        isExpiring = true;
        DynamicTerrainSpawner.NotifyPropRemoved(this);

        if (fadeOutOnExpire && renderers != null && renderers.Length > 0)
        {
            StartCoroutine(FadeOutAndDestroy(0.45f));
            return;
        }

        Destroy(gameObject);
    }

    private IEnumerator FadeOutAndDestroy(float fadeDuration)
    {
        CaptureRuntimeMaterials();
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            SetMaterialAlpha(alpha);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void CaptureRuntimeMaterials()
    {
        if (renderers == null)
        {
            return;
        }

        runtimeMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            runtimeMaterials[i] = renderers[i].material;
        }
    }

    private void SetMaterialAlpha(float alpha)
    {
        if (runtimeMaterials == null)
        {
            return;
        }

        for (int i = 0; i < runtimeMaterials.Length; i++)
        {
            Material mat = runtimeMaterials[i];
            if (mat == null)
            {
                continue;
            }

            Color color = mat.color;
            color.a = alpha;
            mat.color = color;
        }
    }

    private void EnsureFallbackVisualIfNeeded()
    {
        if (renderers != null && renderers.Length > 0)
        {
            return;
        }

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "TerrainBody";
        body.transform.SetParent(transform, false);
        body.transform.localPosition = Vector3.up * 0.5f;
        body.transform.localScale = Vector3.one;
        ApplyTypeVisual(body);
        CacheComponents();
        ApplyInitialCollisionState();
    }

    private void ApplyTypeVisual(GameObject body)
    {
        switch (propType)
        {
            case DynamicTerrainPropType.Ramp:
                body.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
                ApplyColor(body, new Color(0.55f, 0.52f, 0.48f));
                break;
            case DynamicTerrainPropType.Platform:
            case DynamicTerrainPropType.Bridge:
                body.transform.localScale = new Vector3(1f, 0.25f, 1f);
                ApplyColor(body, new Color(0.5f, 0.48f, 0.44f));
                break;
            case DynamicTerrainPropType.Crater:
                Destroy(body.GetComponent<Collider>());
                body = ReplaceWithCylinder(body, new Color(0.12f, 0.1f, 0.1f, 0.75f), 0.06f);
                break;
            case DynamicTerrainPropType.FireZone:
                Destroy(body.GetComponent<Collider>());
                body = ReplaceWithCylinder(body, new Color(1f, 0.35f, 0.08f, 0.45f), 0.05f);
                break;
            case DynamicTerrainPropType.SlowZone:
                Destroy(body.GetComponent<Collider>());
                body = ReplaceWithCylinder(body, new Color(0.25f, 0.45f, 0.95f, 0.4f), 0.05f);
                break;
            case DynamicTerrainPropType.VineWall:
                ApplyColor(body, new Color(0.2f, 0.55f, 0.18f));
                break;
            case DynamicTerrainPropType.Rock:
                Destroy(body);
                body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                body.name = "TerrainBody";
                body.transform.SetParent(transform, false);
                body.transform.localPosition = Vector3.up * 0.45f;
                body.transform.localScale = Vector3.one * 0.9f;
                ApplyColor(body, new Color(0.45f, 0.47f, 0.5f));
                break;
            case DynamicTerrainPropType.JumpPad:
                Destroy(body.GetComponent<Collider>());
                body = ReplaceWithCylinder(body, new Color(0.2f, 0.85f, 1f, 0.65f), 0.08f);
                break;
            default:
                ApplyColor(body, new Color(0.52f, 0.5f, 0.46f));
                break;
        }
    }

    private GameObject ReplaceWithCylinder(GameObject oldBody, Color tint, float height)
    {
        Destroy(oldBody);
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = "TerrainBody";
        cylinder.transform.SetParent(transform, false);
        cylinder.transform.localPosition = Vector3.up * height;
        cylinder.transform.localScale = new Vector3(1f, height, 1f);
        ApplyColor(cylinder, tint);
        return cylinder;
    }

    private static void ApplyColor(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        renderer.material = mat;
    }

    private void OnDestroy()
    {
        if (warningRoutine != null)
        {
            StopCoroutine(warningRoutine);
        }

        DynamicTerrainSpawner.NotifyPropRemoved(this);
    }
}
