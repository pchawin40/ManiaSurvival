using UnityEngine;

/// <summary>
/// Optional health for destructible map props (trees, crates, generated obstacles).
/// Only objects with this component can be damaged by Tonic gas and similar effects.
/// </summary>
[DisallowMultipleComponent]
public class DestructiblePropHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 60;
    public int currentHealth = 60;

    [Header("Death")]
    public bool destroyOnDeath = true;

    [Header("Hit Feedback")]
    public bool flashOnHit = true;
    public Renderer hitFlashRenderer;
    public Color hitFlashColor = new Color(0.85f, 0.35f, 0.2f, 1f);
    public float hitFlashDuration = 0.12f;

    private Color originalColor;
    private bool hasOriginalColor;
    private float hitFlashEndTime;

    public bool IsAlive => currentHealth > 0;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, Mathf.Max(1, maxHealth));
        CacheRenderer();
    }

    private void Update()
    {
        if (!flashOnHit || !hasOriginalColor || hitFlashRenderer == null)
        {
            return;
        }

        if (Time.time >= hitFlashEndTime)
        {
            hitFlashRenderer.material.color = originalColor;
        }
    }

    public void TakeDamage(int amount, GameObject source = null)
    {
        if (!IsAlive || amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        ApplyHitFlash();

        if (currentHealth <= 0 && destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyHitFlash()
    {
        if (!flashOnHit)
        {
            return;
        }

        CacheRenderer();
        if (hitFlashRenderer == null)
        {
            return;
        }

        if (!hasOriginalColor)
        {
            originalColor = hitFlashRenderer.material.color;
            hasOriginalColor = true;
        }

        hitFlashRenderer.material.color = hitFlashColor;
        hitFlashEndTime = Time.time + Mathf.Max(0.01f, hitFlashDuration);
    }

    private void CacheRenderer()
    {
        if (hitFlashRenderer != null)
        {
            return;
        }

        hitFlashRenderer = GetComponentInChildren<Renderer>();
    }
}
