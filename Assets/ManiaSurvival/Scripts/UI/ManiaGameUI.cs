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
    public TMP_Text survivorCountText;
    public TMP_Text monsterKillsText;
    public TMP_Text gameOverTitleText;

    [Header("Panels")]
    public GameObject startScreen;
    public GameObject gameplayHUD;
    public GameObject gameOverPanel;

    [Header("Buttons")]
    public Button startButton;
    public Button restartButton;

    [Header("Mobile Joystick")]
    public bool enableSimpleMobileJoystick = true;
    public RectTransform joystickArea;
    public RectTransform joystickKnob;
    public Camera uiCamera;
    public float joystickRadius = 80f;

    [Header("Labels")]
    public string playingRoleText = "Survive until the timer ends";
    public string monsterWinText = "Monster Wins";
    public string survivorWinText = "Survivors Win";

    [Header("References")]
    public ManiaGameManager gameManager;
    public LocalRoleController localRoleController;
    public SurvivorMovement localSurvivorMovement;

    private int activeTouchId = -1;
    private bool mouseJoystickActive;

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

        if (startButton != null)
        {
            startButton.onClick.AddListener(HandleStartPressed);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(HandleStartPressed);
        }

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

        if (gameplayHUD != null)
        {
            gameplayHUD.SetActive(isPlaying);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(isGameOver);
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

    private void StartGameWithMode(PlayerControlMode mode)
    {
        if (localRoleController != null)
        {
            localRoleController.SetControlMode(mode);
        }

        if (gameManager != null)
        {
            gameManager.BeginRound();
        }
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
