using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityCooldownButton : MonoBehaviour
{
    [Header("Button")]
    public Button button;
    public Image buttonImage;
    public Graphic graphicToTint;

    [Header("Text")]
    public TMP_Text abilityNameText;
    public TMP_Text cooldownText;

    [Header("Colors")]
    public Color readyColor = Color.white;
    public Color cooldownColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Header("Cooldown Authority")]
    public ManiaGameUI gameUi;
    public int abilitySlot = 1;
    public bool isPredatorSide;
    public bool useAbilityControllerCooldown = true;

    [Header("Ability Info")]
    [Tooltip("Display name shown on the button (e.g. 'Roar'). Abilities will overwrite this on Awake.")]
    public string abilityName = "";
    [Tooltip("Mana the ability costs. 0 means no mana label is shown.")]
    [Min(0)] public int manaCost = 0;
    [Tooltip("Format used to combine name + mana cost. {0} = name, {1} = cost.")]
    public string manaCostLabelFormat = "{0}\n{1} mana";
    [Tooltip("If true, the Ability Name Text is overwritten with the formatted label. Turn off if you want to author the label manually.")]
    public bool useAbilityLabel = true;

    private float legacyCooldownEndTime;

    public bool IsCoolingDown
    {
        get
        {
            if (useAbilityControllerCooldown)
            {
                AbilityController controller = ResolveAbilityController();
                return controller != null && controller.GetCooldownRemaining(abilitySlot) > 0f;
            }

            return legacyCooldownEndTime > Time.time;
        }
    }

    private void Awake()
    {
        CacheReferences();
        ApplyAbilityLabel();
        ApplyReadyVisuals();
    }

    public void BindCooldownVisual(ManiaGameUI ui, int slotNumber, bool predatorSide)
    {
        gameUi = ui;
        abilitySlot = slotNumber;
        isPredatorSide = predatorSide;
        useAbilityControllerCooldown = true;
        CacheReferences();
        ApplyReadyVisuals();
    }

    public void SetAbilityInfo(string name, int cost)
    {
        abilityName = name;
        manaCost = Mathf.Max(0, cost);
        CacheReferences();
        ApplyAbilityLabel();
    }

    public void ApplyAbilityLabel()
    {
        if (!useAbilityLabel || abilityNameText == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(abilityName))
        {
            return;
        }

        abilityNameText.text = manaCost > 0
            ? string.Format(manaCostLabelFormat, abilityName, manaCost)
            : abilityName;
    }

    private void Update()
    {
        if (useAbilityControllerCooldown)
        {
            AbilityController controller = ResolveAbilityController();
            if (controller == null)
            {
                ApplyReadyVisuals();
                return;
            }

            float remaining = controller.GetCooldownRemaining(abilitySlot);
            if (remaining > 0f)
            {
                ApplyCooldownVisuals(remaining);
            }
            else
            {
                ApplyReadyVisuals();
            }

            return;
        }

        if (!IsCoolingDown)
        {
            if (legacyCooldownEndTime > 0f)
            {
                legacyCooldownEndTime = 0f;
                ApplyReadyVisuals();
            }

            return;
        }

        ApplyCooldownVisuals(Mathf.Max(0f, legacyCooldownEndTime - Time.time));
    }

    public void StartCooldown(float duration)
    {
        if (useAbilityControllerCooldown)
        {
            return;
        }

        CacheReferences();

        float cooldownDuration = Mathf.Max(0f, duration);
        if (cooldownDuration <= 0f)
        {
            legacyCooldownEndTime = 0f;
            ApplyReadyVisuals();
            return;
        }

        legacyCooldownEndTime = Time.time + cooldownDuration;
        ApplyCooldownVisuals(cooldownDuration);
    }

    private AbilityController ResolveAbilityController()
    {
        if (gameUi == null)
        {
            gameUi = FindFirstObjectByType<ManiaGameUI>();
        }

        if (gameUi == null)
        {
            return null;
        }

        return isPredatorSide ? gameUi.predatorAbilityController : gameUi.survivorAbilityController;
    }

    private void ApplyCooldownVisuals(float remainingSeconds)
    {
        KeepButtonInteractableForTooltip();
        SetTintColor(cooldownColor);

        if (cooldownText != null)
        {
            cooldownText.text = remainingSeconds.ToString("0.0");
        }
    }

    private void ApplyReadyVisuals()
    {
        KeepButtonInteractableForTooltip();
        SetTintColor(readyColor);

        if (cooldownText != null)
        {
            cooldownText.text = string.Empty;
        }
    }

    private void KeepButtonInteractableForTooltip()
    {
        if (button != null && !button.interactable)
        {
            button.interactable = true;
        }
    }

    private void CacheReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }

        if (graphicToTint == null)
        {
            graphicToTint = buttonImage != null ? buttonImage : GetComponent<Graphic>();
        }

        if (abilityNameText == null || cooldownText == null)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                string lowerName = text.gameObject.name.ToLowerInvariant();

                if (cooldownText == null && lowerName.Contains("cooldown"))
                {
                    cooldownText = text;
                    continue;
                }

                if (abilityNameText == null)
                {
                    abilityNameText = text;
                }
            }
        }
    }

    private void SetTintColor(Color color)
    {
        if (graphicToTint != null)
        {
            graphicToTint.color = color;
        }
        else if (buttonImage != null)
        {
            buttonImage.color = color;
        }
    }
}
