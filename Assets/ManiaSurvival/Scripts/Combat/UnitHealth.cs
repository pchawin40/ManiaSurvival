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

    [Header("Debug")]
    [Tooltip("Logs a clear damage line for survivors to improve combat readability while testing.")]
    public bool logSurvivorDamage = true;

    void Awake()
    {
        currentHealth = maxHealth;
        IsDead = false;
    }

    public void TakeDamage(int amount, GameObject damageSource = null)
    {
        if (IsDead) return;
        if (amount <= 0) return;

        int previousHealth = currentHealth;
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        onDamaged?.Invoke();

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

    public void Heal(int amount)
    {
        if (IsDead) return;
        if (amount <= 0) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    public void ResetHealth()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        IsDead = false;
        currentHealth = maxHealth;
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

        onDeath?.Invoke();

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
