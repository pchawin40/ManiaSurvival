using UnityEngine;

public class SurvivorHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 3;
    public bool resetOnStart = true;

    [Header("Death")]
    public GameObject deathVisual;
    public MonoBehaviour[] behavioursToDisableOnDeath = new MonoBehaviour[0];
    public Collider[] collidersToDisableOnDeath = new Collider[0];
    public Renderer[] renderersToHideOnDeath = new Renderer[0];

    [Header("Game Manager")]
    public ManiaGameManager gameManager;

    public int CurrentHealth { get; private set; }
    public bool IsAlive { get; private set; } = true;

    public int GetCurrentHealth()
    {
        return CurrentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public float GetHealthPercent()
    {
        return maxHealth <= 0 ? 0f : (float)CurrentHealth / maxHealth;
    }

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = ManiaGameManager.Instance;
        }
    }

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<ManiaGameManager>();
        }

        if (gameManager != null)
        {
            gameManager.RegisterSurvivor(this);
        }

        if (resetOnStart)
        {
            ResetHealth();
        }
    }

    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        IsAlive = true;
        ApplyAliveState();
    }

    public void TakeDamage(int amount, GameObject damageSource)
    {
        if (!IsAlive || amount <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        if (CurrentHealth <= 0)
        {
            Die(damageSource);
        }
    }

    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
    }

    private void Die(GameObject damageSource)
    {
        IsAlive = false;
        ApplyAliveState();

        if (gameManager != null)
        {
            gameManager.ReportSurvivorDeath(this, damageSource);
        }
    }

    private void ApplyAliveState()
    {
        if (deathVisual != null)
        {
            deathVisual.SetActive(!IsAlive);
        }

        for (int i = 0; i < behavioursToDisableOnDeath.Length; i++)
        {
            if (behavioursToDisableOnDeath[i] != null)
            {
                behavioursToDisableOnDeath[i].enabled = IsAlive;
            }
        }

        for (int i = 0; i < collidersToDisableOnDeath.Length; i++)
        {
            if (collidersToDisableOnDeath[i] != null)
            {
                collidersToDisableOnDeath[i].enabled = IsAlive;
            }
        }

        for (int i = 0; i < renderersToHideOnDeath.Length; i++)
        {
            if (renderersToHideOnDeath[i] != null)
            {
                renderersToHideOnDeath[i].enabled = IsAlive;
            }
        }
    }
}
