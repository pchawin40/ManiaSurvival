using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class UnitHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Death")]
    public bool destroyOnDeath = false;
    public bool disableOnDeath = false;

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onDeath;

    public bool IsDead { get; private set; }

    [Header("Temporary Protection")]
    [Tooltip("Multiplies incoming damage while protection is active. 0.7 = 30% less damage taken.")]
    public float temporaryDamageMultiplier = 1f;

    private float temporaryProtectionEndTime;

    [Header("Debug")]
    [Tooltip("Logs a clear damage line for survivors to improve combat readability while testing.")]
    public bool logSurvivorDamage = true;
    [Tooltip("Logs generic ability damage/heal lines for stability debugging.")]
    public bool logAbilityEffects = true;

    [Header("Combat Feedback")]
    public bool showFloatingCombatText = true;
    public bool showHitFlash = true;
    public Color damageTextColor = new Color(1f, 0.42f, 0.12f, 1f);
    public Color healTextColor = new Color(0.25f, 0.95f, 0.35f, 1f);

    private UnitHealthVisualFeedback visualFeedback;

    void Awake()
    {
        currentHealth = maxHealth;
        IsDead = false;

        if (showHitFlash)
        {
            visualFeedback = GetComponent<UnitHealthVisualFeedback>();
            if (visualFeedback == null)
            {
                visualFeedback = gameObject.AddComponent<UnitHealthVisualFeedback>();
            }
        }
    }

    public void ApplyTemporaryDamageReduction(float reductionPercent, float duration)
    {
        reductionPercent = Mathf.Clamp01(reductionPercent);
        temporaryDamageMultiplier = Mathf.Clamp(1f - reductionPercent, 0.05f, 1f);
        temporaryProtectionEndTime = Mathf.Max(
            temporaryProtectionEndTime,
            Time.time + Mathf.Max(0.05f, duration));

        if (logAbilityEffects)
        {
            Debug.Log("[Protection] Applied " + (reductionPercent * 100f).ToString("0")
                + "% damage reduction for " + duration.ToString("0.0") + "s on " + name);
        }
    }

    public void TakeDamage(int amount, GameObject damageSource = null)
    {
        if (IsDead) return;
        if (amount <= 0) return;

        if (Time.time > temporaryProtectionEndTime)
        {
            temporaryDamageMultiplier = 1f;
        }

        if (temporaryDamageMultiplier < 0.999f)
        {
            amount = Mathf.RoundToInt(amount * temporaryDamageMultiplier);
            if (amount <= 0)
            {
                return;
            }
        }

        int previousHealth = currentHealth;
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        onDamaged?.Invoke();

        if (showFloatingCombatText)
        {
            FloatingCombatText.Spawn(transform.position, "-" + amount, damageTextColor);
        }

        if (showHitFlash && visualFeedback != null)
        {
            visualFeedback.FlashDamage();
        }

        if (logAbilityEffects)
        {
            string sourceName = damageSource != null ? damageSource.name : "Unknown";
            Debug.Log("[AbilityEffect] Damaged " + name + " for " + amount + " by " + sourceName + ".");
        }

        if (logSurvivorDamage && CompareTag("Survivor"))
        {
            string sourceName = damageSource != null ? damageSource.name : "Unknown";
            Debug.Log("[SurvivorHit] " + name + " took " + amount + " damage from " + sourceName +
                      " (" + previousHealth + " -> " + currentHealth + ").");
        }

        if (currentHealth <= 0)
        {
            Die(damageSource);
        }
    }

    public int Heal(int amount)
    {
        if (IsDead)
        {
            if (logAbilityEffects)
            {
                Debug.Log("[AbilityBlock] Heal blocked: target is dead");
            }

            return 0;
        }

        if (amount <= 0) return 0;

        int before = currentHealth;
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        int actualHeal = currentHealth - before;
        if (actualHeal <= 0)
        {
            return 0;
        }

        if (showFloatingCombatText)
        {
            FloatingCombatText.Spawn(transform.position, "+" + actualHeal, healTextColor);
        }

        if (showHitFlash && visualFeedback != null)
        {
            visualFeedback.FlashHeal();
        }

        if (logAbilityEffects)
        {
            Debug.Log("[AbilityEffect] Healed " + name + " for " + actualHeal + ".");
        }

        return actualHeal;
    }

    public void ResetHealth()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        IsDead = false;
        currentHealth = maxHealth;
        temporaryDamageMultiplier = 1f;
        temporaryProtectionEndTime = 0f;
    }

    public float GetHealthPercent()
    {
        if (maxHealth <= 0) return 0f;
        return (float)currentHealth / maxHealth;
    }

    private void Die(GameObject damageSource = null)
    {
        if (IsDead) return;

        IsDead = true;
        currentHealth = 0;
        temporaryDamageMultiplier = 1f;
        temporaryProtectionEndTime = 0f;

        string sourceLabel = damageSource != null ? damageSource.name : "unknown";
        Debug.Log("[UnitHealth] " + name + " died from " + sourceLabel);

        onDeath?.Invoke();

        if (CompareTag("Survivor"))
        {
            ManiaGameManager manager = ManiaGameManager.Instance;
            if (manager != null)
            {
                manager.ReportSurvivorDeath(this, damageSource);
            }
        }

        DeathMessageManager feed = DeathMessageManager.Instance;
        if (feed == null)
        {
            feed = FindFirstObjectByType<DeathMessageManager>();
        }

        if (feed != null)
        {
            feed.ShowDeathMessage(this, damageSource);
        }

        if (disableOnDeath)
        {
            gameObject.SetActive(false);
            return;
        }

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}

