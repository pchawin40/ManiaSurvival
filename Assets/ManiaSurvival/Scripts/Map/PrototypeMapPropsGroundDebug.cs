using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional gizmo markers for prototype terrain prop ground snapping (editor only).
/// Ground snap debug markers — off by default.
/// </summary>
[DisallowMultipleComponent]
public class PrototypeMapPropsGroundDebug : MonoBehaviour
{
    [Tooltip("Ground snap debug markers — off by default. Enable to show green/red gizmo dots in Scene view when this object is selected.")]
    public bool enableGroundSnapDebug = false;

    [System.Serializable]
    public struct GroundSnapMarker
    {
        public Vector3 position;
        public bool success;
        public string reason;
    }

    public List<GroundSnapMarker> markers = new List<GroundSnapMarker>();

    public void ClearMarkers()
    {
        markers.Clear();
    }

    public void AddMarker(Vector3 position, bool success, string reason)
    {
        markers.Add(new GroundSnapMarker
        {
            position = position,
            success = success,
            reason = reason ?? string.Empty
        });
    }

    private void OnDrawGizmosSelected()
    {
        if (!enableGroundSnapDebug || markers == null)
        {
            return;
        }

        for (int i = 0; i < markers.Count; i++)
        {
            GroundSnapMarker marker = markers[i];
            Gizmos.color = marker.success ? new Color(0.2f, 0.9f, 0.3f, 0.85f) : new Color(0.95f, 0.2f, 0.2f, 0.9f);
            Gizmos.DrawSphere(marker.position, marker.success ? 0.12f : 0.18f);
            if (marker.success)
            {
                Gizmos.DrawLine(marker.position, marker.position + Vector3.up * 0.6f);
            }
        }
    }
}
