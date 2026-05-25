using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterfallHealingZone : MonoBehaviour
{
    [Header("Healing")]
    public float healPerSecond = 1f;

    [Header("Monster Damage")]
    public bool damagesWaterWeakMonster = false;
    public float monsterDamagePerSecond = 1f;

    private readonly Dictionary<SurvivorHealth, int> survivorOverlapCounts = new Dictionary<SurvivorHealth, int>();
    private readonly Dictionary<SurvivorHealth, float> survivorHealProgress = new Dictionary<SurvivorHealth, float>();
    private readonly Dictionary<GameObject, int> monsterOverlapCounts = new Dictionary<GameObject, int>();
    private readonly Dictionary<GameObject, float> monsterDamageProgress = new Dictionary<GameObject, float>();

    private void Awake()
    {
        Collider zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    private void Update()
    {
        HealSurvivors();
        DamageWeakMonsters();
    }

    private void OnTriggerEnter(Collider other)
    {
        SurvivorHealth survivor = other.GetComponentInParent<SurvivorHealth>();
        if (survivor != null && survivor.enabled && survivor.IsAlive)
        {
            if (!survivorOverlapCounts.ContainsKey(survivor))
            {
                survivorOverlapCounts[survivor] = 0;
                survivorHealProgress[survivor] = 0f;
            }

            survivorOverlapCounts[survivor]++;
            return;
        }

        if (!damagesWaterWeakMonster)
        {
            return;
        }

        MonsterAI monster = other.GetComponentInParent<MonsterAI>();
        if (monster != null)
        {
            GameObject monsterObject = monster.gameObject;
            if (!monsterOverlapCounts.ContainsKey(monsterObject))
            {
                monsterOverlapCounts[monsterObject] = 0;
                monsterDamageProgress[monsterObject] = 0f;
            }

            monsterOverlapCounts[monsterObject]++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        SurvivorHealth survivor = other.GetComponentInParent<SurvivorHealth>();
        if (survivor != null && survivorOverlapCounts.ContainsKey(survivor))
        {
            survivorOverlapCounts[survivor] = Mathf.Max(0, survivorOverlapCounts[survivor] - 1);
            if (survivorOverlapCounts[survivor] == 0)
            {
                survivorOverlapCounts.Remove(survivor);
                survivorHealProgress.Remove(survivor);
            }
        }

        if (!damagesWaterWeakMonster)
        {
            return;
        }

        MonsterAI monster = other.GetComponentInParent<MonsterAI>();
        if (monster != null)
        {
            GameObject monsterObject = monster.gameObject;
            if (monsterOverlapCounts.ContainsKey(monsterObject))
            {
                monsterOverlapCounts[monsterObject] = Mathf.Max(0, monsterOverlapCounts[monsterObject] - 1);
                if (monsterOverlapCounts[monsterObject] == 0)
                {
                    monsterOverlapCounts.Remove(monsterObject);
                    monsterDamageProgress.Remove(monsterObject);
                }
            }
        }
    }

    private void HealSurvivors()
    {
        if (healPerSecond <= 0f)
        {
            return;
        }

        SurvivorHealth[] survivors = new SurvivorHealth[survivorOverlapCounts.Keys.Count];
        survivorOverlapCounts.Keys.CopyTo(survivors, 0);

        for (int i = 0; i < survivors.Length; i++)
        {
            SurvivorHealth survivor = survivors[i];

            if (survivor == null || !survivor.enabled || !survivor.IsAlive)
            {
                survivorOverlapCounts.Remove(survivor);
                survivorHealProgress.Remove(survivor);
                continue;
            }

            float progress = survivorHealProgress.TryGetValue(survivor, out float storedProgress) ? storedProgress : 0f;
            progress += healPerSecond * Time.deltaTime;

            int healAmount = Mathf.FloorToInt(progress);
            if (healAmount > 0)
            {
                survivor.Heal(healAmount);
                progress -= healAmount;
            }

            survivorHealProgress[survivor] = progress;
        }
    }

    private void DamageWeakMonsters()
    {
        if (!damagesWaterWeakMonster || monsterDamagePerSecond <= 0f)
        {
            return;
        }

        GameObject[] monsters = new GameObject[monsterOverlapCounts.Keys.Count];
        monsterOverlapCounts.Keys.CopyTo(monsters, 0);

        for (int i = 0; i < monsters.Length; i++)
        {
            GameObject monsterObject = monsters[i];

            if (monsterObject == null)
            {
                monsterOverlapCounts.Remove(monsterObject);
                monsterDamageProgress.Remove(monsterObject);
                continue;
            }

            float progress = monsterDamageProgress.TryGetValue(monsterObject, out float storedProgress) ? storedProgress : 0f;
            progress += monsterDamagePerSecond * Time.deltaTime;

            int damageAmount = Mathf.FloorToInt(progress);
            if (damageAmount > 0 && TryDamageMonster(monsterObject, damageAmount))
            {
                progress -= damageAmount;
            }

            monsterDamageProgress[monsterObject] = progress;
        }
    }

    private bool TryDamageMonster(GameObject monsterObject, int damageAmount)
    {
        MonoBehaviour[] components = monsterObject.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < components.Length; i++)
        {
            MonoBehaviour component = components[i];
            if (component == null)
            {
                continue;
            }

            System.Type componentType = component.GetType();

            MethodInfo damageWithSource = componentType.GetMethod("TakeDamage", new[] { typeof(int), typeof(GameObject) });
            if (damageWithSource != null)
            {
                damageWithSource.Invoke(component, new object[] { damageAmount, gameObject });
                return true;
            }

            MethodInfo floatDamageWithSource = componentType.GetMethod("TakeDamage", new[] { typeof(float), typeof(GameObject) });
            if (floatDamageWithSource != null)
            {
                floatDamageWithSource.Invoke(component, new object[] { (float)damageAmount, gameObject });
                return true;
            }

            MethodInfo damageWithoutSource = componentType.GetMethod("TakeDamage", new[] { typeof(int) });
            if (damageWithoutSource != null)
            {
                damageWithoutSource.Invoke(component, new object[] { damageAmount });
                return true;
            }
        }

        return false;
    }
}
