using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class NeutralTree : MonoBehaviour
{
    [Header("Tree")]
    public bool blocksMovement = true;

    [Header("References")]
    public UnitHealth unitHealth;
    public Collider treeCollider;

    public bool IsBlocking => blocksMovement && treeCollider != null && treeCollider.enabled && !treeCollider.isTrigger;

    private void Awake()
    {
        CacheReferences();
        EnsureDestructibleHealth();
        ApplySettings();
    }

    private void OnValidate()
    {
        CacheReferences();
        ApplySettings();
    }

    public void SetBlocksMovement(bool shouldBlock)
    {
        blocksMovement = shouldBlock;
        ApplySettings();
    }

    private void CacheReferences()
    {
        if (unitHealth == null)
        {
            unitHealth = GetComponent<UnitHealth>();
        }

        if (treeCollider == null)
        {
            treeCollider = GetComponent<Collider>();
        }
    }

    private void ApplySettings()
    {
        if (treeCollider != null)
        {
            treeCollider.isTrigger = !blocksMovement;
        }
    }

    private void EnsureDestructibleHealth()
    {
        DestructiblePropHealth health = GetComponent<DestructiblePropHealth>();
        if (health != null)
        {
            return;
        }

        health = gameObject.AddComponent<DestructiblePropHealth>();
        health.maxHealth = 60;
        health.currentHealth = 60;
        health.destroyOnDeath = true;
        health.hitFlashRenderer = GetComponentInChildren<Renderer>();
    }
}
