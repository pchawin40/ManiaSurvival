using UnityEngine;

public enum PrototypePropType
{
    Tree,
    Rock,
    SmallWall,
    Crate,
    Log
}

/// <summary>
/// Marks procedurally generated parkour props for debugging and future systems.
/// </summary>
[DisallowMultipleComponent]
public class PrototypeMapProp : MonoBehaviour
{
    [Header("Prop")]
    public PrototypePropType propType = PrototypePropType.Rock;

    [Tooltip("When true, this prop has a solid collider that blocks CharacterController movement.")]
    public bool blocksMovement = true;

    [Tooltip("When true, this prop can block Hook line-of-sight (tall or wide blocker).")]
    public bool blocksLineOfSight;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = blocksMovement
            ? new Color(0.9f, 0.45f, 0.1f, 0.35f)
            : new Color(0.3f, 0.8f, 0.35f, 0.25f);

        Collider col = GetComponentInChildren<Collider>();
        if (col != null && !col.isTrigger)
        {
            Gizmos.matrix = col.transform.localToWorldMatrix;
            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
                return;
            }

            if (col is CapsuleCollider capsule)
            {
                Gizmos.DrawWireSphere(capsule.center, capsule.radius);
                return;
            }
        }

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.up * 0.5f, Vector3.one);
    }
}
