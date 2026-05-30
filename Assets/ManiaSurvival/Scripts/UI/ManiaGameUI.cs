using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using TMPro;

public class ManiaGameUI : MonoBehaviour
{
    [Header("Text")]
    public TMP_Text timerText;
    public TMP_Text roleText;
    public TMP_Text deathFeedText;
    public TMP_Text survivorCountText;
    public TMP_Text monsterKillsText;
    public TMP_Text gameOverTitleText;
    public TMP_Text playerManaText;

    [Header("Panels")]
    public GameObject startScreen;
    public GameObject gameplayHUD;
    public GameObject gameOverPanel;
    public GameObject sharedHud;
    public GameObject sharedControls;
    public GameObject survivorPanel;
    public GameObject monsterPanel;
    public GameObject avatarPanel;
    public GameObject enterSoulwoodButton;

    [Header("Buttons")]
    public Button startButton;
    public Button restartButton;

    [Header("Survivor Class Buttons")]
    public Button survivorPrimaryButton;
    public Button survivorAbility2Button;
    public Button survivorAbility3Button;
    public Button survivorUltimateButton;

    [Header("Predator Class Buttons")]
    public Button predatorMeleeButton;
    public Button predatorAbility1Button;
    public Button predatorAbility2Button;
    public Button predatorAbility3Button;
    public Button predatorUltimateButton;

    [Header("Ability Labels")]
    public TMP_Text survivorAbility1Label;
    public TMP_Text survivorAbility2Label;
    public TMP_Text survivorAbility3Label;
    public TMP_Text survivorUltimateLabel;
    public TMP_Text predatorAbility1Label;
    public TMP_Text predatorAbility2Label;
    public TMP_Text predatorAbility3Label;
    public TMP_Text predatorUltimateLabel;
    public TMP_Text abilityInfoText;

    [Header("Ability Tooltip")]
    public AbilityTooltipPanel abilityTooltipPanel;
    public bool logTooltipEvents;

    [Header("Role-Specific Visibility")]
    [Tooltip("Any GameObject dragged here is only visible while playing as Survivor (hidden in Monster mode and Avatar mode).")]
    public GameObject[] survivorOnlyObjects;
    [Tooltip("Any GameObject dragged here is only visible while playing as Monster.")]
    public GameObject[] monsterOnlyObjects;

    [Header("Mobile Joystick")]
    public bool enableSimpleMobileJoystick = true;
    public RectTransform joystickArea;
    public RectTransform joystickKnob;
    public Camera uiCamera;
    public float joystickRadius = 80f;
    public float joystickMarginLeft = 24f;
    public float joystickMarginBottom = 24f;
    public Vector2 joystickAreaSize = new Vector2(200f, 200f);

    [Header("Survivor Utility Buttons")]
    public Button survivorSprintButton;
    public Button survivorPickupButton;
    public Button survivorDropButton;

    [Header("Mobile Ability Layout")]
    [Tooltip("Distance from the right screen edge to the ability grid.")]
    public float abilityMarginRight = 24f;
    [Tooltip("Distance from the bottom screen edge to the ability grid.")]
    public float abilityMarginBottom = 24f;
    [Tooltip("Width and height of each ability button cell.")]
    public Vector2 abilityButtonSize = new Vector2(132f, 88f);
    [Tooltip("Gap between ability buttons in the 2x2 grid.")]
    public float abilityButtonSpacing = 12f;
    [Tooltip("Extra safe-area padding applied on top of layout margins.")]
    public float safeAreaPadding = 8f;
    [Tooltip("Gap between the ability grid and the tooltip panel.")]
    public float tooltipGapAboveAbilities = 96f;
    [Tooltip("Tooltip panel size for hold-to-read ability info.")]
    public Vector2 tooltipPanelSize = new Vector2(250f, 100f);
    [Tooltip("Utility button size for Sprint / Pick Up / Drop (Survivor only).")]
    public Vector2 survivorUtilityButtonSize = new Vector2(120f, 64f);
    [Tooltip("Vertical gap between survivor utility buttons.")]
    public float survivorUtilitySpacing = 8f;
    [Tooltip("Gap above the joystick reserved for survivor utility buttons.")]
    public float survivorUtilityGapAboveJoystick = 12f;

    [Header("Labels")]
    public string playingRoleText = "Survive until the timer ends";
    public string monsterWinText = "Monster Wins";
    public string survivorWinText = "Survivors Win";

    [Header("References")]
    public ManiaGameManager gameManager;
    public LocalRoleController localRoleController;
    public SurvivorMovement localSurvivorMovement;
    public AbilityController survivorAbilityController;
    public AbilityController predatorAbilityController;
    public SoulwoodAvatarUIBridge soulwoodAvatarUIBridge;

    private int activeTouchId = -1;
    private bool mouseJoystickActive;
    private PlayerControlMode lastAbilityLabelMode = (PlayerControlMode)(-1);
    private PlayerControlMode lastAppliedLayoutMode = (PlayerControlMode)(-1);
    private bool lastAppliedLayoutPlaying;
    private bool loggedMissingAbilityInfoText;
    private bool loggedMissingAbilityLabels;

