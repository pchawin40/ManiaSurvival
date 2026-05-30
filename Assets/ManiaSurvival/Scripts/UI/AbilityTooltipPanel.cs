using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityTooltipPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject tooltipRoot;
    public float autoHideAfterSeconds = 6f;
    public Vector2 panelSize = new Vector2(620f, 240f);
    public Vector2 panelAnchoredPosition = new Vector2(0f, 300f);

    [Header("Text")]
    public TMP_Text tooltipAbilityNameText;
    public TMP_Text tooltipBodyText;

    [Header("References")]
    public ManiaGameUI gameUi;

    private RectTransform tooltipRootRect;
    private float hideAtUnscaledTime;
    private bool loggedMissingFields;

    private struct AbilityTooltipData
    {
        public string className;
        public string abilityName;
        public string description;
        public float cooldown;
        public string effect;
        public string cost;
        public string rangeTarget;
        public string tip;
    }

    private void Awake()
    {
        if (tooltipRoot == null)
        {
            BuildRuntimeTooltipUi();
        }

        CacheRootRect();
        HideImmediate();
    }

    private void Update()
    {
        if (tooltipRoot == null || !tooltipRoot.activeSelf)
        {
            return;
        }

        if (Time.unscaledTime >= hideAtUnscaledTime)
        {
            Hide();
        }
    }

    public void Show(bool isPredator, int slotNumber)
    {
        if (tooltipRoot != null && tooltipBodyText == null)
        {
            Destroy(tooltipRoot);
            tooltipRoot = null;
            tooltipRootRect = null;
            tooltipAbilityNameText = null;
        }

        if (tooltipRoot == null)
        {
            BuildRuntimeTooltipUi();
        }

        if (tooltipRoot == null)
        {
            return;
        }

        AbilityTooltipData data = GetTooltipData(isPredator, slotNumber);
        data.cooldown = GetLiveCooldown(isPredator, slotNumber, data.cooldown);

        SetText(tooltipAbilityNameText, data.abilityName);
        SetText(tooltipBodyText, BuildCompactBody(data));

        tooltipRoot.SetActive(true);
        ClampTooltipInsideCanvas();
        hideAtUnscaledTime = Time.unscaledTime + Mathf.Max(1f, autoHideAfterSeconds);
    }

    public void Hide()
    {
        HideImmediate();
    }

    private void HideImmediate()
    {
        if (tooltipRoot != null)
        {
            tooltipRoot.SetActive(false);
        }
    }

    private string BuildCompactBody(AbilityTooltipData data)
    {
        return data.description + "\n\n"
            + "Cooldown: " + data.cooldown.ToString("0.0") + "s\n"
            + "Effect: " + data.effect + "\n"
            + "Tip: " + data.tip;
    }

    private void ClampTooltipInsideCanvas()
    {
        if (tooltipRootRect == null)
        {
            CacheRootRect();
        }

        if (tooltipRootRect == null)
        {
            return;
        }

        Canvas canvas = tooltipRootRect.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        Vector3[] corners = new Vector3[4];
        tooltipRootRect.GetWorldCorners(corners);

        Vector3[] canvasCorners = new Vector3[4];
        canvasRect.GetWorldCorners(canvasCorners);

        float minX = canvasCorners[0].x;
        float maxX = canvasCorners[2].x;
        float minY = canvasCorners[0].y;
        float maxY = canvasCorners[2].y;

        float offsetX = 0f;
        float offsetY = 0f;

        if (corners[0].x < minX)
        {
            offsetX += minX - corners[0].x;
        }
        else if (corners[2].x > maxX)
        {
            offsetX -= corners[2].x - maxX;
        }

        if (corners[0].y < minY)
        {
            offsetY += minY - corners[0].y;
        }
        else if (corners[2].y > maxY)
        {
            offsetY -= corners[2].y - maxY;
        }

        if (Mathf.Abs(offsetX) > 0.01f || Mathf.Abs(offsetY) > 0.01f)
        {
            tooltipRootRect.position += new Vector3(offsetX, offsetY, 0f);
        }
    }

    private void CacheRootRect()
    {
        if (tooltipRoot != null)
        {
            tooltipRootRect = tooltipRoot.GetComponent<RectTransform>();
        }
    }

    private float GetLiveCooldown(bool isPredator, int slotNumber, float fallbackCooldown)
    {
        if (gameUi == null)
        {
            gameUi = GetComponent<ManiaGameUI>();
        }

        if (gameUi == null)
        {
            return fallbackCooldown;
        }

        AbilityController controller = isPredator ? gameUi.predatorAbilityController : gameUi.survivorAbilityController;
        if (controller == null)
        {
            return fallbackCooldown;
        }

        switch (slotNumber)
        {
            case 1:
                return isPredator ? controller.predatorSlot1Cooldown : controller.survivorSlot1Cooldown;
            case 2:
                return isPredator ? controller.predatorSlot2Cooldown : controller.survivorSlot2Cooldown;
            case 3:
                return isPredator ? controller.predatorSlot3Cooldown : controller.survivorSlot3Cooldown;
            default:
                return isPredator ? controller.predatorSlot4Cooldown : controller.survivorSlot4Cooldown;
        }
    }

    private AbilityTooltipData GetTooltipData(bool isPredator, int slotNumber)
    {
        if (isPredator)
        {
            switch (slotNumber)
            {
                case 1:
                    return new AbilityTooltipData
                    {
                        className = "Relentless Hook",
                        abilityName = "Spray",
                        description = "Short-range cone burst.",
                        cooldown = 1.4f,
                        effect = "Cone damage",
                        cost = "None",
                        rangeTarget = "Short cone in front",
                        tip = "Fight up close."
                    };
                case 2:
                    return new AbilityTooltipData
                    {
                        className = "Relentless Hook",
                        abilityName = "Hook",
                        description = "Pulls one Survivor toward you.",
                        cooldown = 8f,
                        effect = "Pull target",
                        cost = "None",
                        rangeTarget = "Line skillshot",
                        tip = "Catch isolated prey."
                    };
                case 3:
                    return new AbilityTooltipData
                    {
                        className = "Relentless Hook",
                        abilityName = "Tonic",
                        description = "Recover and reduce damage briefly.",
                        cooldown = 10f,
                        effect = "Self sustain",
                        cost = "None",
                        rangeTarget = "Self",
                        tip = "Use before diving."
                    };
                default:
                    return new AbilityTooltipData
                    {
                        className = "Relentless Hook",
                        abilityName = "Barrage",
                        description = "Rapid cone knockback blasts for 2.4 seconds.",
                        cooldown = 14f,
                        effect = "2 damage + knockback every 0.3s",
                        cost = "None",
                        rangeTarget = "8 unit cone in front",
                        tip = "Scatter survivors."
                    };
            }
        }

        switch (slotNumber)
        {
            case 1:
                return new AbilityTooltipData
                {
                    className = "Field Medic",
                    abilityName = "Heal Dart",
                    description = "Restores health to a wounded ally.",
                    cooldown = 2.5f,
                    effect = "Heal 6 HP",
                    cost = "None",
                    rangeTarget = "Nearest wounded ally or self if hurt",
                    tip = "Save hurt allies."
                };
            case 2:
                return new AbilityTooltipData
                {
                    className = "Field Medic",
                    abilityName = "Heal Pulse",
                    description = "Emits a healing wave around you.",
                    cooldown = 6f,
                    effect = "Area heal 4 HP",
                    cost = "None",
                    rangeTarget = "Area around caster (5 units)",
                    tip = "Use near allies."
                };
            case 3:
                return new AbilityTooltipData
                {
                    className = "Field Medic",
                    abilityName = "Tether",
                    description = "Dash quickly toward an ally.",
                    cooldown = 10f,
                    effect = "Mobility",
                    cost = "None",
                    rangeTarget = "Nearest ally",
                    tip = "Escape or regroup."
                };
            default:
                return new AbilityTooltipData
                {
                    className = "Field Medic",
                    abilityName = "Sanctuary",
                    description = "Drops a healing zone at your position.",
                    cooldown = 16f,
                    effect = "Healing zone: 2 HP/s for 4s",
                    cost = "None",
                    rangeTarget = "Area at caster",
                    tip = "Use under pressure."
                };
        }
    }

    private void SetText(TMP_Text label, string value)
    {
        if (label == null)
        {
            LogMissingFieldsOnce();
            return;
        }

        label.text = value;
    }

    private void LogMissingFieldsOnce()
    {
        if (loggedMissingFields)
        {
            return;
        }

        loggedMissingFields = true;
        Debug.Log("[AbilityTooltip] Some tooltip text fields are unassigned. Missing lines will be skipped.");
    }

    private void BuildRuntimeTooltipUi()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;

        GameObject root = new GameObject("AbilityTooltipPanel");
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0f);
        rootRect.anchorMax = new Vector2(0.5f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.anchoredPosition = panelAnchoredPosition;
        rootRect.sizeDelta = panelSize;

        Image background = root.AddComponent<Image>();
        background.color = new Color(0.04f, 0.06f, 0.1f, 0.94f);
        background.raycastTarget = false;

        TMP_FontAsset font = FindTooltipFont();

        tooltipRoot = root;
        tooltipRootRect = rootRect;
        tooltipAbilityNameText = CreateTooltipText(
            root.transform,
            "AbilityNameText",
            new Vector2(20f, -16f),
            new Vector2(-40f, 40f),
            32f,
            font,
            FontStyles.Bold);
        tooltipBodyText = CreateTooltipText(
            root.transform,
            "BodyText",
            new Vector2(20f, -62f),
            new Vector2(-40f, 150f),
            24f,
            font,
            FontStyles.Normal);

        tooltipBodyText.enableWordWrapping = true;
        tooltipBodyText.overflowMode = TextOverflowModes.Overflow;
        tooltipBodyText.lineSpacing = 4f;

        tooltipRoot.SetActive(false);
    }

    private TMP_Text CreateTooltipText(
        Transform parent,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        float fontSize,
        TMP_FontAsset font,
        FontStyles style)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = new Color(0.95f, 0.97f, 1f, 1f);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private TMP_FontAsset FindTooltipFont()
    {
        if (gameUi != null && gameUi.timerText != null && gameUi.timerText.font != null)
        {
            return gameUi.timerText.font;
        }

        TMP_Text anyText = FindFirstObjectByType<TMP_Text>();
        if (anyText != null && anyText.font != null)
        {
            return anyText.font;
        }

        return TMP_Settings.defaultFontAsset;
    }
}
