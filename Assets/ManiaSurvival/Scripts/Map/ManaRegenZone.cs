using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ManaRegenZone : MonoBehaviour
{
    [Header("Regen By Role")]
    [Tooltip("Extra mana/sec for Survivors while inside. Default 2/sec on top of passive regen.")]
    public float survivorBonusManaRegenPerSecond = 2f;
    [Tooltip("Extra mana/sec for Predators while inside. Default 10/sec on top of passive regen.")]
    public float predatorBonusManaRegenPerSecond = 10f;
    [Tooltip("Fallback bonus when role-specific value is 0.")]
    public float bonusManaRegenPerSecond = 2f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private readonly HashSet<UnitMana> occupants = new HashSet<UnitMana>();

    private void OnTriggerEnter(Collider other)
    {
        TryRegisterOccupant(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryRegisterOccupant(other);
    }

    private void OnTriggerExit(Collider other)
    {
        UnitMana mana = ResolveUnitMana(other);
        if (mana == null)
        {
            return;
        }

        if (!occupants.Remove(mana))
        {
            return;
        }

        mana.SetZoneBonusRegen(0f);

        if (showDebugLogs)
        {
            Debug.Log("[ManaZone] " + mana.name + " exited Waterfall mana zone");
        }
    }

    private void TryRegisterOccupant(Collider other)
    {
        UnitMana mana = ResolveUnitMana(other);
        if (mana == null)
        {
            return;
        }

        if (!occupants.Add(mana))
        {
            return;
        }

        mana.SetZoneBonusRegen(GetBonusForRole(mana));

        if (showDebugLogs)
        {
            Debug.Log("[ManaZone] " + mana.name + " entered Waterfall mana zone (+" +
                      GetBonusForRole(mana).ToString("0.#") + " mana/sec)");
        }
    }

    private static UnitMana ResolveUnitMana(Collider other)
    {
        if (other == null)
        {
            return null;
        }

        UnitMana mana = other.GetComponentInParent<UnitMana>();
        if (mana != null)
        {
            return mana;
        }

        UnitHealth health = other.GetComponentInParent<UnitHealth>();
        if (health == null || health.IsDead)
        {
            return null;
        }

        if (!health.CompareTag("Survivor")
            && !health.CompareTag("Monster")
            && !health.CompareTag("Predator"))
        {
            return null;
        }

        bool isPredator = health.CompareTag("Monster") || health.CompareTag("Predator");
        return UnitMana.EnsureOn(health.gameObject, isPredator);
    }

    private float GetBonusForRole(UnitMana mana)
    {
        if (mana.CompareTag("Survivor"))
        {
            return survivorBonusManaRegenPerSecond > 0f
                ? survivorBonusManaRegenPerSecond
                : bonusManaRegenPerSecond;
        }

        if (mana.CompareTag("Monster") || mana.CompareTag("Predator"))
        {
            return predatorBonusManaRegenPerSecond > 0f
                ? predatorBonusManaRegenPerSecond
                : bonusManaRegenPerSecond;
        }

        return bonusManaRegenPerSecond;
    }

    private void OnDisable()
    {
        foreach (UnitMana mana in occupants)
        {
            if (mana != null)
            {
                mana.SetZoneBonusRegen(0f);
            }
        }

        occupants.Clear();
    }
}
