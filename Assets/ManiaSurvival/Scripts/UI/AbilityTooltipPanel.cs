using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityTooltipPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject tooltipRoot;
    public float autoHideAfterSeconds = 6f;
    public Vector2 panelSize = new Vector2(250f, 100f);
    public Vector2 panelAnchoredPosition = new Vector2(-24f, 320f);

    [Header("Compact Layout")]
    public bool compactMode = true;
    public bool showDetailedTooltip = false;
    public float tooltipMaxWidth = 250f;
    public float tooltipPadding = 7f;
    public float tooltipButtonOffset = 88f;
    public float tooltipLeftFallbackOffset = 16f;
    public float titleFontSize = 22f;
    public float bodyFontSize = 18f;

    [Header("Mobile Layout")]
    public RectTransform abilityPanelAnchor;
    public float mobileMarginRight = 24f;
    public float mobileMarginBottom = 24f;
    public float mobileGapAboveAbilities = 96f;

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
        public string roleTag;
        public Color themeColor;
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
        AbilityDetail detail = GetAbilityDetail(isPredator, slotNumber);
        data.cooldown = GetLiveCooldown(isPredator, slotNumber, data.cooldown);
        data.cost = GetLiveManaCostText(isPredator, slotNumber, data.cost);
        string status = GetAbilityStatus(isPredator, slotNumber);

        ApplyTooltipFontSizes();
        SetText(tooltipAbilityNameText, data.abilityName);
        if (tooltipAbilityNameText != null && detail.themeColor.a > 0.01f)
        {
            tooltipAbilityNameText.color = detail.themeColor;
        }

        SetText(tooltipBodyText, BuildCompactBody(data, status));

        tooltipRoot.SetActive(true);
        FitPanelToContent();
        ApplyMobilePosition();
        ClampTooltipInsideCanvas();
        hideAtUnscaledTime = Time.unscaledTime + Mathf.Max(1f, autoHideAfterSeconds);
    }

    public void ConfigureMobileLayout(
        RectTransform abilityPanel,
        Vector2 size,
        float marginRight,
        float marginBottom,
        float gapAboveAbilities)
    {
        abilityPanelAnchor = abilityPanel;
        panelSize = size;
        mobileMarginRight = marginRight;
        mobileMarginBottom = marginBottom;
        mobileGapAboveAbilities = gapAboveAbilities;

        if (tooltipRoot != null)
        {
            ApplyMobilePosition();
        }
    }

    private void ApplyMobilePosition()
    {
        if (tooltipRootRect == null)
        {
            CacheRootRect();
        }

        if (tooltipRootRect == null)
        {
            return;
        }

        tooltipRootRect.sizeDelta = new Vector2(Mathf.Max(160f, tooltipMaxWidth), tooltipRootRect.sizeDelta.y);

        float gap = compactMode ? Mathf.Max(mobileGapAboveAbilities, tooltipButtonOffset) : mobileGapAboveAbilities;

        if (abilityPanelAnchor == null)
        {
            tooltipRootRect.anchorMin = new Vector2(0.5f, 0f);
            tooltipRootRect.anchorMax = new Vector2(0.5f, 0f);
            tooltipRootRect.pivot = new Vector2(0.5f, 0f);
            tooltipRootRect.anchoredPosition = panelAnchoredPosition;
            return;
        }

        float gridHeight = abilityPanelAnchor.rect.height;
        if (gridHeight <= 0f)
        {
            gridHeight = abilityPanelAnchor.sizeDelta.y;
        }

        float gridWidth = abilityPanelAnchor.rect.width;
        if (gridWidth <= 0f)
        {
            gridWidth = abilityPanelAnchor.sizeDelta.x;
        }

        tooltipRootRect.anchorMin = new Vector2(1f, 0f);
        tooltipRootRect.anchorMax = new Vector2(1f, 0f);
        tooltipRootRect.pivot = new Vector2(1f, 0f);
        tooltipRootRect.anchoredPosition = new Vector2(
            -mobileMarginRight,
            mobileMarginBottom + gridHeight + gap);

        if (RectOverlapsAbilityGrid())
        {
            tooltipRootRect.pivot = new Vector2(1f, 0.5f);
            tooltipRootRect.anchoredPosition = new Vector2(
                -mobileMarginRight - gridWidth - tooltipLeftFallbackOffset,
                mobileMarginBottom + gridHeight * 0.55f);
        }
    }

    private bool RectOverlapsAbilityGrid()
    {
        if (tooltipRootRect == null || abilityPanelAnchor == null)
        {
            return false;
        }

        Rect tooltipRect = GetScreenRect(tooltipRootRect);
        Rect gridRect = GetScreenRect(abilityPanelAnchor);
        return tooltipRect.Overlaps(gridRect);
    }

    private static Rect GetScreenRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        float minX = corners[0].x;
        float minY = corners[0].y;
        float maxX = corners[2].x;
        float maxY = corners[2].y;
        return Rect.MinMaxRect(minX, minY, maxX, maxY);
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

    private string BuildCompactBody(AbilityTooltipData data, string status)
    {
        if (!compactMode || showDetailedTooltip)
        {
            return BuildRoleTagLine(data.roleTag, data.themeColor)
                + data.description + "\n\n"
                + "Cost: " + data.cost + "\n"
                + "CD: " + data.cooldown.ToString("0.0") + "s\n"
                + status;
        }

        string body = BuildRoleTagLine(data.roleTag, data.themeColor)
            + data.description + "\n"
            + data.cost + " • " + data.cooldown.ToString("0.##") + "s CD";

        if (!string.IsNullOrEmpty(status) && status != "Ready")
        {
            body += "\n" + status;
        }

        return body;
    }

    private static string BuildRoleTagLine(string roleTag, Color themeColor)
    {
        if (string.IsNullOrWhiteSpace(roleTag))
        {
            return string.Empty;
        }

        string hex = ColorUtility.ToHtmlStringRGB(themeColor.a > 0.01f ? themeColor : Color.white);
        return "<color=#" + hex + "><size=80%>" + roleTag + "</size></color>\n";
    }

    private AbilityDetail GetAbilityDetail(bool isPredator, int slotNumber)
    {
        AbilityController controller = GetAbilityController(isPredator);
        AbilityDetail liveDetail = controller != null
            ? controller.GetAbilityDetail(slotNumber)
            : default;
        return AbilityPresentationFallback.ResolveForUi(isPredator, slotNumber, liveDetail);
    }

    private void FitPanelToContent()
    {
        if (tooltipRootRect == null)
        {
            CacheRootRect();
        }

        if (tooltipRootRect == null)
        {
            return;
        }

        float width = Mathf.Clamp(tooltipMaxWidth, 180f, 280f);
        float pad = tooltipPadding;
        float titleHeight = titleFontSize + 4f;
        float bodyHeight = bodyFontSize * 2f;

        if (tooltipAbilityNameText != null)
        {
            tooltipAbilityNameText.ForceMeshUpdate();
            titleHeight = Mathf.Max(titleFontSize + 2f, tooltipAbilityNameText.preferredHeight);
        }

        if (tooltipBodyText != null)
        {
            tooltipBodyText.ForceMeshUpdate();
            bodyHeight = Mathf.Max(bodyFontSize + 8f, tooltipBodyText.preferredHeight);
        }

        float height = pad + titleHeight + 4f + bodyHeight + pad;
        height = Mathf.Clamp(height, 72f, 120f);
        tooltipRootRect.sizeDelta = new Vector2(width, height);
    }

    private string GetLiveManaCostText(bool isPredator, int slotNumber, string fallbackCost)
    {
        AbilityController controller = GetAbilityController(isPredator);
        if (controller == null)
        {
            return fallbackCost;
        }

        float cost = controller.GetSlotManaCost(slotNumber);
        return cost.ToString("0") + " mana";
    }

    private string GetAbilityStatus(bool isPredator, int slotNumber)
    {
        AbilityController controller = GetAbilityController(isPredator);
        if (controller == null)
        {
            return "Ready";
        }

        float cooldownRemaining = controller.GetCooldownRemaining(slotNumber);
        if (cooldownRemaining > 0.05f)
        {
            return "Cooldown " + cooldownRemaining.ToString("0.0") + "s";
        }

        if (!controller.HasSufficientMana(slotNumber))
        {
            return "No Mana";
        }

        return "Ready";
    }

    private AbilityController GetAbilityController(bool isPredator)
    {
        if (gameUi == null)
        {
            gameUi = GetComponent<ManiaGameUI>();
        }

        if (gameUi == null)
        {
            return null;
        }

        return isPredator ? gameUi.predatorAbilityController : gameUi.survivorAbilityController;
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
        AbilityController controller = GetAbilityController(isPredator);
        AbilityDetail liveDetail = controller != null
            ? controller.GetAbilityDetail(slotNumber)
            : default;
        AbilityDetail detail = AbilityPresentationFallback.ResolveForUi(isPredator, slotNumber, liveDetail);

        float fallbackCooldown = controller != null
            ? controller.GetSlotCooldownDuration(slotNumber)
            : 0f;
        string fallbackCost = controller != null
            ? controller.GetSlotManaCost(slotNumber).ToString("0") + " mana"
            : string.Empty;

        return new AbilityTooltipData
        {
            className = controller != null ? controller.GetClassDisplayName() : string.Empty,
            abilityName = detail.displayName,
            description = detail.shortDescription,
            cooldown = fallbackCooldown,
            cost = fallbackCost,
            roleTag = detail.roleTag,
            themeColor = detail.themeColor
        };
    }

    private void ApplyTooltipFontSizes()
    {
        if (tooltipAbilityNameText != null)
        {
            tooltipAbilityNameText.fontSize = titleFontSize;
        }

        if (tooltipBodyText != null)
        {
            tooltipBodyText.fontSize = bodyFontSize;
            tooltipBodyText.lineSpacing = -2f;
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
        rootRect.anchorMin = new Vector2(1f, 0f);
        rootRect.anchorMax = new Vector2(1f, 0f);
        rootRect.pivot = new Vector2(1f, 0f);
        rootRect.anchoredPosition = panelAnchoredPosition;
        rootRect.sizeDelta = panelSize;

        Image background = root.AddComponent<Image>();
        background.color = new Color(0.04f, 0.06f, 0.1f, 0.92f);
        background.raycastTarget = false;

        TMP_FontAsset font = FindTooltipFont();
        float pad = tooltipPadding;

        tooltipRoot = root;
        tooltipRootRect = rootRect;
        tooltipAbilityNameText = CreateTooltipText(
            root.transform,
            "AbilityNameText",
            new Vector2(pad, -pad),
            new Vector2(-pad * 2f, titleFontSize + 6f),
            titleFontSize,
            font,
            FontStyles.Bold);
        tooltipBodyText = CreateTooltipText(
            root.transform,
            "BodyText",
            new Vector2(pad, -pad - titleFontSize - 6f),
            new Vector2(-pad * 2f, bodyFontSize * 3f),
            bodyFontSize,
            font,
            FontStyles.Normal);

        tooltipBodyText.enableWordWrapping = true;
        tooltipBodyText.overflowMode = TextOverflowModes.Overflow;
        tooltipBodyText.lineSpacing = -2f;

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
