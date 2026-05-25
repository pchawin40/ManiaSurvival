using UnityEngine;

public class SpeedBootsPickup : MonoBehaviour
{
    [Header("Boost")]
    public float duration = 8f;
    public float speedMultiplier = 1.5f;

    [Header("Pickup")]
    public bool destroyOnCollect = true;

    private bool collected;

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        Debug.Log($"SpeedBootsPickup trigger entered by: {other.name}");

        Transform root = other.transform.root;

        bool isSurvivor =
            other.CompareTag("Survivor") ||
            other.GetComponentInParent<UnitHealth>() != null && other.GetComponentInParent<UnitHealth>().CompareTag("Survivor") ||
            root.CompareTag("Survivor");

        if (!isSurvivor)
        {
            Debug.Log("SpeedBootsPickup rejected: not Survivor.");
            return;
        }

        UnitHealth health = other.GetComponentInParent<UnitHealth>();

        if (health == null)
        {
            Debug.LogWarning("SpeedBootsPickup rejected: Survivor has no UnitHealth.");
            return;
        }

        SurvivorMovement movement = other.GetComponentInParent<SurvivorMovement>();

        if (movement == null)
        {
            Debug.LogWarning("SpeedBootsPickup found Survivor, but no SurvivorMovement was found.");
            return;
        }

        collected = true;

        movement.ApplySpeedBoost(speedMultiplier, duration);

        Debug.Log($"SpeedBootsPickup collected by {movement.gameObject.name}. Boost x{speedMultiplier} for {duration} seconds.");

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
    }
}