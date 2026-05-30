using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class UnitMana : MonoBehaviour
{
    [Header("Mana")]
    public float maxMana = 100f;
    public float currentMana = 100f;

    [Header("Max Mana By Role")]
    [Tooltip("Survivor mana pool cap.")]
    public float survivorMaxMana = 20f;
    [Tooltip("Predator mana pool cap.")]
    public float predatorMaxMana = 100f;
    [Tooltip("NPC mana pool cap.")]
    public float npcMaxMana = 100f;
    [Tooltip("If true, picks max mana from tag/role on Awake.")]
    public bool autoApplyMaxManaByRole = true;

    [Header("Regen By Role")]
    [Tooltip("Passive mana regen for Survivors. Default ~0.33/sec keeps Medic from spam-healing.")]
    public float survivorManaRegenPerSecond = 0.33f;
    [Tooltip("Passive mana regen for Predators.")]
    public float predatorManaRegenPerSecond = 6f;
    [Tooltip("Passive mana regen for NPC casters.")]
    public float npcManaRegenPerSecond = 1f;
    [Tooltip("If true, picks regen from tag/role on Awake.")]
    public bool autoApplyRegenByRole = true;
    [Tooltip("Active regen rate used each frame. Set automatically when autoApplyRegenByRole is enabled.")]
    public float manaRegenPerSecond = 0.33f;

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
        ApplyMaxManaForRole();
        ApplyRegenForRole(logResult: showManaLogs);
    }

    public void ApplyMaxManaForRole()
    {
        if (!autoApplyMaxManaByRole)
        {
            return;
        }

        if (CompareTag("Survivor"))
        {
            maxMana = survivorMaxMana;
        }
        else if (CompareTag("Monster") || CompareTag("Predator"))
        {
            maxMana = predatorMaxMana;
        }
        else if (GetComponentInParent<NPCChaosCaster>() != null)
        {
            maxMana = npcMaxMana;
        }

        currentMana = Mathf.Clamp(currentMana, 0f, maxMana);
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

    public void ApplyRegenForRole(bool logResult = false)
    {
        if (!autoApplyRegenByRole)
        {
            return;
        }

        if (CompareTag("Survivor"))
        {
            manaRegenPerSecond = survivorManaRegenPerSecond;
        }
        else if (CompareTag("Monster") || CompareTag("Predator"))
        {
            manaRegenPerSecond = predatorManaRegenPerSecond;
        }
        else if (GetComponentInParent<NPCChaosCaster>() != null)
        {
            manaRegenPerSecond = npcManaRegenPerSecond;
        }

        if (logResult)
        {
            Debug.Log("[UnitMana] " + name + " mana regen = " + manaRegenPerSecond.ToString("0.##") + "/sec");
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
            existing.ApplyMaxManaForRole();
            existing.ApplyRegenForRole();
            return existing;
        }

        UnitMana added = host.AddComponent<UnitMana>();
        added.survivorMaxMana = 20f;
        added.predatorMaxMana = 100f;
        added.npcMaxMana = 100f;
        added.maxMana = isPredator ? 100f : 20f;
        added.currentMana = added.maxMana;
        added.survivorManaRegenPerSecond = 0.33f;
        added.predatorManaRegenPerSecond = 6f;
        added.npcManaRegenPerSecond = 1f;
        added.autoApplyRegenByRole = true;
        added.ApplyRegenForRole();
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
        Destroy(legacy);
        autoApplyMaxManaByRole = true;
        autoApplyRegenByRole = true;
        ApplyMaxManaForRole();
        ApplyRegenForRole();
    }
}
