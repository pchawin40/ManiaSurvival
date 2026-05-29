using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class HoldAimAbilityButton : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    public SurvivorSpiritBoltAbility spiritBoltAbility;
    private float pressTime;
    private bool isPressed;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (spiritBoltAbility == null)
        {
            return;
        }

        isPressed = true;
        pressTime = Time.time;
        spiritBoltAbility.OnSpiritBoltButtonPressed(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (spiritBoltAbility == null)
        {
            return;
        }

        spiritBoltAbility.OnSpiritBoltButtonDragged(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (spiritBoltAbility == null)
        {
            return;
        }

        float heldDuration = isPressed ? Mathf.Max(0f, Time.time - pressTime) : 0f;
        isPressed = false;
        spiritBoltAbility.OnSpiritBoltButtonReleased(eventData.position, heldDuration);
    }
}
