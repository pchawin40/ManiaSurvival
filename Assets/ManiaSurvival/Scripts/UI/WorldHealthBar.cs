using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldHealthBar : MonoBehaviour
{
    [Header("Target")]
    public UnitHealth unitHealth;

    [Header("UI")]
    public Image fillImage;
    public TMP_Text healthText;

    [Header("Color Thresholds")]
    [Range(0f, 1f)] public float lowHealthPercent = 0.30f;
    [Range(0f, 1f)] public float mediumHealthPercent = 0.60f;

    [Header("Colors")]
    public Color highHealthColor = Color.green;
    public Color mediumHealthColor = new Color(1f, 0.5f, 0f); // orange
    public Color lowHealthColor = Color.red;

    void LateUpdate()
    {
        if (unitHealth != null)
        {
            float percent = unitHealth.GetHealthPercent();

            if (fillImage != null)
            {
                fillImage.fillAmount = percent;
                fillImage.color = GetHealthColor(percent);
            }

            if (healthText != null)
            {
                healthText.text = $"{unitHealth.currentHealth} / {unitHealth.maxHealth}";
            }
        }

        transform.rotation = Quaternion.Euler(60f, 0f, 0f);
    }

    Color GetHealthColor(float percent)
    {
        if (percent < lowHealthPercent)
        {
            return lowHealthColor;
        }

        if (percent <= mediumHealthPercent)
        {
            return mediumHealthColor;
        }

        return highHealthColor;
    }
}
