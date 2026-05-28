using UnityEngine;
using UnityEngine.Events;

public class SurvivorMana : MonoBehaviour
{
    [Header("Mana")]
    public int maxMana = 100;
    public int currentMana = 100;

    [Header("Passive Regen")]
    [Tooltip("Mana points restored per second by default (anywhere). Water adds +2, Heaven adds +6 on top of any zone-specific behavior.")]
    public float passiveRegenPerSecond = 1f;

    [Header("Events")]
    public UnityEvent onManaChanged;

    public float ManaPercent => maxMana <= 0 ? 0f : (float)currentMana / maxMana;

    private float regenCarry;

    private void Awake()
    {
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
    }

    private void Update()
    {
        if (passiveRegenPerSecond <= 0f)
        {
            return;
        }

        if (currentMana >= maxMana)
        {
            regenCarry = 0f;
            return;
        }

        regenCarry += passiveRegenPerSecond * Time.deltaTime;

        if (regenCarry >= 1f)
        {
            int gain = Mathf.FloorToInt(regenCarry);
            regenCarry -= gain;
            RestoreMana(gain);
        }
    }

    public void RestoreMana(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int previous = currentMana;
        currentMana = Mathf.Clamp(currentMana + amount, 0, maxMana);

        if (currentMana != previous)
        {
            onManaChanged?.Invoke();
        }
    }

    public void RestoreToFull()
    {
        if (currentMana == maxMana)
        {
            return;
        }

        currentMana = maxMana;
        onManaChanged?.Invoke();
    }

    public bool SpendMana(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (currentMana < amount)
        {
            return false;
        }

        currentMana -= amount;
        onManaChanged?.Invoke();
        return true;
    }

    public bool HasMana(int amount)
    {
        return currentMana >= amount;
    }

    public static SurvivorMana EnsureOn(GameObject host, int requiredCost = 0)
    {
        return EnsureOn(host, requiredCost, -1, -1f);
    }

    public static SurvivorMana EnsureOn(GameObject host, int requiredCost, int overrideMaxMana, float overrideRegenPerSecond)
    {
        if (host == null)
        {
            return null;
        }

        SurvivorMana existing = host.GetComponent<SurvivorMana>();
        if (existing != null)
        {
            return existing;
        }

        if (requiredCost <= 0)
        {
            return null;
        }

        SurvivorMana added = host.AddComponent<SurvivorMana>();

        if (overrideMaxMana > 0)
        {
            added.maxMana = overrideMaxMana;
            added.currentMana = overrideMaxMana;
        }

        if (overrideRegenPerSecond >= 0f)
        {
            added.passiveRegenPerSecond = overrideRegenPerSecond;
        }

        return added;
    }
}
