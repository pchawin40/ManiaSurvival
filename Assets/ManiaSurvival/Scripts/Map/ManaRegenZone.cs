using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ManaRegenZone : MonoBehaviour
{
    [Header("Regen")]
    public float bonusManaRegenPerSecond = 20f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private readonly HashSet<UnitMana> occupants = new HashSet<UnitMana>();

    private void OnTriggerEnter(Collider other)
    {
        UnitMana mana = other.GetComponentInParent<UnitMana>();
        if (mana == null)
        {
            return;
        }

        if (!occupants.Add(mana))
        {
            return;
        }

        mana.SetZoneBonusRegen(bonusManaRegenPerSecond);

        if (showDebugLogs)
        {
            Debug.Log("[ManaZone] " + mana.name + " entered Waterfall mana zone");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        UnitMana mana = other.GetComponentInParent<UnitMana>();
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
