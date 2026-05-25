using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldHealthBar : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 worldOffset = new Vector3(0f, 2f, 0f);
    public Vector2 screenOffset = Vector2.zero;

    [Header("UI")]
    public Image fillImage;
    public TMP_Text hpText;

    [Header("Colors")]
    public Color highHealthColor = Color.green;
    public Color mediumHealthColor = new Color(1f, 0.55f, 0f, 1f);
    public Color lowHealthColor = Color.red;

    private Camera mainCamera;
    private Canvas canvasRoot;
    private RectTransform rectTransform;
    private SurvivorHealth survivorHealth;
    private Transform cachedTarget;
    private bool hasWarnedAboutCanvas;
    private bool hasWarnedAboutTarget;
    private bool hasWarnedAboutFillImage;
    private bool hasWarnedAboutHpText;
    private bool hasWarnedAboutCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        canvasRoot = GetComponent<Canvas>();
        rectTransform = GetComponent<RectTransform>();
        ValidateCanvasSetup();
        AutoAssignReferences();
    }

    private void OnEnable()
    {
        ValidateCanvasSetup();
        AutoAssignReferences();
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            if (!hasWarnedAboutCamera)
            {
                hasWarnedAboutCamera = true;
                Debug.LogWarning("[WorldHealthBar] No Main Camera found. Tag the active camera as MainCamera.", this);
            }
            mainCamera = Camera.main;
        }

        AutoAssignReferences();

        if (target == null || survivorHealth == null)
        {
            SetVisible(false);
            return;
        }

        Vector3 worldPosition = target.position + worldOffset;
        if (mainCamera == null)
        {
            SetVisible(false);
            return;
        }

        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z <= 0f)
        {
            SetVisible(false);
            return;
        }

        if (rectTransform != null)
        {
            rectTransform.position = screenPosition + (Vector3)screenOffset;
        }
        else
        {
            transform.position = screenPosition + (Vector3)screenOffset;
        }

        UpdateBar();
    }

    private void AutoAssignReferences()
    {
        if (fillImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i] != GetComponent<Image>())
                {
                    if (fillImage == null)
                    {
                        fillImage = images[i];
                    }

                    if (images[i].type == Image.Type.Filled)
                    {
                        fillImage = images[i];
                        break;
                    }
                }
            }
        }

        if (hpText == null)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null)
                {
                    hpText = texts[i];
                    break;
                }
            }
        }

        if (survivorHealth == null)
        {
            survivorHealth = GetComponentInParent<SurvivorHealth>();
        }

        if (target == null)
        {
            if (survivorHealth != null)
            {
                target = survivorHealth.transform;
            }
            else if (transform.parent != null)
            {
                target = transform.parent;
            }
        }

        if (survivorHealth == null && target != null)
        {
            survivorHealth = target != null ? target.GetComponentInParent<SurvivorHealth>() : null;
        }

        if (target != cachedTarget)
        {
            cachedTarget = target;
        }

        if (survivorHealth == null && !hasWarnedAboutTarget)
        {
            hasWarnedAboutTarget = true;
            Debug.LogWarning("[WorldHealthBar] No SurvivorHealth found. Put this bar under a Survivor prefab or assign Target manually.", this);
        }

        if (fillImage == null && !hasWarnedAboutFillImage)
        {
            hasWarnedAboutFillImage = true;
            Debug.LogWarning("[WorldHealthBar] Fill Image is missing. Assign a child Image set to Filled, or name it Fill.", this);
        }

        if (hpText == null && !hasWarnedAboutHpText)
        {
            hasWarnedAboutHpText = true;
            Debug.LogWarning("[WorldHealthBar] Hp Text is missing. Assign a child TMP_Text object.", this);
        }
    }

    private void UpdateBar()
    {
        if (survivorHealth == null)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        int currentHealth = survivorHealth.GetCurrentHealth();
        int maxHealth = survivorHealth.GetMaxHealth();

        if (maxHealth <= 0)
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
                fillImage.color = lowHealthColor;
            }

            if (hpText != null)
            {
                hpText.text = "0";
            }

            return;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        float healthPercent = survivorHealth.GetHealthPercent();

        if (fillImage != null)
        {
            fillImage.fillAmount = healthPercent;
            fillImage.color = GetHealthColor(healthPercent);
        }

        if (hpText != null)
        {
            hpText.text = currentHealth.ToString();
        }
    }

    private Color GetHealthColor(float healthPercent)
    {
        if (healthPercent >= 0.65f)
        {
            return highHealthColor;
        }

        if (healthPercent >= 0.30f)
        {
            return mediumHealthColor;
        }

        return lowHealthColor;
    }

    private void SetVisible(bool isVisible)
    {
        if (canvasRoot != null)
        {
            canvasRoot.enabled = isVisible;
        }

        if (fillImage != null)
        {
            fillImage.enabled = isVisible;
        }

        if (hpText != null)
        {
            hpText.enabled = isVisible;
        }
    }

    private void ValidateCanvasSetup()
    {
        if (canvasRoot == null && !hasWarnedAboutCanvas)
        {
            hasWarnedAboutCanvas = true;
            Debug.LogWarning("[WorldHealthBar] Missing Canvas component. Add a Canvas to the health bar prefab.", this);
            return;
        }

        if (canvasRoot != null && canvasRoot.renderMode == RenderMode.WorldSpace && !hasWarnedAboutCanvas)
        {
            hasWarnedAboutCanvas = true;
            Debug.LogWarning("[WorldHealthBar] This version uses screen-space placement. Change the Canvas render mode to Screen Space Overlay or Screen Space Camera.", this);
        }
    }
}
