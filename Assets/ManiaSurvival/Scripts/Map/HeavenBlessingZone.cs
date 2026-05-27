using UnityEngine;

public class HeavenBlessingZone : MonoBehaviour
{
    [Header("Blessing Settings")]
    public bool giveSpeedBoots = true;
    public bool healHealth = true;
    public bool restoreMana = true;

    [Header("Debug")]
    public bool showDebugMessages = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (showDebugMessages)
        {
            Debug.Log("Player entered Heaven blessing zone.");
        }

        if (healHealth)
        {
            // Later we connect this to CamperHealth or PlayerHealth.
            other.SendMessage("HealToFull", SendMessageOptions.DontRequireReceiver);
            other.SendMessage("Heal", 9999, SendMessageOptions.DontRequireReceiver);
        }

        if (restoreMana)
        {
            // Later we connect this to your mana system.
            other.SendMessage("RestoreManaToFull", SendMessageOptions.DontRequireReceiver);
            other.SendMessage("RestoreMana", 9999, SendMessageOptions.DontRequireReceiver);
        }

        if (giveSpeedBoots)
        {
            // Later we connect this to your real speed boots pickup/effect.
            other.SendMessage("GiveSpeedBoots", SendMessageOptions.DontRequireReceiver);
        }
    }
}