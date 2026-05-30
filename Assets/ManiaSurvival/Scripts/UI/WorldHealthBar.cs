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

    [Header("Damage Feedback")]
    [Tooltip("Tiny placeholder flash when this unit takes damage.")]
    public bool enableDamageFlash = true;
    [Tooltip("How long the damage flash lasts.")]
    public float damageFlashDuration = 0.12f;
    public Color damageFlashColor = Color.white;

    [Header("Rotation")]
    [Tooltip("Tilt this transform to face the top-down camera. Disable when this script sits on a unit root with a CharacterController.")]
    public bool applyBillboardRotation = true;
    public Vector3 billboardEulerAngles = new Vector3(60f, 0f, 0f);

    private float damageFlashUntil;
    private bool loggedBillboardDisabled;

    private void Awake()
    {
        if (applyBillboardRotation && GetComponent<CharacterController>() != null)
        {
            applyBillboardRotation = false;

            if (!loggedBillboardDisabled)
            {
                loggedBillboardDisabled = true;
                Debug.LogWarning("[WorldHealthBar] Disabled billboard rotation on unit root '" + gameObject.name + "'. Move WorldHealthBar to the HP bar canvas child instead.");
            }
        }
    }

    private void OnEnable()
    {
        if (unitHealth != null)
        {
            unitHealth.onDamaged.AddListener(OnUnitDamaged);
        }
    }

    private void OnDisable()
    {
        if (unitHealth != null)
        {
            unitHealth.onDamaged.RemoveListener(OnUnitDamaged);
        }
    }

    private void LateUpdate()
    {
        if (unitHealth != null)
        {
            float percent = unitHealth.GetHealthPercent();
            Color baseHealthColor = GetHealthColor(percent);

            if (fillImage != null)
            {
                fillImage.fillAmount = percent;
                fillImage.color = (enableDamageFlash && Time.time < damageFlashUntil)
                    ? damageFlashColor
                    : baseHealthColor;
            }

            if (healthText != null)
            {
                healthText.text = $"{unitHealth.currentHealth} / {unitHealth.maxHealth}";
                healthText.color = (enableDamageFlash && Time.time < damageFlashUntil)
                    ? lowHealthColor
                    : Color.white;
            }
        }

        if (applyBillboardRotation)
        {
            transform.rotation = Quaternion.Euler(billboardEulerAngles);
        }
    }

    private void OnUnitDamaged()
    {
        if (!enableDamageFlash)
        {
            return;
        }

        damageFlashUntil = Time.time + Mathf.Max(0.01f, damageFlashDuration);
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
