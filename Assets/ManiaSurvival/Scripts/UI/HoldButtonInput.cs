using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class HoldButtonInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Events")]
    public UnityEvent onHoldStart;
    public UnityEvent onHoldEnd;

    public void OnPointerDown(PointerEventData eventData)
    {
        onHoldStart?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        onHoldEnd?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onHoldEnd?.Invoke();
    }
}
