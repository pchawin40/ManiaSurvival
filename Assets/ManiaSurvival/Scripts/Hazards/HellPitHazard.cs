using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class HellPitHazard : MonoBehaviour
{
    [Header("Damage")]
    public float survivorDamagePerSecond = 15f;
    public float monsterDamagePerSecond = 80f;
    public float tickInterval = 0.25f;
    public bool instantKillMonster = false;
    public bool destroyOnMonsterDeath = false;

    [Header("Debug")]
    public bool drawGizmo = true;

    private readonly HashSet<UnitHealth> occupants = new HashSet<UnitHealth>();
    private readonly Dictionary<UnitHealth, float> tickTimers = new Dictionary<UnitHealth, float>();

    private void Awake()
    {
        Collider pitCollider = GetComponent<Collider>();
        pitCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        RegisterOccupant(other.GetComponentInParent<UnitHealth>());
    }

    private void OnTriggerStay(Collider other)
    {
        RegisterOccupant(other.GetComponentInParent<UnitHealth>());
    }

    private void OnTriggerExit(Collider other)
    {
        UnregisterOccupant(other.GetComponentInParent<UnitHealth>());
    }

    private void Update()
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
            if (unitHealth == null)
            {
                continue;
            }

            if (!tickTimers.ContainsKey(unitHealth))
            {
                tickTimers[unitHealth] = 0f;
            }

            tickTimers[unitHealth] += Time.deltaTime;
            if (tickTimers[unitHealth] >= Mathf.Max(0.01f, tickInterval))
            {
                tickTimers[unitHealth] = 0f;
                ApplyDamageTick(unitHealth);
            }
        }
    }

    private void ApplyDamageTick(UnitHealth unitHealth)
    {
        if (unitHealth == null || unitHealth.IsDead)
        {
            UnregisterOccupant(unitHealth);
            return;
        }

        if (!IsEligibleTarget(unitHealth))
        {
            return;
        }

        float damagePerSecond = GetDamagePerSecond(unitHealth);
        if (damagePerSecond <= 0f)
        {
            return;
        }

        if (instantKillMonster && IsMonster(unitHealth))
        {
            Debug.Log("damage tick");
            unitHealth.TakeDamage(unitHealth.currentHealth, gameObject);
            if (destroyOnMonsterDeath)
            {
                Destroy(unitHealth.gameObject);
            }

            return;
        }

        int damageAmount = Mathf.Max(1, Mathf.RoundToInt(damagePerSecond * Mathf.Max(0.01f, tickInterval)));
        unitHealth.TakeDamage(damageAmount, gameObject);
        Debug.Log("damage tick");
    }

    private float GetDamagePerSecond(UnitHealth unitHealth)
    {
        if (IsMonster(unitHealth))
        {
            return monsterDamagePerSecond;
        }

        if (IsSurvivor(unitHealth))
        {
            return survivorDamagePerSecond;
        }

        return 0f;
    }

    private bool IsEligibleTarget(UnitHealth unitHealth)
    {
        if (unitHealth == null || unitHealth.IsDead)
        {
            return false;
        }

        return IsSurvivor(unitHealth) || IsMonster(unitHealth);
    }

    private bool IsSurvivor(UnitHealth unitHealth)
    {
        return unitHealth != null && unitHealth.CompareTag("Survivor");
    }

    private bool IsMonster(UnitHealth unitHealth)
    {
        return unitHealth != null && (unitHealth.CompareTag("Monster") || unitHealth.CompareTag("Predator"));
    }

    private void RegisterOccupant(UnitHealth unitHealth)
    {
        if (unitHealth == null || unitHealth.IsDead)
        {
            return;
        }

        if (!IsEligibleTarget(unitHealth))
        {
            return;
        }

        if (occupants.Add(unitHealth))
        {
            tickTimers[unitHealth] = 0f;
            Debug.Log("unit entered HellPit: " + unitHealth.name);
        }
    }

    private void UnregisterOccupant(UnitHealth unitHealth)
    {
        if (unitHealth == null)
        {
            return;
        }

        if (occupants.Remove(unitHealth))
        {
            tickTimers.Remove(unitHealth);
            Debug.Log("unit exited HellPit: " + unitHealth.name);
        }
    }

    private void OnDisable()
    {
        occupants.Clear();
        tickTimers.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo)
        {
            return;
        }

        Collider pitCollider = GetComponent<Collider>();
        if (pitCollider == null)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.25f, 0.1f, 0.35f);

        if (pitCollider is BoxCollider box)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(box.center, box.size);
            return;
        }

        Gizmos.DrawWireSphere(transform.position, Mathf.Max(1f, Mathf.Max(transform.localScale.x, transform.localScale.z)));
    }
}
