using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class AbilityButtonHoldTooltip : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Ability")]
    public int abilitySlot = 1;
    public bool isPredatorButton;

    [Header("Hold")]
    public float holdThreshold = 0.35f;

    [Header("References")]
    public ManiaGameUI gameUi;
    public AbilityTooltipPanel tooltipPanel;

    [Header("Debug")]
    public bool logTooltipEvents;

    private bool pointerDown;
    private bool tooltipShownThisPress;
    private bool suppressCastOnRelease;
    private Coroutine holdRoutine;

    public void Configure(ManiaGameUI ui, AbilityTooltipPanel panel, int slot, bool isPredator, bool logEvents)
    {
        gameUi = ui;
        tooltipPanel = panel;
        abilitySlot = slot;
        isPredatorButton = isPredator;
        logTooltipEvents = logEvents;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDown = true;
        tooltipShownThisPress = false;
        suppressCastOnRelease = false;
        StopHoldRoutine();
        holdRoutine = StartCoroutine(HoldRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopHoldRoutine();

        if (tooltipShownThisPress)
        {
            HideTooltip();
        }
        else if (pointerDown && !suppressCastOnRelease)
        {
            CastAbility();
            Log("Quick tap cast slot " + abilitySlot);
        }

        ResetPressState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopHoldRoutine();

        if (tooltipShownThisPress)
        {
            HideTooltip();
        }

        ResetPressState();
    }

    private IEnumerator HoldRoutine()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, holdThreshold));

        if (!pointerDown)
        {
            yield break;
        }

        tooltipShownThisPress = true;
        suppressCastOnRelease = true;

        if (tooltipPanel != null)
        {
            tooltipPanel.Show(isPredatorButton, abilitySlot);
        }

        string roleLabel = isPredatorButton ? "Predator" : "Survivor";
        string abilityName = tooltipPanel != null ? GetAbilityNameForLog() : "Ability";
        Log("Show tooltip: " + roleLabel + " slot " + abilitySlot + " " + abilityName);
    }

    private string GetAbilityNameForLog()
    {
        if (gameUi == null)
        {
            return "Ability";
        }

        AbilityController controller = isPredatorButton
            ? gameUi.predatorAbilityController
            : gameUi.survivorAbilityController;

        if (controller == null)
        {
            return "Ability";
        }

        return controller.GetAbilityDetail(abilitySlot).displayName;
    }

    private void CastAbility()
    {
        if (gameUi == null)
        {
            return;
        }

        if (isPredatorButton)
        {
            gameUi.CastPredatorSlot(abilitySlot);
            return;
        }

        gameUi.CastSurvivorSlot(abilitySlot);
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.Hide();
        }

        Log("Hide tooltip");
    }

    private void StopHoldRoutine()
    {
        if (holdRoutine != null)
        {
            StopCoroutine(holdRoutine);
            holdRoutine = null;
        }
    }

    private void ResetPressState()
    {
        pointerDown = false;
        tooltipShownThisPress = false;
        suppressCastOnRelease = false;
    }

    private void Log(string message)
    {
        if (!logTooltipEvents)
        {
            return;
        }

        Debug.Log("[AbilityTooltip] " + message);
    }
}