    private void Awake()
    {
        if (avatarPanel != null)
        {
            avatarPanel.SetActive(false);
        }

        if (enterSoulwoodButton != null)
        {
            enterSoulwoodButton.SetActive(false);
        }
    }

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<ManiaGameManager>();
        }

        if (localSurvivorMovement == null)
        {
            localSurvivorMovement = FindFirstObjectByType<SurvivorMovement>();
        }

        if (localRoleController == null)
        {
            localRoleController = FindFirstObjectByType<LocalRoleController>();
        }

        if (survivorAbilityController == null && localRoleController != null && localRoleController.survivorMovement != null)
        {
            survivorAbilityController = localRoleController.survivorMovement.GetComponent<AbilityController>();
        }

        if (predatorAbilityController == null && localRoleController != null && localRoleController.monsterMovement != null)
        {
            predatorAbilityController = localRoleController.monsterMovement.GetComponent<AbilityController>();
        }

        if (soulwoodAvatarUIBridge == null)
        {
            soulwoodAvatarUIBridge = FindFirstObjectByType<SoulwoodAvatarUIBridge>();
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(HandleStartPressed);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(HandleStartPressed);
        }

        SetupAbilityHoldButtons();
        ResolveSurvivorUtilityButtons();
        ConfigureMobileAbilityLayout();
        RefreshPredatorAbilityButtonBindings();

        RefreshAbilityLabels(force: true);
        RefreshAbilityInfo(force: true);

        if (gameManager != null)
        {
            Refresh(gameManager);
        }
    }

    private void Update()
    {
        if (enableSimpleMobileJoystick)
        {
            UpdateSimpleMobileJoystick();
        }

        UpdateRolePanels();
        RefreshAbilityLabels();
        RefreshAbilityInfo();
        RefreshPlayerManaDisplay();
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HandleStartPressed);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(HandleStartPressed);
        }
    }

    private void SetupAbilityHoldButtons()
    {
        EnsureAbilityTooltipPanel();

        SetupAbilityHoldButton(survivorPrimaryButton, 1, false);
        SetupAbilityHoldButton(survivorAbility2Button, 2, false);
        SetupAbilityHoldButton(survivorAbility3Button, 3, false);
        SetupAbilityHoldButton(survivorUltimateButton, 4, false);
        SetupAbilityHoldButton(predatorMeleeButton, 1, true);
        // predatorAbility1Button duplicates slot 1 (Spray) — hidden in UpdateRolePanels.
        SetupAbilityHoldButton(predatorAbility2Button, 2, true);
        SetupAbilityHoldButton(predatorAbility3Button, 3, true);
        SetupAbilityHoldButton(predatorUltimateButton, 4, true);
    }

    private void ConfigureMobileAbilityLayout()
    {
        GetLayoutInsets(out float insetLeft, out float insetRight, out float insetBottom, out float insetTop);

        ConfigureJoystickLayout(insetLeft, insetBottom);
        ConfigureRoleHudPanels(insetRight, insetBottom);
        ConfigureSurvivorAbilityLayout(insetRight, insetBottom);
        ConfigurePredatorAbilityLayout(insetRight, insetBottom);
        ConfigureTooltipLayout(insetRight, insetBottom);
    }

    private void ConfigurePredatorAbilityLayout(float insetRight, float insetBottom)
    {
        HideDuplicatePredatorSprayButton();

        SetSiblingOrder(
            predatorMeleeButton,
            predatorAbility2Button,
            predatorAbility3Button,
            predatorUltimateButton);

        if (predatorAbility1Button != null)
        {
            predatorAbility1Button.transform.SetAsLastSibling();
        }

        RectTransform gridRect = GetAbilityGridRoot(predatorMeleeButton, predatorUltimateButton);
        ApplyAbilityGridLayout(gridRect, insetRight, insetBottom);
    }

    private void ConfigureSurvivorAbilityLayout(float insetRight, float insetBottom)
    {
        HideSurvivorAbilityGridExtras();

        SetSiblingOrder(
            survivorPrimaryButton,
            survivorAbility2Button,
            survivorAbility3Button,
            survivorUltimateButton);

        RectTransform gridRect = GetAbilityGridRoot(survivorPrimaryButton, survivorUltimateButton);
        ApplyAbilityGridLayout(gridRect, insetRight, insetBottom);
        RefreshActionButtonLayout(insetLeft: GetLayoutInsetsLeft(), insetBottom);
    }

    public void RefreshActionButtonLayout()
    {
        GetLayoutInsets(out float insetLeft, out _, out float insetBottom, out _);
        RefreshActionButtonLayout(insetLeft, insetBottom);
    }

    private void RefreshActionButtonLayout(float insetLeft, float insetBottom)
    {
        ResolveSurvivorUtilityButtons();

        RectTransform gridRect = GetAbilityGridRoot(survivorPrimaryButton, survivorUltimateButton);
        Transform utilityParent = gridRect != null ? gridRect.parent : survivorPanel != null ? survivorPanel.transform : transform;

        RelocateUtilityButton(survivorSprintButton, utilityParent, insetLeft, insetBottom, 0);
        RelocateUtilityButton(survivorPickupButton, utilityParent, insetLeft, insetBottom, 1);
        RelocateUtilityButton(survivorDropButton, utilityParent, insetLeft, insetBottom, 1);
        ConfigureSurvivorUtilityPanel(insetLeft, insetBottom);
    }

    private void ResolveSurvivorUtilityButtons()
    {
        ItemActionButtonUI itemUi = GetComponent<ItemActionButtonUI>();
        if (itemUi != null)
        {
            if (survivorPickupButton == null)
            {
                survivorPickupButton = itemUi.pickupButton;
            }

            if (survivorDropButton == null)
            {
                survivorDropButton = itemUi.dropButton;
            }
        }

        if (survivorSprintButton == null)
        {
            survivorSprintButton = FindSurvivorUtilityButton("Sprint");
        }
    }

    private Button FindSurvivorUtilityButton(string objectName)
    {
        RectTransform gridRect = GetAbilityGridRoot(survivorPrimaryButton, survivorUltimateButton);
        if (gridRect == null)
        {
            return null;
        }

        Button[] buttons = gridRect.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && button.gameObject.name == objectName)
            {
                return button;
            }
        }

        return null;
    }

    private void RelocateUtilityButton(Button button, Transform parent, float insetLeft, float insetBottom, int slotIndex)
    {
        if (button == null)
        {
            return;
        }

        RectTransform utilityRect = button.transform as RectTransform;
        if (utilityRect == null)
        {
            return;
        }

        if (parent != null && utilityRect.parent != parent)
        {
            utilityRect.SetParent(parent, false);
        }

        SetIgnoreLayout(button.gameObject, true);

        float yOffset = insetBottom
            + joystickMarginBottom
            + joystickAreaSize.y
            + survivorUtilityGapAboveJoystick
            + (survivorUtilityButtonSize.y + survivorUtilitySpacing) * slotIndex;

        ApplyBottomLeftAnchor(
            utilityRect,
            survivorUtilityButtonSize,
            insetLeft + joystickMarginLeft,
            yOffset);
    }

    private void ConfigureSurvivorUtilityPanel(float insetLeft, float insetBottom)
    {
        if (survivorPanel == null)
        {
            return;
        }

        Transform utilityRoot = survivorPanel.transform.Find("SurvivorUtilityControls");
        if (utilityRoot is not RectTransform utilityRect)
        {
            return;
        }

        float yOffset = insetBottom
            + joystickMarginBottom
            + joystickAreaSize.y
            + survivorUtilityGapAboveJoystick
            + (survivorUtilityButtonSize.y + survivorUtilitySpacing) * 2f
            + 8f;

        ApplyBottomLeftAnchor(
            utilityRect,
            survivorUtilityButtonSize,
            insetLeft + joystickMarginLeft,
            yOffset);
    }

    private void ConfigureRoleHudPanels(float insetRight, float insetBottom)
    {
        ApplyBottomRightContainer(survivorPanel != null ? survivorPanel.transform as RectTransform : null, insetRight, insetBottom);
        ApplyBottomRightContainer(monsterPanel != null ? monsterPanel.transform as RectTransform : null, insetRight, insetBottom);
    }

    private void ConfigureJoystickLayout(float insetLeft, float insetBottom)
    {
        if (joystickArea == null)
        {
            return;
        }

        ApplyBottomLeftAnchor(joystickArea, joystickAreaSize, insetLeft, insetBottom);
    }

    private void ConfigureTooltipLayout(float insetRight, float insetBottom)
    {
        EnsureAbilityTooltipPanel();

        if (abilityTooltipPanel == null)
        {
            return;
        }

        RectTransform activeGrid = GetActiveAbilityGridRect();
        abilityTooltipPanel.ConfigureMobileLayout(
            activeGrid,
            tooltipPanelSize,
            abilityMarginRight + insetRight,
            abilityMarginBottom + insetBottom,
            tooltipGapAboveAbilities);
    }

    private RectTransform GetActiveAbilityGridRect()
    {
        bool isMonsterControlled = localRoleController != null
            && localRoleController.controlMode == PlayerControlMode.MonsterControlled;

        if (isMonsterControlled)
        {
            return GetAbilityGridRoot(predatorMeleeButton, predatorUltimateButton);
        }

        return GetAbilityGridRoot(survivorPrimaryButton, survivorUltimateButton);
    }

    private void ApplyAbilityGridLayout(RectTransform gridRect, float insetRight, float insetBottom)
    {
        if (gridRect == null)
        {
            return;
        }

        GridLayoutGroup gridLayout = gridRect.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.LowerRight;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;
            gridLayout.cellSize = abilityButtonSize;
            gridLayout.spacing = new Vector2(abilityButtonSpacing, abilityButtonSpacing);
            gridLayout.padding = new RectOffset(0, 0, 0, 0);
        }

        Vector2 gridSize = ComputeGridSize(2, 2, abilityButtonSize, abilityButtonSpacing);
        if (IsRoleHudChild(gridRect))
        {
            StretchRectToParent(gridRect);
        }
        else
        {
            ApplyBottomRightAnchor(
                gridRect,
                gridSize,
                abilityMarginRight + insetRight,
                abilityMarginBottom + insetBottom);
        }
    }

    private bool IsRoleHudChild(RectTransform gridRect)
    {
        if (gridRect == null || gridRect.parent == null)
        {
            return false;
        }

        GameObject parentObject = gridRect.parent.gameObject;
        return parentObject == survivorPanel || parentObject == monsterPanel;
    }

    private static void StretchRectToParent(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void HideSurvivorAbilityGridExtras()
    {
        RectTransform gridRect = GetAbilityGridRoot(survivorPrimaryButton, survivorUltimateButton);
        if (gridRect == null)
        {
            return;
        }

        Button[] buttons = gridRect.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            if (IsSurvivorAbilityButton(button))
            {
                continue;
            }

            SetIgnoreLayout(button.gameObject, true);
        }
    }

    private bool IsSurvivorAbilityButton(Button button)
    {
        return button == survivorPrimaryButton
            || button == survivorAbility2Button
            || button == survivorAbility3Button
            || button == survivorUltimateButton;
    }

    private void RelocateSurvivorUtilityButtons(float insetLeft, float insetBottom)
    {
        RefreshActionButtonLayout(insetLeft, insetBottom);
    }

    private void ApplyBottomRightContainer(RectTransform container, float insetRight, float insetBottom)
    {
        if (container == null)
        {
            return;
        }

        container.anchorMin = new Vector2(1f, 0f);
        container.anchorMax = new Vector2(1f, 0f);
        container.pivot = new Vector2(1f, 0f);
        container.anchoredPosition = new Vector2(-(insetRight + safeAreaPadding), insetBottom + safeAreaPadding);
        container.sizeDelta = ComputeGridSize(2, 2, abilityButtonSize, abilityButtonSpacing);
    }

    private void ApplyBottomRightAnchor(RectTransform rect, Vector2 size, float marginRight, float marginBottom)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(-marginRight, marginBottom);
    }

    private void ApplyBottomLeftAnchor(RectTransform rect, Vector2 size, float marginLeft, float marginBottom)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(marginLeft, marginBottom);
    }

    private static Vector2 ComputeGridSize(int columns, int rows, Vector2 cellSize, float spacing)
    {
        float width = columns * cellSize.x + (columns - 1) * spacing;
        float height = rows * cellSize.y + (rows - 1) * spacing;
        return new Vector2(width, height);
    }

    private static RectTransform GetAbilityGridRoot(Button firstButton, Button lastButton)
    {
        Transform gridRoot = firstButton != null
            ? firstButton.transform.parent
            : lastButton != null ? lastButton.transform.parent : null;

        return gridRoot as RectTransform;
    }

    private static void SetSiblingOrder(params Button[] buttons)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].transform.SetSiblingIndex(i);
            }
        }
    }

    private static void SetIgnoreLayout(GameObject target, bool ignoreLayout)
    {
        if (target == null)
        {
            return;
        }

        LayoutElement layoutElement = target.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = target.AddComponent<LayoutElement>();
        }

        layoutElement.ignoreLayout = ignoreLayout;
    }

    private void GetLayoutInsets(out float insetLeft, out float insetRight, out float insetBottom, out float insetTop)
    {
        insetLeft = safeAreaPadding;
        insetRight = safeAreaPadding;
        insetBottom = safeAreaPadding;
        insetTop = safeAreaPadding;

        Rect safeArea = Screen.safeArea;
        if (safeArea.width <= 0f || safeArea.height <= 0f)
        {
            return;
        }

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            return;
        }

        float scale = rootCanvas.scaleFactor <= 0f ? 1f : rootCanvas.scaleFactor;
        insetLeft += safeArea.x / scale;
        insetRight += (Screen.width - safeArea.xMax) / scale;
        insetBottom += safeArea.y / scale;
        insetTop += (Screen.height - safeArea.yMax) / scale;
    }

    private float GetLayoutInsetsLeft()
    {
        GetLayoutInsets(out float insetLeft, out _, out _, out _);
        return insetLeft;
    }

    private void HideDuplicatePredatorSprayButton()
    {
        if (predatorAbility1Button == null)
        {
            return;
        }

        predatorAbility1Button.gameObject.SetActive(false);
        LayoutElement hiddenLayout = predatorAbility1Button.GetComponent<LayoutElement>();
        if (hiddenLayout == null)
        {
            hiddenLayout = predatorAbility1Button.gameObject.AddComponent<LayoutElement>();
        }

        hiddenLayout.ignoreLayout = true;
    }

    private void RefreshPredatorAbilityButtonBindings()
    {
        BindPredatorAbilityButton(predatorMeleeButton, 1, "Spray");
        BindPredatorAbilityButton(predatorAbility2Button, 2, "Hook");
        BindPredatorAbilityButton(predatorAbility3Button, 3, "Tonic");
        BindPredatorAbilityButton(predatorUltimateButton, 4, "Barrage");
    }

    private void BindPredatorAbilityButton(Button button, int slotNumber, string label)
    {
        if (button == null)
        {
            return;
        }

        AbilityButtonHoldTooltip holdTooltip = button.GetComponent<AbilityButtonHoldTooltip>();
        if (holdTooltip != null)
        {
            holdTooltip.Configure(this, abilityTooltipPanel, slotNumber, true, logTooltipEvents);
        }

        AbilityCooldownButton cooldownButton = button.GetComponent<AbilityCooldownButton>();
        if (cooldownButton != null)
        {
            cooldownButton.BindCooldownVisual(this, slotNumber, true);
        }

        SetButtonLabel(button, null, label);
    }

    private void EnsureAbilityTooltipPanel()
    {
        if (abilityTooltipPanel == null)
        {
            abilityTooltipPanel = GetComponent<AbilityTooltipPanel>();
        }

        if (abilityTooltipPanel == null)
        {
            abilityTooltipPanel = gameObject.AddComponent<AbilityTooltipPanel>();
        }

        abilityTooltipPanel.gameUi = this;
    }

    private void SetupAbilityHoldButton(Button button, int slotNumber, bool isPredatorButton)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();

        AbilityButtonHoldTooltip holdTooltip = button.GetComponent<AbilityButtonHoldTooltip>();
        if (holdTooltip == null)
        {
            holdTooltip = button.gameObject.AddComponent<AbilityButtonHoldTooltip>();
        }

        holdTooltip.Configure(this, abilityTooltipPanel, slotNumber, isPredatorButton, logTooltipEvents);

        AbilityCooldownButton cooldownButton = button.GetComponent<AbilityCooldownButton>();
        if (cooldownButton != null)
        {
            cooldownButton.BindCooldownVisual(this, slotNumber, isPredatorButton);
        }
    }

    public void CastSurvivorSlot(int slotNumber)
    {
        UseSurvivorSlot(slotNumber);
    }

    public void CastPredatorSlot(int slotNumber)
    {
        UsePredatorSlot(slotNumber);
    }

    public void Refresh(ManiaGameManager manager)
    {
        if (manager == null)
        {
            return;
        }

        if (timerText != null)
        {
            timerText.text = FormatTime(manager.TimeRemaining);
        }

        if (roleText != null)
        {
            roleText.text = manager.State == ManiaGameState.Playing ? GetPlayingRoleLabel() : "";
        }

        if (survivorCountText != null)
        {
            survivorCountText.text = "Survivors: " + manager.AliveSurvivorCount;
        }

        if (monsterKillsText != null)
        {
            monsterKillsText.text = "Kills: " + manager.MonsterKills;
        }

        if (gameOverTitleText != null)
        {
            if (manager.State == ManiaGameState.MonsterWon)
            {
                gameOverTitleText.text = monsterWinText;
            }
            else if (manager.State == ManiaGameState.SurvivorsWon)
            {
                gameOverTitleText.text = survivorWinText;
            }
        }

        bool isWaiting = manager.State == ManiaGameState.WaitingToStart;
        bool isPlaying = manager.State == ManiaGameState.Playing;
        bool isGameOver = manager.State == ManiaGameState.MonsterWon || manager.State == ManiaGameState.SurvivorsWon;

        if (startScreen != null)
        {
            startScreen.SetActive(isWaiting);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(isGameOver);
        }

        UpdateRolePanels();
    }

    private void UpdateRolePanels()
    {
        bool isPlaying = gameManager != null && gameManager.State == ManiaGameState.Playing;
        bool isMonsterControlled = localRoleController != null && localRoleController.controlMode == PlayerControlMode.MonsterControlled;
        bool isAvatarControlled = soulwoodAvatarUIBridge != null && soulwoodAvatarUIBridge.HasActiveAvatar;
        bool shouldShowEnterButton = isPlaying
            && !isMonsterControlled
            && soulwoodAvatarUIBridge != null
            && soulwoodAvatarUIBridge.HasNearbyAvatar
            && !isAvatarControlled;

        SetPanelActive(sharedHud, isPlaying, gameplayHUD);
        SetPanelActive(sharedControls, isPlaying);
        SetPanelActive(avatarPanel, isPlaying && isAvatarControlled);
        SetPanelActive(enterSoulwoodButton, shouldShowEnterButton);

        if (survivorPanel != null)
        {
            survivorPanel.SetActive(isPlaying && !isMonsterControlled && !isAvatarControlled);
        }

        if (monsterPanel != null)
        {
            monsterPanel.SetActive(isPlaying && isMonsterControlled);
        }

        if (survivorPrimaryButton != null)
        {
            survivorPrimaryButton.gameObject.SetActive(isPlaying && !isMonsterControlled && !isAvatarControlled);
        }

        if (survivorAbility2Button != null)
        {
            survivorAbility2Button.gameObject.SetActive(isPlaying && !isMonsterControlled && !isAvatarControlled);
        }

        if (survivorAbility3Button != null)
        {
            survivorAbility3Button.gameObject.SetActive(isPlaying && !isMonsterControlled && !isAvatarControlled);
        }

        if (survivorUltimateButton != null)
        {
            survivorUltimateButton.gameObject.SetActive(isPlaying && !isMonsterControlled && !isAvatarControlled);
        }

        if (predatorMeleeButton != null)
        {
            predatorMeleeButton.gameObject.SetActive(isPlaying && isMonsterControlled);
        }

        if (predatorAbility1Button != null)
        {
            HideDuplicatePredatorSprayButton();
        }

        if (predatorAbility2Button != null)
        {
            predatorAbility2Button.gameObject.SetActive(isPlaying && isMonsterControlled);
        }

        if (predatorAbility3Button != null)
        {
            predatorAbility3Button.gameObject.SetActive(isPlaying && isMonsterControlled);
        }

        if (predatorUltimateButton != null)
        {
            predatorUltimateButton.gameObject.SetActive(isPlaying && isMonsterControlled);
        }

        bool showSurvivorOnly = isPlaying && !isMonsterControlled && !isAvatarControlled;
        bool showMonsterOnly = isPlaying && isMonsterControlled;

        SetGameObjectsActive(survivorOnlyObjects, showSurvivorOnly);
        SetGameObjectsActive(monsterOnlyObjects, showMonsterOnly);

        RefreshMobileLayoutIfNeeded(isPlaying);
    }

    private void RefreshMobileLayoutIfNeeded(bool isPlaying)
    {
        PlayerControlMode currentMode = localRoleController != null
            ? localRoleController.controlMode
            : PlayerControlMode.SurvivorControlled;

        if (!isPlaying)
        {
            lastAppliedLayoutPlaying = false;
            return;
        }

        if (lastAppliedLayoutPlaying && currentMode == lastAppliedLayoutMode)
        {
            return;
        }

        lastAppliedLayoutPlaying = true;
        lastAppliedLayoutMode = currentMode;
        ConfigureMobileAbilityLayout();
    }

    private void SetGameObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject target = objects[i];
            if (target == null)
            {
                continue;
            }

            if (target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }

    private void SetPanelActive(GameObject primary, bool active, GameObject fallback = null)
    {
        if (primary != null)
        {
            primary.SetActive(active);
            return;
        }

        if (fallback != null)
        {
            fallback.SetActive(active);
        }
    }

    public void ShowSurvivorPanel()
    {
        Debug.Log("Showing Survivor Panel - survivorPanel assigned: " + (survivorPanel != null) + ", avatarPanel assigned: " + (avatarPanel != null));
        if (survivorPanel != null)
        {
            survivorPanel.SetActive(true);
        }

        if (monsterPanel != null)
        {
            monsterPanel.SetActive(false);
        }

        if (avatarPanel != null)
        {
            avatarPanel.SetActive(false);
        }

        ConfigureMobileAbilityLayout();
        RefreshAbilityLabels(force: true);
    }

    public void ShowMonsterPanel()
    {
        Debug.Log("Showing Monster Panel - survivorPanel assigned: " + (survivorPanel != null) + ", avatarPanel assigned: " + (avatarPanel != null));
        if (survivorPanel != null)
        {
            survivorPanel.SetActive(false);
        }

        if (monsterPanel != null)
        {
            monsterPanel.SetActive(true);
        }

        if (avatarPanel != null)
        {
            avatarPanel.SetActive(false);
        }

        ConfigureMobileAbilityLayout();
        RefreshPredatorAbilityButtonBindings();
        RefreshAbilityLabels(force: true);

        HideEnterSoulwoodButton();
    }

    public void ShowAvatarPanel()
    {
        Debug.Log("Showing Avatar Panel - survivorPanel assigned: " + (survivorPanel != null) + ", avatarPanel assigned: " + (avatarPanel != null));
        if (survivorPanel != null)
        {
            survivorPanel.SetActive(false);
        }

        if (monsterPanel != null)
        {
            monsterPanel.SetActive(false);
        }

        if (avatarPanel != null)
        {
            avatarPanel.SetActive(true);
        }

        HideEnterSoulwoodButton();
    }

    public void HideAvatarPanel()
    {
        if (avatarPanel != null)
        {
            avatarPanel.SetActive(false);
        }
    }

    public void ShowEnterSoulwoodButton()
    {
        Debug.Log("Showing Enter Soulwood button, assigned: " + (enterSoulwoodButton != null));
        if (enterSoulwoodButton != null)
        {
            enterSoulwoodButton.SetActive(true);
        }
    }

    public void HideEnterSoulwoodButton()
    {
        Debug.Log("Hiding Enter Soulwood button");
        if (enterSoulwoodButton != null)
        {
            enterSoulwoodButton.SetActive(false);
        }
    }

    public void SetMobileMoveInput(Vector2 input)
    {
        if (localRoleController != null)
        {
            localRoleController.SetMoveInput(input);
            return;
        }

        if (localSurvivorMovement != null)
        {
            localSurvivorMovement.SetMoveInput(input);
        }
    }

    public void SetMobileSprintHeld(bool isHeld)
    {
        if (localRoleController != null)
        {
            localRoleController.SetSprintHeld(isHeld);
            return;
        }

        if (localSurvivorMovement != null)
        {
            localSurvivorMovement.SetSprintHeld(isHeld);
        }
    }

    public void PressMobileDodge()
    {
        if (localRoleController != null)
        {
            localRoleController.PressDodge();
            return;
        }

        if (localSurvivorMovement != null)
        {
            localSurvivorMovement.TryDodge();
        }
    }

    private void UpdateSimpleMobileJoystick()
    {
        if (joystickArea == null || (localRoleController == null && localSurvivorMovement == null))
        {
            return;
        }

        if (HasPressedTouch() || activeTouchId != -1)
        {
            UpdateTouchJoystick();
            return;
        }

        UpdateMouseJoystickForEditor();
    }

    private void UpdateTouchJoystick()
    {
        Touchscreen touchscreen = Touchscreen.current;

        if (touchscreen == null)
        {
            return;
        }

        bool foundActiveTouch = false;

        for (int i = 0; i < touchscreen.touches.Count; i++)
        {
            TouchControl touch = touchscreen.touches[i];
            int touchId = touch.touchId.ReadValue();
            Vector2 touchPosition = touch.position.ReadValue();

            if (activeTouchId == -1 && touch.press.wasPressedThisFrame && IsInsideJoystick(touchPosition))
            {
                activeTouchId = touchId;
            }

            if (touchId != activeTouchId)
            {
                continue;
            }

            foundActiveTouch = true;

            if (!touch.press.isPressed || touch.press.wasReleasedThisFrame)
            {
                activeTouchId = -1;
                ApplyJoystickInput(Vector2.zero);
                return;
            }

            ApplyJoystickInput(GetJoystickInput(touchPosition));
            return;
        }

        if (!foundActiveTouch)
        {
            activeTouchId = -1;
            ApplyJoystickInput(Vector2.zero);
        }
    }

    private void UpdateMouseJoystickForEditor()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            return;
        }

        Vector2 mousePosition = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame && IsInsideJoystick(mousePosition))
        {
            mouseJoystickActive = true;
        }

        if (!mouseJoystickActive)
        {
            return;
        }

        if (mouse.leftButton.isPressed)
        {
            ApplyJoystickInput(GetJoystickInput(mousePosition));
        }
        else
        {
            mouseJoystickActive = false;
            ApplyJoystickInput(Vector2.zero);
        }
    }

    private bool HasPressedTouch()
    {
        Touchscreen touchscreen = Touchscreen.current;

        if (touchscreen == null)
        {
            return false;
        }

        for (int i = 0; i < touchscreen.touches.Count; i++)
        {
            TouchControl touch = touchscreen.touches[i];

            if (touch.press.isPressed || touch.press.wasPressedThisFrame || touch.press.wasReleasedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsInsideJoystick(Vector2 screenPosition)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(joystickArea, screenPosition, uiCamera);
    }

    private Vector2 GetJoystickInput(Vector2 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickArea, screenPosition, uiCamera, out Vector2 localPoint);

        if (joystickRadius <= 0f)
        {
            return Vector2.zero;
        }

        return Vector2.ClampMagnitude(localPoint / joystickRadius, 1f);
    }

    private void ApplyJoystickInput(Vector2 input)
    {
        SetMobileMoveInput(input);

        if (joystickKnob != null)
        {
            joystickKnob.anchoredPosition = input * joystickRadius;
        }
    }

    private void HandleStartPressed()
    {
        if (localRoleController != null)
        {
            StartGameWithMode(localRoleController.controlMode);
            return;
        }

        StartAsSurvivor();
    }

    public void StartAsSurvivor()
    {
        StartGameWithMode(PlayerControlMode.SurvivorControlled);
    }

    public void StartAsMonster()
    {
        StartGameWithMode(PlayerControlMode.MonsterControlled);
    }

    public void OnSurvivorPrimaryPressed()
    {
        Debug.Log("[ManiaGameUI] Survivor button pressed: slot 1");
        UseSurvivorSlot(1);
    }

    public void OnSurvivorAbility2Pressed()
    {
        Debug.Log("[ManiaGameUI] Survivor button pressed: slot 2");
        UseSurvivorSlot(2);
    }

    public void OnSurvivorAbility3Pressed()
    {
        Debug.Log("[ManiaGameUI] Survivor button pressed: slot 3");
        UseSurvivorSlot(3);
    }

    public void OnSurvivorUltimatePressed()
    {
        Debug.Log("[ManiaGameUI] Survivor button pressed: slot 4");
        UseSurvivorSlot(4);
    }

    public void OnPredatorMeleePressed()
    {
        Debug.Log("[ManiaGameUI] OnPredatorMeleePressed -> slot 1");
        UsePredatorSlot(1);
    }

    public void OnPredatorAbility1Pressed()
    {
        Debug.Log("[ManiaGameUI] OnPredatorAbility1Pressed -> slot 1");
        UsePredatorSlot(1);
    }

    public void OnPredatorAbility2Pressed()
    {
        Debug.Log("[ManiaGameUI] OnPredatorAbility2Pressed -> slot 2");
        UsePredatorSlot(2);
    }

    public void OnPredatorAbility3Pressed()
    {
        Debug.Log("[ManiaGameUI] OnPredatorAbility3Pressed -> slot 3");
        UsePredatorSlot(3);
    }

    public void OnPredatorUltimatePressed()
    {
        Debug.Log("[ManiaGameUI] OnPredatorUltimatePressed -> slot 4");
        UsePredatorSlot(4);
    }

    private void UseSurvivorSlot(int slotNumber)
    {
        if (localRoleController != null)
        {
            localRoleController.UseSurvivorAbilitySlot(slotNumber);
            return;
        }

        Debug.LogWarning("[ManiaGameUI] Missing localRoleController/AbilityController reference on '" + gameObject.name + "' for Survivor slot " + slotNumber + ".");
    }

    private void UsePredatorSlot(int slotNumber)
    {
        if (localRoleController != null)
        {
            localRoleController.UsePredatorAbilitySlot(slotNumber);
            return;
        }

        Debug.LogWarning("[ManiaGameUI] Missing localRoleController/AbilityController reference on '" + gameObject.name + "' for Predator slot " + slotNumber + ".");
    }

    private void StartGameWithMode(PlayerControlMode mode)
    {
        lastAbilityLabelMode = (PlayerControlMode)(-1);

        if (localRoleController != null)
        {
            localRoleController.SetControlMode(mode);
        }

        if (soulwoodAvatarUIBridge != null)
        {
            soulwoodAvatarUIBridge.ResetActiveAvatarState();
        }

        HideEnterSoulwoodButton();

        if (gameManager != null)
        {
            if (gameManager.State != ManiaGameState.WaitingToStart)
            {
                gameManager.ReturnToWaitingScreen();
            }

            if (!gameManager.TryRegisterLocalPlayerReady())
            {
                Debug.Log("Waiting for other players to click Start...");
                return;
            }
        }

        if (mode == PlayerControlMode.SurvivorControlled)
        {
            Debug.Log("UI Start Survivor Mode");
            ShowSurvivorPanel();
        }
        else
        {
            Debug.Log("UI Start Monster Mode");
            ShowMonsterPanel();
        }

        RefreshAbilityLabels(force: true);
        RefreshAbilityInfo(force: true);
    }

    public void RefreshAbilityLabels(bool force = false)
    {
        PlayerControlMode currentMode = localRoleController != null
            ? localRoleController.controlMode
            : PlayerControlMode.SurvivorControlled;

        if (!force && currentMode == lastAbilityLabelMode)
        {
            return;
        }

        lastAbilityLabelMode = currentMode;
        bool isMonsterControlled = currentMode == PlayerControlMode.MonsterControlled;

        if (isMonsterControlled)
        {
            ApplyPredatorAbilityLabels();
            return;
        }

        ApplySurvivorAbilityLabels();
    }

    public void RefreshAbilityInfo(bool force = false)
    {
        if (abilityInfoText == null)
        {
            if (!loggedMissingAbilityInfoText)
            {
                loggedMissingAbilityInfoText = true;
                Debug.Log("[ManiaGameUI] abilityInfoText is not assigned. Info panel will stay hidden until wired.");
            }

            return;
        }

        PlayerControlMode currentMode = localRoleController != null
            ? localRoleController.controlMode
            : PlayerControlMode.SurvivorControlled;

        if (currentMode == PlayerControlMode.MonsterControlled)
        {
            abilityInfoText.text =
                "Relentless Hook\n"
                + "1. Spray — 12 dmg cone, knockback 2.6 (8 mana, 2s cd).\n"
                + "2. Hook — Pull + 10 dmg (25 mana, 9s cd).\n"
                + "3. Tonic — Heal 35, self-slow, toxic gas (35 mana).\n"
                + "4. Barrage — Ultimate cone knockback (60 mana, 16s cd).";
            return;
        }

        abilityInfoText.text =
            "Field Medic\n"
            + "1. Biotic Dart — Aim heal ally or hurt monster (2 mana, 2.5s cd).\n"
            + "2. Heal Pulse — Heal self + nearby allies (5 mana, 8s cd).\n"
            + "3. Tether — Dash to ally or blink forward (4 mana, 10s cd).\n"
            + "4. Sanctuary — Strong heal zone 7s (12 mana, 28s cd).\n";
    }

    private void RefreshPlayerManaDisplay()
    {
        if (playerManaText == null)
        {
            return;
        }

        UnitMana mana = ResolveLocalPlayerMana();
        if (mana == null)
        {
            playerManaText.text = string.Empty;
            return;
        }

        playerManaText.text = "Mana: " + Mathf.CeilToInt(mana.currentMana) + " / " + Mathf.CeilToInt(mana.maxMana);
    }

    private UnitMana ResolveLocalPlayerMana()
    {
        if (localRoleController == null)
        {
            localRoleController = FindFirstObjectByType<LocalRoleController>();
        }

        if (localRoleController == null)
        {
            return null;
        }

        if (localRoleController.controlMode == PlayerControlMode.MonsterControlled
            && localRoleController.monsterMovement != null)
        {
            return localRoleController.monsterMovement.GetComponent<UnitMana>();
        }

        if (localRoleController.survivorMovement != null)
        {
            return localRoleController.survivorMovement.GetComponent<UnitMana>();
        }

        return null;
    }

    private void ApplySurvivorAbilityLabels()
    {
        SetButtonLabel(survivorPrimaryButton, survivorAbility1Label, "Biotic", survivorAbilityController, 1, false);
        SetButtonLabel(survivorAbility2Button, survivorAbility2Label, "Heal Pulse", survivorAbilityController, 2, false);
        SetButtonLabel(survivorAbility3Button, survivorAbility3Label, "Tether", survivorAbilityController, 3, false);
        SetButtonLabel(survivorUltimateButton, survivorUltimateLabel, "Sanctuary", survivorAbilityController, 4, false);
    }

    private void ApplyPredatorAbilityLabels()
    {
        SetButtonLabel(predatorMeleeButton, null, "Spray", predatorAbilityController, 1, true);
        SetButtonLabel(predatorAbility2Button, predatorAbility2Label, "Hook", predatorAbilityController, 2, true);
        SetButtonLabel(predatorAbility3Button, predatorAbility3Label, "Tonic", predatorAbilityController, 3, true);
        SetButtonLabel(predatorUltimateButton, predatorUltimateLabel, "Barrage", predatorAbilityController, 4, true);
    }

    private void SetButtonLabel(
        Button button,
        TMP_Text explicitLabel,
        string labelText,
        AbilityController controller,
        int slotNumber,
        bool predatorSide)
    {
        if (button == null)
        {
            return;
        }

        int manaCost = 0;
        if (controller != null)
        {
            manaCost = Mathf.RoundToInt(controller.GetSlotManaCost(slotNumber));
        }

        AbilityCooldownButton cooldownButton = button.GetComponent<AbilityCooldownButton>();
        if (cooldownButton != null)
        {
            cooldownButton.BindCooldownVisual(this, slotNumber, predatorSide);
            cooldownButton.SetAbilityInfo(labelText, manaCost);
        }

        TMP_Text resolvedLabel = explicitLabel != null ? explicitLabel : FindAbilityLabelText(button);
        if (resolvedLabel != null)
        {
            resolvedLabel.text = manaCost > 0
                ? labelText + "\n" + manaCost + " mana"
                : labelText;
            return;
        }

        if (!loggedMissingAbilityLabels)
        {
            loggedMissingAbilityLabels = true;
            Debug.Log("[ManiaGameUI] No ability label text found for button '" + button.name + "'. Assign a TMP child or AbilityCooldownButton.");
        }
    }

    private void SetButtonLabel(Button button, TMP_Text explicitLabel, string labelText)
    {
        SetButtonLabel(button, explicitLabel, labelText, null, 0, false);
    }

    private TMP_Text FindAbilityLabelText(Button button)
    {
        AbilityCooldownButton cooldownButton = button.GetComponent<AbilityCooldownButton>();
        if (cooldownButton != null && cooldownButton.abilityNameText != null)
        {
            return cooldownButton.abilityNameText;
        }

        TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            string lowerName = text.gameObject.name.ToLowerInvariant();
            if (lowerName.Contains("cooldown"))
            {
                continue;
            }

            return text;
        }

        return null;
    }

    private string GetPlayingRoleLabel()
    {
        if (localRoleController != null)
        {
            return localRoleController.controlMode == PlayerControlMode.MonsterControlled
                ? "Playing: Monster"
                : "Playing: Survivor";
        }

        return playingRoleText;
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.CeilToInt(seconds);
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return minutes.ToString("0") + ":" + remainingSeconds.ToString("00");
    }

}
