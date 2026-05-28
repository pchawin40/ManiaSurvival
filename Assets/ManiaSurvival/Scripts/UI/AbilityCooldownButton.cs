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

    [Header("Ability Info")]
    [Tooltip("Display name shown on the button (e.g. 'Roar'). Abilities will overwrite this on Awake.")]
    public string abilityName = "";
    [Tooltip("Mana the ability costs. 0 means no mana label is shown.")]
    [Min(0)] public int manaCost = 0;
    [Tooltip("Format used to combine name + mana cost. {0} = name, {1} = cost.")]
    public string manaCostLabelFormat = "{0}\n{1} mana";
    [Tooltip("If true, the Ability Name Text is overwritten with the formatted label. Turn off if you want to author the label manually.")]
    public bool useAbilityLabel = true;

    private float cooldownEndTime;

    public bool IsCoolingDown => cooldownEndTime > Time.time;

    private void Awake()
    {
        CacheReferences();
        ApplyAbilityLabel();
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
