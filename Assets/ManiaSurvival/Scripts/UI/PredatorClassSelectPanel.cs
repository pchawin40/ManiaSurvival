using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime-built predator class picker shown before Hunt. Builds under startScreen when scene refs are missing.
/// </summary>
[DisallowMultipleComponent]
public class PredatorClassSelectPanel : MonoBehaviour
{
    [Header("Layout")]
    public Vector2 panelSize = new Vector2(920f, 620f);
    public Vector2 cardSize = new Vector2(280f, 220f);
    public float cardSpacing = 12f;
    public int cardsPerRow = 3;

    [Header("Colors")]
    public Color panelBackground = new Color(0.08f, 0.1f, 0.14f, 0.94f);
    public Color cardBackground = new Color(0.14f, 0.16f, 0.22f, 0.95f);
    public Color cardSelectedBackground = new Color(0.2f, 0.24f, 0.32f, 0.98f);
    public Color confirmButtonColor = new Color(0.85f, 0.32f, 0.18f, 1f);
    public Color defaultBodyTextColor = new Color(0.92f, 0.94f, 0.98f, 1f);
    public Color mutedBodyTextColor = new Color(0.78f, 0.82f, 0.88f, 1f);

    private ManiaGameUI hostUi;
    private GameObject panelRoot;
    private RectTransform cardGridContent;
    private TMP_Text detailTitleText;
    private TMP_Text detailBodyText;
    private Button huntButton;
    private TMP_FontAsset resolvedFont;
    private PredatorClass selectedClass = PredatorClass.RelentlessHook;
    private readonly List<CardBinding> cardBindings = new List<CardBinding>();

    private struct CardBinding
    {
        public PredatorClass classId;
        public Image background;
        public Image accentBar;
        public Image selectedOutline;
        public GameObject checkmark;
        public TMP_Text nameText;
    }

    public void Show(ManiaGameUI ui)
    {
        hostUi = ui;
        EnsureBuilt();

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (ui != null && ui.startScreen != null)
        {
            ui.startScreen.SetActive(true);
        }

        SelectClass(selectedClass);
        ForceRefreshLayout();
    }

    public void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public PredatorClass GetSelectedClass()
    {
        return selectedClass;
    }

    private void EnsureBuilt()
    {
        if (panelRoot != null)
        {
            return;
        }

        resolvedFont = ResolveFont();

        Transform parent = hostUi != null && hostUi.startScreen != null
            ? hostUi.startScreen.transform
            : transform;

        panelRoot = new GameObject("PredatorClassSelectPanel");
        panelRoot.transform.SetParent(parent, false);

        RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panelRoot.AddComponent<Image>();
        panelBg.color = panelBackground;

        VerticalLayoutGroup rootLayout = panelRoot.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(24, 24, 24, 24);
        rootLayout.spacing = 16f;
        rootLayout.childAlignment = TextAnchor.UpperCenter;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = false;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        CreateHeader(panelRoot.transform);
        CreateCardGrid(panelRoot.transform);
        CreateDetailSection(panelRoot.transform);
        CreateFooterButtons(panelRoot.transform);

        ForceRefreshLayout();
    }

    private void CreateHeader(Transform parent)
    {
        GameObject header = CreateTextObject(parent, "HeaderTitle", "Choose Your Predator", 34f, FontStyles.Bold, defaultBodyTextColor);
        LayoutElement headerLayout = header.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 48f;

        GameObject subtitle = CreateTextObject(parent, "HeaderSubtitle", "Pick a class, review abilities, then Hunt.", 20f, FontStyles.Italic, mutedBodyTextColor);
        LayoutElement subtitleLayout = subtitle.AddComponent<LayoutElement>();
        subtitleLayout.preferredHeight = 28f;
    }

