using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime-built predator class picker shown before Hunt.
/// </summary>
[DisallowMultipleComponent]
public class PredatorClassSelectPanel : MonoBehaviour
{
    private const int PanelBuildVersion = 6;

    [Header("Layout")]
    public Vector2 cardSize = new Vector2(210f, 96f);
    public float cardSpacing = 14f;
    public int cardsPerRow = 3;
    public float selectedCardScale = 1.04f;
    public Vector2 mainPanelSizePercent = new Vector2(0.78f, 0.72f);
    public float headerHeight = 78f;
    public float footerHeight = 80f;
    public Vector2 footerButtonSize = new Vector2(96f, 46f);

    [Header("Colors")]
    public Color overlayBackground = new Color(0.04f, 0.06f, 0.1f, 0.88f);
    public Color mainPanelBackground = new Color(0.1f, 0.12f, 0.17f, 0.97f);
    public Color cardBackground = new Color(0.16f, 0.18f, 0.24f, 0.98f);
    public Color cardSelectedBackground = new Color(0.22f, 0.26f, 0.34f, 0.98f);
    public Color confirmButtonColor = new Color(0.85f, 0.32f, 0.18f, 1f);
    public Color titleTextColor = new Color(0.98f, 0.99f, 1f, 1f);
    public Color bodyTextColor = new Color(0.88f, 0.9f, 0.94f, 1f);
    public Color mutedBodyTextColor = new Color(0.72f, 0.76f, 0.82f, 1f);

    [Header("Debug")]
    public bool logUiBuild = true;

    private ManiaGameUI hostUi;
    private GameObject panelRoot;
    private RectTransform mainPanelRect;
    private RectTransform cardGridContent;
    private TMP_Text detailTitleText;
    private TMP_Text detailBodyText;
    private TMP_FontAsset resolvedFont;
    private int appliedBuildVersion;
    private PredatorClass selectedClass = PredatorClass.RelentlessHook;
    private readonly List<CardBinding> cardBindings = new List<CardBinding>();
    private readonly List<GameObject> hiddenStartScreenElements = new List<GameObject>();

    private struct CardBinding
    {
        public PredatorClass classId;
        public RectTransform root;
        public Image background;
        public Image selectedOutline;
        public TMP_Text nameText;
        public GameObject selectedBadge;
    }

    public void Show(ManiaGameUI ui)
    {
        hostUi = ui;
        EnsureBuilt();

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
        }

        HideStartScreenChrome();

        if (ui != null && ui.startScreen != null)
        {
            ui.startScreen.SetActive(true);
        }

