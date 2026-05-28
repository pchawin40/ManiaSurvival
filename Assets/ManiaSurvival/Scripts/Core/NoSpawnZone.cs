using UnityEngine;

/// <summary>
/// Mark an area as off-limits for certain spawn types.
/// Attach to a GameObject that has a Collider set to isTrigger = true.
/// MapSpawnUtility will reject any candidate position that overlaps this zone
/// according to the flags set below.
///
/// Suggested placement:
///   - Heaven safe-zone platform
///   - Heaven gate opening
///   - Survivor spawn point (small radius)
///   - Monster spawn point (small radius)
///   - Portal / shop area
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class NoSpawnZone : MonoBehaviour
{
    [Header("Zone Identity")]
    [Tooltip("Human-readable name shown in debug logs when this zone rejects a spawn.")]
    public string zoneName = "NoSpawnZone";

    [Header("Spawn Blocking Flags")]
    [Tooltip("Block world items (InvisibilityScroll, SpeedBoots, etc.) from spawning here.")]
    public bool blockItems = true;

    [Tooltip("Block hazard spawns (Tornado, future AoE effects) from being placed here.")]
    public bool blockHazards = true;

    [Tooltip("Block tree patches from spawning here (Wildkeeper and NPCChaosCaster).")]
    public bool blockTreePatches = true;

    [Tooltip("Block all NPCChaosCaster abilities from targeting this area.")]
    public bool blockNPCChaos = true;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("[NoSpawnZone] '" + zoneName + "': Collider should be set to isTrigger = true " +
                             "so it does not block character movement.", this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);

        if (col is BoxCollider box)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.7f);
            Gizmos.DrawWireCube(box.center, box.size);
            return;
        }

        float r = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        Gizmos.DrawSphere(transform.position, r * 0.5f);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, r * 0.5f);
    }
}