    private void CreateCardGrid(Transform parent)
    {
        GameObject scrollHost = new GameObject("CardScroll");
        scrollHost.transform.SetParent(parent, false);
        LayoutElement scrollLayout = scrollHost.AddComponent<LayoutElement>();
        scrollLayout.preferredHeight = 460f;
        scrollLayout.flexibleHeight = 1f;

        ScrollRect scroll = scrollHost.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollHost.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        cardGridContent = content.AddComponent<RectTransform>();
        cardGridContent.anchorMin = new Vector2(0f, 1f);
        cardGridContent.anchorMax = new Vector2(1f, 1f);
        cardGridContent.pivot = new Vector2(0.5f, 1f);
        cardGridContent.anchoredPosition = Vector2.zero;

        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = cardSize;
        grid.spacing = new Vector2(cardSpacing, cardSpacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cardsPerRow;
        grid.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewportRect;
        scroll.content = cardGridContent;

        cardBindings.Clear();
        IReadOnlyList<PredatorClass> playable = PredatorClassCatalog.GetPlayableClasses();
        for (int i = 0; i < playable.Count; i++)
        {
            CreateClassCard(content.transform, playable[i]);
        }
    }

    private void CreateClassCard(Transform parent, PredatorClass classId)
    {
        PredatorClassDetail detail = PredatorClassCatalog.GetDetail(classId);

        GameObject card = new GameObject("Card_" + detail.displayName);
        card.transform.SetParent(parent, false);

        Image bg = card.AddComponent<Image>();
        bg.color = cardBackground;

        Button button = card.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(() => SelectClass(classId));

        GameObject outlineObj = new GameObject("SelectedOutline");
        outlineObj.transform.SetParent(card.transform, false);
        Image outline = outlineObj.AddComponent<Image>();
        outline.color = new Color(detail.themeColor.r, detail.themeColor.g, detail.themeColor.b, 0.95f);
        outline.raycastTarget = false;
        RectTransform outlineRect = outlineObj.GetComponent<RectTransform>();
        outlineRect.anchorMin = Vector2.zero;
        outlineRect.anchorMax = Vector2.one;
        outlineRect.offsetMin = new Vector2(-3f, -3f);
        outlineRect.offsetMax = new Vector2(3f, 3f);
        outlineObj.transform.SetAsFirstSibling();
        outline.enabled = false;

        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;

        GameObject accent = new GameObject("Accent");
        accent.transform.SetParent(card.transform, false);
        Image accentImage = accent.AddComponent<Image>();
        accentImage.color = detail.themeColor;
        LayoutElement accentLayout = accent.AddComponent<LayoutElement>();
        accentLayout.preferredHeight = 6f;

        GameObject nameObj = CreateTextObject(card.transform, "Name", detail.displayName, 22f, FontStyles.Bold, GetContrastTextColor(cardBackground, detail.themeColor));
        TMP_Text nameText = nameObj.GetComponent<TMP_Text>();

        CreateTextObject(card.transform, "Role", detail.shortRole, 16f, FontStyles.Bold, defaultBodyTextColor);
        CreateTextObject(card.transform, "Tagline", detail.tagline, 14f, FontStyles.Italic, mutedBodyTextColor);

        GameObject abilityLines = CreateTextObject(card.transform, "Abilities", BuildAbilityLine(detail), 13f, FontStyles.Normal, defaultBodyTextColor);
        TMP_Text abilityText = abilityLines.GetComponent<TMP_Text>();
        abilityText.enableWordWrapping = true;
        LayoutElement abilityLayout = abilityLines.GetComponent<LayoutElement>();
        abilityLayout.preferredHeight = 36f;

        GameObject checkmark = CreateTextObject(card.transform, "Checkmark", "✓ Selected", 15f, FontStyles.Bold, GetContrastTextColor(cardSelectedBackground, detail.themeColor));
        TMP_Text checkText = checkmark.GetComponent<TMP_Text>();
        checkText.alignment = TextAlignmentOptions.Center;
        LayoutElement checkLayout = checkmark.AddComponent<LayoutElement>();
        checkLayout.preferredHeight = 22f;
        checkmark.SetActive(false);

        cardBindings.Add(new CardBinding
        {
            classId = classId,
            background = bg,
            accentBar = accentImage,
            selectedOutline = outline,
            checkmark = checkmark,
            nameText = nameText
        });
    }

    private void CreateDetailSection(Transform parent)
    {
        GameObject detailBox = new GameObject("SelectedDetail");
        detailBox.transform.SetParent(parent, false);
        Image detailBg = detailBox.AddComponent<Image>();
        detailBg.color = new Color(0.12f, 0.14f, 0.2f, 0.95f);
        LayoutElement detailLayout = detailBox.AddComponent<LayoutElement>();
        detailLayout.preferredHeight = 120f;

        VerticalLayoutGroup detailGroup = detailBox.AddComponent<VerticalLayoutGroup>();
        detailGroup.padding = new RectOffset(14, 14, 10, 10);
        detailGroup.spacing = 6f;
        detailGroup.childAlignment = TextAnchor.UpperLeft;
        detailGroup.childControlWidth = true;
        detailGroup.childControlHeight = false;
        detailGroup.childForceExpandWidth = true;

        GameObject titleObj = CreateTextObject(detailBox.transform, "DetailTitle", "Relentless Hook", 24f, FontStyles.Bold, defaultBodyTextColor);
        detailTitleText = titleObj.GetComponent<TMP_Text>();

        GameObject bodyObj = CreateTextObject(detailBox.transform, "DetailBody", string.Empty, 16f, FontStyles.Normal, mutedBodyTextColor);
        detailBodyText = bodyObj.GetComponent<TMP_Text>();
        detailBodyText.enableWordWrapping = true;
    }

    private void CreateFooterButtons(Transform parent)
    {
        GameObject footer = new GameObject("Footer");
        footer.transform.SetParent(parent, false);
        HorizontalLayoutGroup footerLayout = footer.AddComponent<HorizontalLayoutGroup>();
        footerLayout.spacing = 16f;
        footerLayout.childAlignment = TextAnchor.MiddleCenter;
        footerLayout.childControlWidth = false;
        footerLayout.childControlHeight = true;
        footerLayout.childForceExpandWidth = false;
        LayoutElement footerElement = footer.AddComponent<LayoutElement>();
        footerElement.preferredHeight = 56f;

        Button backButton = CreateFooterButton(footer.transform, "BackButton", "Back", new Color(0.35f, 0.38f, 0.45f, 1f));
        backButton.onClick.AddListener(HandleBackPressed);

        huntButton = CreateFooterButton(footer.transform, "HuntButton", "Hunt", confirmButtonColor);
        huntButton.onClick.AddListener(HandleHuntPressed);
    }

    private Button CreateFooterButton(Transform parent, string name, string label, Color color)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        Image image = buttonObj.AddComponent<Image>();
        image.color = color;
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;

        LayoutElement layout = buttonObj.AddComponent<LayoutElement>();
        layout.preferredWidth = 180f;
        layout.preferredHeight = 48f;

        GameObject textObj = CreateTextObject(buttonObj.transform, "Label", label, 22f, FontStyles.Bold, defaultBodyTextColor);
        TMP_Text text = textObj.GetComponent<TMP_Text>();
        text.alignment = TextAlignmentOptions.Center;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private void SelectClass(PredatorClass classId)
    {
        selectedClass = classId;
        PredatorClassDetail detail = PredatorClassCatalog.GetDetail(classId);

        if (detailTitleText != null)
        {
            detailTitleText.text = detail.displayName;
            detailTitleText.color = GetContrastTextColor(new Color(0.12f, 0.14f, 0.2f, 0.95f), detail.themeColor);
        }

        if (detailBodyText != null)
        {
            detailBodyText.text = detail.shortRole + " · " + detail.difficulty + "\n"
                + detail.tagline + "\n\n"
                + BuildAbilityDetailBlock(detail);
        }

        for (int i = 0; i < cardBindings.Count; i++)
        {
            CardBinding binding = cardBindings[i];
            bool selected = binding.classId == classId;
            PredatorClassDetail cardDetail = PredatorClassCatalog.GetDetail(binding.classId);

            if (binding.background != null)
            {
                binding.background.color = selected ? cardSelectedBackground : cardBackground;
            }

            if (binding.accentBar != null)
            {
                binding.accentBar.color = cardDetail.themeColor;
            }

            if (binding.selectedOutline != null)
            {
                binding.selectedOutline.enabled = selected;
                binding.selectedOutline.color = new Color(cardDetail.themeColor.r, cardDetail.themeColor.g, cardDetail.themeColor.b, 0.95f);
            }

            if (binding.checkmark != null)
            {
                binding.checkmark.SetActive(selected);
            }

            if (binding.nameText != null)
            {
                Color nameBg = selected ? cardSelectedBackground : cardBackground;
                binding.nameText.color = GetContrastTextColor(nameBg, cardDetail.themeColor);
            }
        }

        ForceRefreshLayout();
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
        if (cardGridContent == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardGridContent);
    }

    private TMP_FontAsset ResolveFont()
    {
        if (resolvedFont != null)
        {
            return resolvedFont;
        }

        if (hostUi != null && hostUi.timerText != null && hostUi.timerText.font != null)
        {
            return hostUi.timerText.font;
        }

        TMP_Text anyText = FindFirstObjectByType<TMP_Text>();
        if (anyText != null && anyText.font != null)
        {
            return anyText.font;
        }

        return TMP_Settings.defaultFontAsset;
    }

    private static Color GetContrastTextColor(Color background, Color accent)
    {
        float luminance = 0.2126f * background.r + 0.7152f * background.g + 0.0722f * background.b;
        if (luminance < 0.45f)
        {
            Color text = Color.Lerp(Color.white, accent, 0.35f);
            text.a = 1f;
            return text;
        }

        Color dark = Color.Lerp(new Color(0.12f, 0.14f, 0.18f, 1f), accent, 0.55f);
        dark.a = 1f;
        return dark;
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
            if (i > 0)
            {
                builder.Append(" · ");
            }

            builder.Append(detail.abilityNames[i]);
        }

        return builder.ToString();
    }

    private static string BuildAbilityDetailBlock(PredatorClassDetail detail)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
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

    private GameObject CreateTextObject(Transform parent, string name, string text, float fontSize, FontStyles style, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.font = ResolveFont();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.ForceMeshUpdate();

        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.minHeight = fontSize + 6f;
        layout.preferredHeight = fontSize + 8f;
        layout.flexibleWidth = 1f;

        return obj;
    }
}
