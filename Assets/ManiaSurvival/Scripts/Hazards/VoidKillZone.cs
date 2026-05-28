using UnityEngine;

/// <summary>
/// Fallback kill system for units that fall below the playable area.
/// Attach to any always-active GameObject (e.g. GameManager or a dedicated
/// WorldBounds object). Every <see cref="checkInterval"/> seconds this script
/// scans all live UnitHealth objects; any below <see cref="killY"/> is
/// killed instantly.
///
/// The HellPit trigger (HellPitHazard) should handle the primary case.
/// This catches edge-cases where a unit clips through geometry.
/// </summary>
[DisallowMultipleComponent]
public class VoidKillZone : MonoBehaviour
{
    [Header("Kill Threshold")]
    [Tooltip("Any unit with a Y position below this value is killed. " +
             "Typical value: -10. Adjust to sit below your lowest valid platform.")]
    public float killY = -10f;

    [Header("Performance")]
    [Tooltip("How often (in seconds) to scan for units below the kill threshold. " +
             "0.5 is a good balance between responsiveness and cost.")]
    public float checkInterval = 0.5f;

    private float nextCheckTime;

    private void Update()
    {
        if (Time.time < nextCheckTime)
        {
            return;
        }

        nextCheckTime = Time.time + Mathf.Max(0.1f, checkInterval);

        UnitHealth[] units = FindObjectsByType<UnitHealth>(FindObjectsSortMode.None);

        for (int i = 0; i < units.Length; i++)
        {
            UnitHealth unit = units[i];

            if (unit == null || unit.IsDead)
            {
                continue;
            }

            if (unit.transform.position.y < killY)
            {
                Debug.Log("[VoidKill] " + unit.name + " fell below Y=" + killY + ". Applying lethal damage.");
                unit.TakeDamage(unit.maxHealth * 2 + 1, gameObject);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0f, 1f, 0.4f);
        Vector3 center = new Vector3(transform.position.x, killY, transform.position.z);
        Gizmos.DrawLine(center + new Vector3(-50f, 0f, 0f), center + new Vector3(50f, 0f, 0f));
        Gizmos.DrawLine(center + new Vector3(0f, 0f, -50f), center + new Vector3(0f, 0f, 50f));
    }
}
