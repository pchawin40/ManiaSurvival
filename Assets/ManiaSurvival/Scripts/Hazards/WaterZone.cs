using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterZone : MonoBehaviour
{
    [Header("Water Effects")]
    [Min(0f)]
    public float survivorHealPerSecond = 5f;
    [Min(0f)]
    public float hellBruteDamagePerSecond = 8f;
    [Min(0.05f)]
    public float tickInterval = 0.5f;

    private readonly HashSet<UnitHealth> occupants = new HashSet<UnitHealth>();
    private readonly Dictionary<UnitHealth, float> healProgress = new Dictionary<UnitHealth, float>();
    private readonly Dictionary<UnitHealth, float> damageProgress = new Dictionary<UnitHealth, float>();
    private Coroutine tickRoutine;

    private void OnEnable()
    {
        tickRoutine = StartCoroutine(WaterTickLoop());
    }

    private void OnDisable()
    {
        if (tickRoutine != null)
        {
            StopCoroutine(tickRoutine);
            tickRoutine = null;
        }

        occupants.Clear();
        healProgress.Clear();
        damageProgress.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        RegisterOccupant(other.GetComponentInParent<UnitHealth>());
    }

    private void OnTriggerExit(Collider other)
    {
        UnregisterOccupant(other.GetComponentInParent<UnitHealth>());
    }

    private IEnumerator WaterTickLoop()
    {
        while (true)
        {
            ApplyWaterEffects();
            yield return new WaitForSeconds(Mathf.Max(0.05f, tickInterval));
        }
    }

    private void ApplyWaterEffects()
    {
        if (occupants.Count == 0)
        {
            return;
        }

        UnitHealth[] snapshot = new UnitHealth[occupants.Count];
        occupants.CopyTo(snapshot);

        for (int i = 0; i < snapshot.Length; i++)
        {
            UnitHealth unitHealth = snapshot[i];

            if (unitHealth == null || !unitHealth.gameObject.activeInHierarchy)
            {
                UnregisterOccupant(unitHealth);
                continue;
            }

            if (unitHealth.CompareTag("Survivor"))
            {
                ApplyOverTime(unitHealth, survivorHealPerSecond, healProgress, true);
            }
            else if (IsHellBrute(unitHealth))
            {
                ApplyOverTime(unitHealth, hellBruteDamagePerSecond, damageProgress, false);
            }
        }
    }

    private void ApplyOverTime(UnitHealth unitHealth, float ratePerSecond, Dictionary<UnitHealth, float> progressMap, bool isHealing)
    {
        if (ratePerSecond <= 0f)
        {
            return;
        }

        float progress = 0f;
        progressMap.TryGetValue(unitHealth, out progress);
        progress += ratePerSecond * tickInterval;

        int wholePoints = Mathf.FloorToInt(progress);
        if (wholePoints <= 0)
        {
            progressMap[unitHealth] = progress;
            return;
        }

        progress -= wholePoints;
        progressMap[unitHealth] = progress;

        if (isHealing)
        {
            unitHealth.Heal(wholePoints);
        }
        else
        {
            unitHealth.TakeDamage(wholePoints);
        }
    }

    private void RegisterOccupant(UnitHealth unitHealth)
    {
        if (unitHealth == null)
        {
            return;
        }

        if (unitHealth.CompareTag("Survivor"))
        {
            occupants.Add(unitHealth);
            return;
        }

        if (unitHealth.CompareTag("Monster") || unitHealth.CompareTag("Predator"))
        {
            occupants.Add(unitHealth);
        }
    }

    private void UnregisterOccupant(UnitHealth unitHealth)
    {
        if (unitHealth == null)
        {
            return;
        }

        occupants.Remove(unitHealth);
        healProgress.Remove(unitHealth);
        damageProgress.Remove(unitHealth);
    }

    private bool IsHellBrute(UnitHealth unitHealth)
    {
        PredatorIdentity predatorIdentity = unitHealth.GetComponent<PredatorIdentity>();
        return predatorIdentity != null && predatorIdentity.archetype == PredatorArchetype.HellBrute;
    }
}
