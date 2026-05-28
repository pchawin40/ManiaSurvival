using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class HoldAimAbilityButton : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    public SurvivorSpiritBoltAbility spiritBoltAbility;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (spiritBoltAbility == null)
        {
            return;
        }

        spiritBoltAbility.OnSpiritBoltButtonDrag(eventData.position);
        spiritBoltAbility.OnSpiritBoltButtonDown();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (spiritBoltAbility == null)
        {
            return;
        }

        spiritBoltAbility.OnSpiritBoltButtonDrag(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (spiritBoltAbility == null)
        {
            return;
        }

        spiritBoltAbility.OnSpiritBoltButtonDrag(eventData.position);
        spiritBoltAbility.OnSpiritBoltButtonUp();
    }
}
