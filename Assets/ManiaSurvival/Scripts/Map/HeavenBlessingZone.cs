using System.Collections.Generic;
using UnityEngine;

public class HeavenBlessingZone : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("Only objects with this tag are blessed. The rest of the project uses 'Survivor'.")]
    public string survivorTag = "Survivor";

    [Header("Blessing Settings")]
    public bool healHealth = true;
    public bool restoreMana = true;
    public bool giveSpeedBoots = true;

    [Header("Heal While Inside")]
    [Tooltip("Health points restored per second while standing inside the zone.")]
    public int healPerSecond = 10;

    [Header("Mana While Inside")]
    [Tooltip("Mana points restored per second while standing inside the zone. Heaven default is 6 (vs 1 baseline, vs 2 in Water).")]
    public int manaPerSecond = 6;
    [Tooltip("If a unit has no UnitMana, add one automatically on enter.")]
    public bool autoAddManaComponent = true;
    public int defaultMaxMana = 100;

    [Header("Speed Boost While Inside")]
    public float speedMultiplier = 1.5f;
    [Tooltip("Each refresh of the speed boost lasts this many seconds. The boost ends naturally this many seconds after leaving Heaven.")]
    public float speedRefreshSeconds = 1f;

    [Header("Debug")]
    public bool showDebugMessages = true;

    private class BlessedSurvivor
    {
        public UnitHealth health;
        public UnitMana mana;
        public SurvivorMovement movement;
        public float healCarry;
        public float manaCarry;
        public float nextSpeedRefreshTime;
    }

    private readonly Dictionary<UnitHealth, BlessedSurvivor> blessed = new Dictionary<UnitHealth, BlessedSurvivor>();
    private readonly List<UnitHealth> staleKeys = new List<UnitHealth>();

    private void OnTriggerEnter(Collider other)
    {
        UnitHealth health = other.GetComponentInParent<UnitHealth>();
        if (health == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(survivorTag) && !health.CompareTag(survivorTag))
        {
            return;
        }

        if (blessed.ContainsKey(health))
        {
            return;
        }

        UnitMana mana = health.GetComponent<UnitMana>();
        if (restoreMana && mana == null && autoAddManaComponent)
        {
            mana = UnitMana.EnsureOn(health.gameObject, false);
            mana.maxMana = defaultMaxMana;
            mana.currentMana = defaultMaxMana;

            if (showDebugMessages)
            {
                Debug.Log("[HeavenBlessingZone] Auto-added UnitMana to " + health.name);
            }
        }

        SurvivorMovement movement = health.GetComponent<SurvivorMovement>();

        BlessedSurvivor entry = new BlessedSurvivor
        {
            health = health,
            mana = mana,
            movement = movement,
            healCarry = 0f,
            manaCarry = 0f,
            nextSpeedRefreshTime = 0f
        };

        blessed.Add(health, entry);

        if (showDebugMessages)
        {
            Debug.Log("[HeavenBlessingZone] " + health.name + " entered Heaven.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        UnitHealth health = other.GetComponentInParent<UnitHealth>();
        if (health == null)
        {
            return;
        }

        if (!blessed.Remove(health))
        {
            return;
        }

        if (showDebugMessages)
        {
            Debug.Log("[HeavenBlessingZone] " + health.name + " left Heaven.");
        }
    }

    private void Update()
    {
        if (blessed.Count == 0)
        {
            return;
        }

        float dt = Time.deltaTime;
        float refreshInterval = Mathf.Max(0.1f, speedRefreshSeconds * 0.5f);

        staleKeys.Clear();

        foreach (KeyValuePair<UnitHealth, BlessedSurvivor> pair in blessed)
        {
            BlessedSurvivor entry = pair.Value;

            if (entry.health == null || entry.health.IsDead)
            {
                staleKeys.Add(pair.Key);
                continue;
            }

            if (healHealth)
            {
                entry.healCarry += healPerSecond * dt;
                int healAmount = Mathf.FloorToInt(entry.healCarry);
                if (healAmount > 0)
                {
                    entry.healCarry -= healAmount;
                    entry.health.Heal(healAmount);
                }
            }

            if (restoreMana && entry.mana != null)
            {
                entry.manaCarry += manaPerSecond * dt;
                int manaAmount = Mathf.FloorToInt(entry.manaCarry);
                if (manaAmount > 0)
                {
                    entry.manaCarry -= manaAmount;
                    entry.mana.RestoreMana(manaAmount);
                }
            }

            if (giveSpeedBoots && entry.movement != null && Time.time >= entry.nextSpeedRefreshTime)
            {
                entry.movement.ApplySpeedBoost(speedMultiplier, speedRefreshSeconds);
                entry.nextSpeedRefreshTime = Time.time + refreshInterval;
            }
        }

        for (int i = 0; i < staleKeys.Count; i++)
        {
            blessed.Remove(staleKeys[i]);
        }
    }

    private void OnDisable()
    {
        blessed.Clear();
        staleKeys.Clear();
    }
}
