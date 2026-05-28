using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class HeavenRecoveryZone : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("Only units with this tag get recovery effects.")]
    public string survivorTag = "Survivor";

    [Header("Recovery")]
    [Min(0f)] public float healPerSecond = 5f;
    [Min(0f)] public float staminaPerSecond = 1.5f;
    [Min(0.1f)] public float tickInterval = 0.25f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private readonly Dictionary<UnitHealth, SurvivorMovement> occupants = new Dictionary<UnitHealth, SurvivorMovement>();
    private readonly Dictionary<UnitHealth, float> healCarry = new Dictionary<UnitHealth, float>();
    private readonly Dictionary<UnitHealth, float> staminaCarry = new Dictionary<UnitHealth, float>();
    private float nextTickTime;

    private void Awake()
    {
        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        UnitHealth health = other.GetComponentInParent<UnitHealth>();
        if (health == null || health.IsDead)
        {
            return;
        }

        if (!string.IsNullOrEmpty(survivorTag) && !health.CompareTag(survivorTag))
        {
            return;
        }

        if (occupants.ContainsKey(health))
        {
            return;
        }

        SurvivorMovement movement = health.GetComponent<SurvivorMovement>();
        occupants.Add(health, movement);
        healCarry[health] = 0f;
        staminaCarry[health] = 0f;

        if (showDebugLogs)
        {
            Debug.Log("[HeavenRecoveryZone] " + health.name + " entered Heaven recovery zone.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        UnitHealth health = other.GetComponentInParent<UnitHealth>();
        if (health == null)
        {
            return;
        }

        if (!occupants.Remove(health))
        {
            return;
        }

        healCarry.Remove(health);
        staminaCarry.Remove(health);

        if (showDebugLogs)
        {
            Debug.Log("[HeavenRecoveryZone] " + health.name + " left Heaven recovery zone.");
        }
    }

    private void Update()
    {
        if (occupants.Count == 0 || Time.time < nextTickTime)
        {
            return;
        }

        nextTickTime = Time.time + Mathf.Max(0.1f, tickInterval);
        float dt = Mathf.Max(0.1f, tickInterval);

        UnitHealth[] keys = new UnitHealth[occupants.Count];
        occupants.Keys.CopyTo(keys, 0);

        for (int i = 0; i < keys.Length; i++)
        {
            UnitHealth health = keys[i];
            if (health == null || health.IsDead || !health.gameObject.activeInHierarchy)
            {
                occupants.Remove(health);
                healCarry.Remove(health);
                staminaCarry.Remove(health);
                continue;
            }

            healCarry[health] += healPerSecond * dt;
            int healAmount = Mathf.FloorToInt(healCarry[health]);
            if (healAmount > 0)
            {
                healCarry[health] -= healAmount;
                health.Heal(healAmount);
            }

            SurvivorMovement movement = occupants[health];
            if (movement == null || staminaPerSecond <= 0f)
            {
                continue;
            }

            staminaCarry[health] += staminaPerSecond * dt;
            float staminaAmount = staminaCarry[health];
            if (staminaAmount > 0f)
            {
                movement.RestoreStamina(staminaAmount);
                staminaCarry[health] = 0f;
            }
        }
    }
}