        SelectClass(selectedClass);
        ForceRefreshLayout();
    }

    public void Hide()
    {
        RestoreStartScreenChrome();

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public PredatorClass GetSelectedClass()
    {
        return selectedClass;
    }

    public bool HasReadableTabLabels()
    {
        if (cardBindings.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < cardBindings.Count; i++)
        {
            if (cardBindings[i].nameText == null || string.IsNullOrWhiteSpace(cardBindings[i].nameText.text))
            {
                return false;
            }

            if (cardBindings[i].nameText.font == null)
            {
                return false;
            }
        }

        return true;
    }

    private void EnsureBuilt()
    {
        if (panelRoot != null && appliedBuildVersion == PanelBuildVersion)
        {
            return;
        }

        DestroyPanel();
        resolvedFont = ResolveFont();

        if (logUiBuild)
        {
            Debug.Log("[PredatorClassUI] Building class select menu...");
        }

        Transform parent = hostUi != null && hostUi.startScreen != null
            ? hostUi.startScreen.transform
            : transform;

        panelRoot = CreateUiObject("ClassSelectRoot", parent);
        StretchRect(panelRoot);

        Image overlay = panelRoot.AddComponent<Image>();
        overlay.color = overlayBackground;
        overlay.raycastTarget = true;

        CanvasGroup overlayGroup = panelRoot.AddComponent<CanvasGroup>();
        overlayGroup.alpha = 1f;
        overlayGroup.interactable = true;
        overlayGroup.blocksRaycasts = true;

        GameObject dimOverlay = CreateUiObject("DimOverlay", panelRoot.transform);
        StretchRect(dimOverlay);
        Image dimImage = dimOverlay.AddComponent<Image>();
        dimImage.color = Color.clear;
        dimImage.raycastTarget = true;

        GameObject mainPanel = CreateUiObject("MainPanel", panelRoot.transform);
        mainPanelRect = GetRectTransform(mainPanel);
        mainPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        mainPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        mainPanelRect.pivot = new Vector2(0.5f, 0.5f);
        mainPanelRect.sizeDelta = new Vector2(
            Screen.width * mainPanelSizePercent.x,
            Screen.height * mainPanelSizePercent.y);

        Image mainBg = mainPanel.AddComponent<Image>();
        mainBg.color = mainPanelBackground;
        mainBg.raycastTarget = true;

        VerticalLayoutGroup mainLayout = mainPanel.AddComponent<VerticalLayoutGroup>();
        mainLayout.padding = new RectOffset(24, 24, 24, 24);
        mainLayout.spacing = 16f;
        mainLayout.childAlignment = TextAnchor.UpperCenter;
        mainLayout.childControlWidth = true;
        mainLayout.childControlHeight = true;
        mainLayout.childForceExpandWidth = true;
        mainLayout.childForceExpandHeight = false;

        CreateHeaderContainer(mainPanel.transform);
        CreateContentContainer(mainPanel.transform);
        CreateFooterContainer(mainPanel.transform);

        appliedBuildVersion = PanelBuildVersion;
        ForceRefreshLayout();

        if (logUiBuild && cardGridContent != null)
        {
            Debug.Log("[PredatorClassUI] Class card grid children=" + cardGridContent.childCount);
        }
    }

    private void DestroyPanel()
    {
        if (panelRoot != null)
        {
            Destroy(panelRoot);
            panelRoot = null;
        }

        cardBindings.Clear();
        detailTitleText = null;
        detailBodyText = null;
        cardGridContent = null;
        mainPanelRect = null;
        RestoreStartScreenChrome();
        hiddenStartScreenElements.Clear();
    }

    private void HideStartScreenChrome()
    {
        RestoreStartScreenChrome();

        if (hostUi == null || hostUi.startScreen == null || panelRoot == null)
        {
            return;
        }

        Transform startRoot = hostUi.startScreen.transform;
        for (int i = 0; i < startRoot.childCount; i++)
        {
            Transform child = startRoot.GetChild(i);
            if (child.gameObject == panelRoot)
            {
                continue;
            }

            if (!child.gameObject.activeSelf)
            {
                continue;
            }

            hiddenStartScreenElements.Add(child.gameObject);
            child.gameObject.SetActive(false);
        }
    }

    private void RestoreStartScreenChrome()
    {
        for (int i = 0; i < hiddenStartScreenElements.Count; i++)
        {
            GameObject element = hiddenStartScreenElements[i];
            if (element != null)
            {
                element.SetActive(true);
            }
        }

        hiddenStartScreenElements.Clear();
    }

    private void CreateHeaderContainer(Transform parent)
    {
        GameObject header = CreateUiObject("HeaderContainer", parent);
        LayoutElement headerLayout = header.AddComponent<LayoutElement>();
        headerLayout.minHeight = 70f;
        headerLayout.preferredHeight = headerHeight;
        headerLayout.flexibleHeight = 0f;

        VerticalLayoutGroup headerGroup = header.AddComponent<VerticalLayoutGroup>();
        headerGroup.spacing = 4f;
        headerGroup.childAlignment = TextAnchor.UpperLeft;
        headerGroup.childControlWidth = true;
        headerGroup.childControlHeight = true;
        headerGroup.childForceExpandWidth = true;
        headerGroup.childForceExpandHeight = false;

        GameObject title = CreateTextObject(header.transform, "TitleText", "Choose Your Predator", 36f, FontStyles.Bold, titleTextColor);
        LayoutElement titleLayout = title.GetComponent<LayoutElement>();
        titleLayout.preferredHeight = 44f;
        titleLayout.flexibleHeight = 0f;

        GameObject subtitle = CreateTextObject(
            header.transform,
            "SubtitleText",
            "Pick a hunter. Each predator changes how the match feels.",
            18f,
            FontStyles.Italic,
            mutedBodyTextColor);
        LayoutElement subtitleLayout = subtitle.GetComponent<LayoutElement>();
        subtitleLayout.preferredHeight = 28f;
        subtitleLayout.flexibleHeight = 0f;
    }

    private void CreateContentContainer(Transform parent)
    {
        float gridHeight = cardSize.y * 2f + cardSpacing + 8f;

        GameObject content = CreateUiObject("ContentContainer", parent);
        LayoutElement contentLayout = content.AddComponent<LayoutElement>();
        contentLayout.minHeight = gridHeight;
        contentLayout.preferredHeight = Mathf.Max(gridHeight, 220f);
        contentLayout.flexibleHeight = 1f;

        HorizontalLayoutGroup row = content.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 24f;
        row.padding = new RectOffset(0, 0, 0, 0);
        row.childAlignment = TextAnchor.UpperCenter;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = true;
        row.childForceExpandHeight = false;

        CreateCardGrid(content.transform, gridHeight);
        CreateDetailSection(content.transform, gridHeight);
    }

    private void CreateCardGrid(Transform parent, float targetHeight)
    {
        GameObject leftPanel = CreateUiObject("LeftCardsPanel", parent);
        LayoutElement leftLayout = leftPanel.AddComponent<LayoutElement>();
        leftLayout.flexibleWidth = 0.55f;
        leftLayout.minWidth = 420f;
        leftLayout.preferredHeight = targetHeight;
        leftLayout.minHeight = targetHeight;
        leftLayout.flexibleHeight = 0f;

        GameObject gridHost = CreateUiObject("ClassCardGrid", leftPanel.transform);
        StretchRect(gridHost);

        cardGridContent = GetRectTransform(gridHost);
        GridLayoutGroup grid = gridHost.AddComponent<GridLayoutGroup>();
        grid.cellSize = cardSize;
        grid.spacing = new Vector2(cardSpacing, cardSpacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cardsPerRow;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.padding = new RectOffset(4, 4, 4, 4);

        cardBindings.Clear();
        IReadOnlyList<PredatorClass> playable = PredatorClassCatalog.GetPlayableClasses();
        for (int i = 0; i < playable.Count; i++)
        {
            CreateClassCard(cardGridContent, playable[i]);
        }
    }

    private void CreateClassCard(Transform parent, PredatorClass classId)
    {
        PredatorClassDetail detail = PredatorClassCatalog.GetDetail(classId);
        string cardTitle = detail.displayName;
        string roleLabel = GetCardRoleLabel(detail);
        string abilityLine = BuildAbilityLine(detail);

        GameObject card = CreateUiObject("Card_" + detail.tabShortName, parent);
        RectTransform cardRect = GetRectTransform(card);

        LayoutElement cardLayout = card.AddComponent<LayoutElement>();
        cardLayout.preferredWidth = cardSize.x;
        cardLayout.preferredHeight = cardSize.y;
        cardLayout.minWidth = cardSize.x;
        cardLayout.minHeight = cardSize.y;

        Image bg = card.AddComponent<Image>();
        bg.color = Color.Lerp(cardBackground, detail.themeColor, 0.28f);
        bg.raycastTarget = true;

        Button button = card.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(() => SelectClass(classId));

        GameObject outlineObj = CreateUiObject("SelectedOutline", card.transform);
        outlineObj.transform.SetAsFirstSibling();
        Image outline = outlineObj.AddComponent<Image>();
        outline.color = detail.themeColor;
        outline.raycastTarget = false;
        StretchRect(outlineObj);
        RectTransform outlineRect = GetRectTransform(outlineObj);
        outlineRect.offsetMin = new Vector2(-4f, -4f);
        outlineRect.offsetMax = new Vector2(4f, 4f);
        outline.enabled = false;

        GameObject accent = CreateUiObject("Accent", card.transform);
        Image accentImage = accent.AddComponent<Image>();
        accentImage.color = detail.themeColor;
        accentImage.raycastTarget = false;
        RectTransform accentRect = GetRectTransform(accent);
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.sizeDelta = new Vector2(0f, 7f);
        accentRect.anchoredPosition = Vector2.zero;

        GameObject contentRoot = CreateUiObject("Content", card.transform);
        StretchRect(contentRoot);
        RectTransform contentRect = GetRectTransform(contentRoot);
        contentRect.offsetMin = new Vector2(12f, 10f);
        contentRect.offsetMax = new Vector2(-12f, -10f);

        VerticalLayoutGroup contentLayout = contentRoot.AddComponent<VerticalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.spacing = 3f;

        GameObject nameObj = CreateTextObject(contentRoot.transform, "CardName", cardTitle, 20f, FontStyles.Bold, titleTextColor);
        TMP_Text nameText = nameObj.GetComponent<TMP_Text>();
        nameText.enableWordWrapping = true;
        ApplyLabelOutline(nameText);
        LayoutElement nameLayout = nameObj.GetComponent<LayoutElement>();
        nameLayout.preferredHeight = 26f;

        GameObject roleObj = CreateTextObject(contentRoot.transform, "CardRole", roleLabel, 15f, FontStyles.Bold, bodyTextColor);
        ApplyLabelOutline(roleObj.GetComponent<TMP_Text>());
        LayoutElement roleLayout = roleObj.GetComponent<LayoutElement>();
        roleLayout.preferredHeight = 20f;

        GameObject abilityObj = CreateTextObject(contentRoot.transform, "CardAbilities", abilityLine, 13f, FontStyles.Normal, mutedBodyTextColor);
        TMP_Text abilityText = abilityObj.GetComponent<TMP_Text>();
        abilityText.enableWordWrapping = true;
        LayoutElement abilityLayout = abilityObj.GetComponent<LayoutElement>();
        abilityLayout.preferredHeight = 34f;

        GameObject selectedObj = CreateTextObject(contentRoot.transform, "SelectedBadge", "SELECTED", 12f, FontStyles.Bold, titleTextColor);
        TMP_Text selectedText = selectedObj.GetComponent<TMP_Text>();
        selectedText.alignment = TextAlignmentOptions.Center;
        ApplyLabelOutline(selectedText);
        LayoutElement selectedLayout = selectedObj.GetComponent<LayoutElement>();
        selectedLayout.preferredHeight = 16f;
        selectedObj.SetActive(false);

        if (nameText.font == null)
        {
            Debug.LogWarning("[PredatorClassUI] Card " + cardTitle + " missing TMP font.");
        }

        cardBindings.Add(new CardBinding
        {
            classId = classId,
            root = cardRect,
            background = bg,
            selectedOutline = outline,
            nameText = nameText,
            selectedBadge = selectedObj
        });

        if (logUiBuild)
        {
            Debug.Log("[PredatorClassUI] Created card " + cardTitle + " active=" + card.activeSelf
                + " size=" + cardSize.x.ToString("0") + "x" + cardSize.y.ToString("0"));
        }
    }

    private void CreateDetailSection(Transform parent, float targetHeight)
    {
        GameObject detailBox = CreateUiObject("RightDetailsPanel", parent);
        LayoutElement detailLayoutElement = detailBox.AddComponent<LayoutElement>();
        detailLayoutElement.flexibleWidth = 0.4f;
        detailLayoutElement.minWidth = 280f;
        detailLayoutElement.preferredHeight = targetHeight;
        detailLayoutElement.minHeight = targetHeight;
        detailLayoutElement.flexibleHeight = 0f;

        Image detailBg = detailBox.AddComponent<Image>();
        detailBg.color = new Color(0.08f, 0.1f, 0.14f, 0.98f);
        detailBg.raycastTarget = true;

        VerticalLayoutGroup detailGroup = detailBox.AddComponent<VerticalLayoutGroup>();
        detailGroup.padding = new RectOffset(16, 16, 14, 14);
        detailGroup.spacing = 8f;
        detailGroup.childAlignment = TextAnchor.UpperLeft;
        detailGroup.childControlWidth = true;
        detailGroup.childControlHeight = true;
        detailGroup.childForceExpandWidth = true;
        detailGroup.childForceExpandHeight = false;

        GameObject titleObj = CreateTextObject(detailBox.transform, "DetailTitle", "Swarm Overlord", 26f, FontStyles.Bold, titleTextColor);
        detailTitleText = titleObj.GetComponent<TMP_Text>();
        LayoutElement titleLayout = titleObj.GetComponent<LayoutElement>();
        titleLayout.preferredHeight = 32f;
        titleLayout.flexibleHeight = 0f;

        GameObject bodyObj = CreateTextObject(detailBox.transform, "DetailBody", string.Empty, 15f, FontStyles.Normal, bodyTextColor);
        detailBodyText = bodyObj.GetComponent<TMP_Text>();
        detailBodyText.enableWordWrapping = true;
        detailBodyText.overflowMode = TextOverflowModes.Overflow;
        LayoutElement bodyLayout = bodyObj.GetComponent<LayoutElement>();
        bodyLayout.flexibleHeight = 1f;
        bodyLayout.preferredHeight = targetHeight - 56f;
        bodyLayout.minHeight = 120f;
    }

    private void CreateFooterContainer(Transform parent)
    {
        GameObject footer = CreateUiObject("FooterContainer", parent);
        LayoutElement footerElement = footer.AddComponent<LayoutElement>();
        footerElement.minHeight = 70f;
        footerElement.preferredHeight = footerHeight;
        footerElement.flexibleHeight = 0f;

        HorizontalLayoutGroup footerLayout = footer.AddComponent<HorizontalLayoutGroup>();
        footerLayout.padding = new RectOffset(0, 24, 8, 0);
        footerLayout.spacing = 12f;
        footerLayout.childAlignment = TextAnchor.MiddleRight;
        footerLayout.childControlWidth = false;
        footerLayout.childControlHeight = true;
        footerLayout.childForceExpandWidth = false;
        footerLayout.childForceExpandHeight = false;

        Button backButton = CreateFooterButton(footer.transform, "BackButton", "Back", new Color(0.35f, 0.38f, 0.45f, 1f));
        backButton.onClick.AddListener(HandleBackPressed);

        Button huntButton = CreateFooterButton(footer.transform, "HuntButton", "Hunt", confirmButtonColor);
        huntButton.onClick.AddListener(HandleHuntPressed);
    }

    private Button CreateFooterButton(Transform parent, string name, string label, Color color)
    {
        GameObject buttonObj = CreateUiObject(name, parent);
        Image image = buttonObj.AddComponent<Image>();
        image.color = color;
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;

        LayoutElement layout = buttonObj.AddComponent<LayoutElement>();
        layout.preferredWidth = footerButtonSize.x;
        layout.preferredHeight = footerButtonSize.y;
        layout.minWidth = footerButtonSize.x;
        layout.minHeight = footerButtonSize.y;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;

        GameObject textObj = CreateTextObject(buttonObj.transform, "Label", label, 22f, FontStyles.Bold, titleTextColor);
        TMP_Text text = textObj.GetComponent<TMP_Text>();
        text.alignment = TextAlignmentOptions.Center;
        StretchRect(textObj);

        return button;
    }

    private void SelectClass(PredatorClass classId)
    {
        selectedClass = classId;
        PredatorClassDetail detail = PredatorClassCatalog.GetDetail(classId);

        if (detailTitleText != null)
        {
            detailTitleText.text = detail.displayName;
            detailTitleText.color = detail.themeColor;
        }

        if (detailBodyText != null)
        {
            detailBodyText.text = BuildDetailPanelBody(detail);
        }

        for (int i = 0; i < cardBindings.Count; i++)
        {
            CardBinding binding = cardBindings[i];
            bool selected = binding.classId == classId;
            PredatorClassDetail cardDetail = PredatorClassCatalog.GetDetail(binding.classId);

            if (binding.background != null)
            {
                binding.background.color = selected
                    ? Color.Lerp(cardSelectedBackground, cardDetail.themeColor, 0.45f)
                    : Color.Lerp(cardBackground, cardDetail.themeColor, 0.28f);
            }

            if (binding.selectedOutline != null)
            {
                binding.selectedOutline.enabled = selected;
            }

            if (binding.selectedBadge != null)
            {
                binding.selectedBadge.SetActive(selected);
            }

            if (binding.root != null)
            {
                binding.root.localScale = selected ? Vector3.one * selectedCardScale : Vector3.one;
            }
        }

        if (logUiBuild)
        {
            Debug.Log("[PredatorClassUI] Selected " + detail.displayName);
        }

        ForceRefreshLayout();
    }

    private static string GetCardRoleLabel(PredatorClassDetail detail)
    {
        if (string.IsNullOrWhiteSpace(detail.shortRole))
        {
            return "Predator";
        }

        int spaceIndex = detail.shortRole.IndexOf(' ');
        if (spaceIndex > 0)
        {
            return detail.shortRole.Substring(0, spaceIndex);
        }

        return detail.shortRole;
    }

    private static string BuildAbilityLine(PredatorClassDetail detail)
    {
        if (detail.abilityNames == null || detail.abilityNames.Length == 0)
        {
            return string.Empty;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int i = 0; i < detail.abilityNames.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(detail.abilityNames[i]))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(" • ");
            }

            builder.Append(detail.abilityNames[i]);
        }

        return builder.ToString();
    }

    private static string BuildDetailPanelBody(PredatorClassDetail detail)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append("Role: ").Append(detail.shortRole).Append('\n');
        string style = string.IsNullOrWhiteSpace(detail.styleLine) ? detail.tagline : detail.styleLine;
        builder.Append("Style: ").Append(style).Append("\n\nAbilities:\n");

        for (int i = 0; i < 4; i++)
        {
            string name = detail.GetAbilityName(i);
            string desc = detail.GetAbilityShortDescription(i);
            builder.Append(i + 1).Append(". ").Append(name);
            if (!string.IsNullOrWhiteSpace(desc))
            {
                builder.Append(" — ").Append(desc);
            }

            if (i < 3)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }

    private void HandleBackPressed()
    {
        Hide();
    }

    private void HandleHuntPressed()
    {
        if (hostUi != null)
        {
            hostUi.ConfirmPredatorClassAndStart(selectedClass);
            return;
        }

        Hide();
    }

    private void ForceRefreshLayout()
    {
        Canvas.ForceUpdateCanvases();

        if (mainPanelRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(mainPanelRect);
        }

        if (panelRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetRectTransform(panelRoot));
        }

        if (cardGridContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardGridContent);
        }
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static RectTransform GetRectTransform(GameObject obj)
    {
        return obj.GetComponent<RectTransform>();
    }

    private static void StretchRect(GameObject obj)
    {
        RectTransform rect = GetRectTransform(obj);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private TMP_FontAsset ResolveFont()
    {
        if (resolvedFont != null)
        {
            return resolvedFont;
        }

        if (hostUi != null && hostUi.timerText != null && hostUi.timerText.font != null)
        {
            resolvedFont = hostUi.timerText.font;
            return resolvedFont;
        }

        TMP_Text anyText = FindFirstObjectByType<TMP_Text>();
        if (anyText != null && anyText.font != null)
        {
            resolvedFont = anyText.font;
            return resolvedFont;
        }

        resolvedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (resolvedFont != null)
        {
            return resolvedFont;
        }

        resolvedFont = TMP_Settings.defaultFontAsset;
        return resolvedFont;
    }

    private static void ApplyLabelOutline(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        text.outlineWidth = 0.2f;
        text.outlineColor = new Color(0f, 0f, 0f, 0.85f);
    }

    private GameObject CreateTextObject(Transform parent, string name, string text, float fontSize, FontStyles style, Color color)
    {
        GameObject obj = CreateUiObject(name, parent);
        RectTransform rect = GetRectTransform(obj);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, fontSize + 12f);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = ResolveFont();
        if (font != null)
        {
            tmp.font = font;
        }
        else
        {
            Debug.LogWarning("[PredatorClassUI] TMP font missing for " + name);
        }

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.ForceMeshUpdate();

        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.minHeight = fontSize + 8f;
        layout.preferredHeight = fontSize + 12f;
        layout.flexibleWidth = 1f;

        return obj;
    }
}
