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
    public Color cooldownColor = Color.gray;

    private float cooldownEndTime;

    public bool IsCoolingDown => cooldownEndTime > Time.time;

    private void Awake()
    {
        CacheReferences();
        ApplyReadyVisuals();
    }

    private void Update()
    {
        if (!IsCoolingDown)
        {
            if (cooldownEndTime > 0f)
            {
                cooldownEndTime = 0f;
                ApplyReadyVisuals();
            }

            return;
        }

        if (button != null)
        {
            button.interactable = false;
        }

        SetTintColor(cooldownColor);

        if (cooldownText != null)
        {
            float remaining = Mathf.Max(0f, cooldownEndTime - Time.time);
            int seconds = Mathf.CeilToInt(remaining);
            cooldownText.text = seconds > 0 ? seconds + " sec" : "";
        }
    }

    public void StartCooldown(float duration)
    {
        CacheReferences();

        float cooldownDuration = Mathf.Max(0f, duration);
        if (cooldownDuration <= 0f)
        {
            cooldownEndTime = 0f;
            ApplyReadyVisuals();
            return;
        }

        cooldownEndTime = Time.time + cooldownDuration;

        if (button != null)
        {
            button.interactable = false;
        }

        SetTintColor(cooldownColor);

        if (cooldownText != null)
        {
            cooldownText.text = Mathf.CeilToInt(cooldownDuration) + " sec";
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

    private void ApplyReadyVisuals()
    {
        if (button != null)
        {
            button.interactable = true;
        }

        SetTintColor(readyColor);

        if (cooldownText != null)
        {
            cooldownText.text = "";
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