[DisallowMultipleComponent]
public class UnitHealthVisualFeedback : MonoBehaviour
{
    [Header("Flash")]
    public Color damageFlashColor = new Color(1f, 0.35f, 0.15f, 1f);
    public Color healFlashColor = new Color(0.25f, 0.95f, 0.55f, 1f);
    public float flashDuration = 0.1f;

    private readonly System.Collections.Generic.List<RendererState> rendererStates
        = new System.Collections.Generic.List<RendererState>();
    private Coroutine flashRoutine;

    private struct RendererState
    {
        public Renderer renderer;
        public Color originalColor;
    }

    public void FlashDamage()
    {
        StartFlash(damageFlashColor);
    }

    public void FlashHeal()
    {
        StartFlash(healFlashColor);
    }

    private void CacheRenderers()
    {
        rendererStates.Clear();
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer)
            {
                continue;
            }

            Material mat = renderer.material;
            if (mat == null || !mat.HasProperty("_Color"))
            {
                continue;
            }

            rendererStates.Add(new RendererState
            {
                renderer = renderer,
                originalColor = mat.color
            });
        }
    }

    private void StartFlash(Color flashColor)
    {
        if (rendererStates.Count == 0)
        {
            CacheRenderers();
        }

        if (rendererStates.Count == 0)
        {
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine(flashColor));
    }

    private System.Collections.IEnumerator FlashRoutine(Color flashColor)
    {
        for (int i = 0; i < rendererStates.Count; i++)
        {
            Renderer renderer = rendererStates[i].renderer;
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = flashColor;
            }
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < rendererStates.Count; i++)
        {
            RendererState state = rendererStates[i];
            if (state.renderer != null && state.renderer.material != null)
            {
                state.renderer.material.color = state.originalColor;
            }
        }

        flashRoutine = null;
    }
}

public class FloatingCombatText : MonoBehaviour
{
    public float riseSpeed = 1.4f;
    public float lifetime = 1f;

    private TextMeshPro textMesh;
    private Color startColor;
    private float spawnTime;

    public static void Spawn(Vector3 worldPosition, string text, Color color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        GameObject host = new GameObject("FloatingCombatText");
        host.transform.position = worldPosition + Vector3.up * 1.6f;
        FloatingCombatText floater = host.AddComponent<FloatingCombatText>();
        floater.Initialize(text, color);
    }

    private void Initialize(string text, Color color)
    {
        spawnTime = Time.time;
        startColor = color;
        textMesh = gameObject.AddComponent<TextMeshPro>();
        textMesh.text = text;
        textMesh.fontSize = 4.5f;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = color;
        textMesh.rectTransform.sizeDelta = new Vector2(4f, 2f);
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }

        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        float elapsed = Time.time - spawnTime;
        float t = lifetime <= 0f ? 1f : Mathf.Clamp01(elapsed / lifetime);
        if (textMesh != null)
        {
            Color color = startColor;
            color.a = Mathf.Lerp(startColor.a, 0f, t);
            textMesh.color = color;
        }

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
