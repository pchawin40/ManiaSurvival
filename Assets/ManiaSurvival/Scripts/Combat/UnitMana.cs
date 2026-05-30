using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class UnitMana : MonoBehaviour
{
    [Header("Mana")]
    public float maxMana = 100f;
    public float currentMana = 100f;

    [Header("Regen")]
    public float manaRegenPerSecond = 3f;

    [Header("Zone Bonus")]
    [Tooltip("Extra regen from ManaRegenZone while inside. Applied on top of base regen.")]
    public float zoneBonusRegenPerSecond;

    [Header("Events")]
    public UnityEvent onManaChanged;

    [Header("Debug")]
    public bool showManaLogs;

    private float regenCarry;

    private void Awake()
    {
        currentMana = Mathf.Clamp(currentMana, 0f, maxMana);
        MigrateLegacySurvivorMana();
    }

    private void Update()
    {
        float totalRegen = manaRegenPerSecond + zoneBonusRegenPerSecond;
        if (totalRegen <= 0f || currentMana >= maxMana)
        {
            regenCarry = 0f;
            return;
        }

        regenCarry += totalRegen * Time.deltaTime;
        if (regenCarry >= 0.01f)
        {
            float gain = regenCarry;
            regenCarry = 0f;
            RestoreMana(gain);
        }
    }

    public float GetManaPercent()
    {
        return maxMana <= 0f ? 0f : currentMana / maxMana;
    }

    public bool HasMana(float amount)
    {
        return amount <= 0f || currentMana >= amount;
    }

    public bool SpendMana(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (currentMana < amount)
        {
            if (showManaLogs)
            {
                Debug.Log("[UnitMana] " + name + " insufficient mana for spend " + amount.ToString("0.0")
                    + " (have " + currentMana.ToString("0.0") + ")");
            }

            return false;
        }

        currentMana -= amount;
        currentMana = Mathf.Clamp(currentMana, 0f, maxMana);
        onManaChanged?.Invoke();

        if (showManaLogs)
        {
            Debug.Log("[UnitMana] " + name + " spent " + amount.ToString("0.0") + " mana (remaining "
                + currentMana.ToString("0.0") + ")");
        }

        return true;
    }

    public void RestoreMana(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        float previous = currentMana;
        currentMana = Mathf.Clamp(currentMana + amount, 0f, maxMana);
        if (!Mathf.Approximately(currentMana, previous))
        {
            onManaChanged?.Invoke();
        }
    }

    public void RestoreToFull()
    {
        if (Mathf.Approximately(currentMana, maxMana))
        {
            return;
        }

        currentMana = maxMana;
        onManaChanged?.Invoke();
    }

    public void SetZoneBonusRegen(float bonusPerSecond)
    {
        zoneBonusRegenPerSecond = Mathf.Max(0f, bonusPerSecond);
    }

    public static UnitMana EnsureOn(GameObject host, bool isPredator)
    {
        if (host == null)
        {
            return null;
        }

        UnitMana existing = host.GetComponent<UnitMana>();
        if (existing != null)
        {
            return existing;
        }

        UnitMana added = host.AddComponent<UnitMana>();
        added.maxMana = 100f;
        added.currentMana = 100f;
        added.manaRegenPerSecond = isPredator ? 6f : 3f;
        return added;
    }

    private void MigrateLegacySurvivorMana()
    {
        SurvivorMana legacy = GetComponent<SurvivorMana>();
        if (legacy == null)
        {
            return;
        }

        maxMana = legacy.maxMana;
        currentMana = legacy.currentMana;
        manaRegenPerSecond = legacy.passiveRegenPerSecond;
        Destroy(legacy);
    }
}
