using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldManaBar : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Drag a UnitMana here, or leave empty to auto-find one on this object's root.")]
    public UnitMana unitMana;

    [Header("Auto Find")]
    [Tooltip("If unitMana is empty, keep searching the root each frame until one shows up (cheap).")]
    public bool autoFindOnRoot = true;

    [Header("UI")]
    public Image fillImage;
    public TMP_Text manaText;

    [Header("Color")]
    public Color manaColor = new Color(0.25f, 0.6f, 1f);

    [Header("Visibility")]
    [Tooltip("Hide the bar visuals when no UnitMana exists yet.")]
    public bool hideWhenNoMana = true;
    [Tooltip("Hide the bar when the linked survivor/unit is dead.")]
    public bool hideWhenDead = true;

    [Header("Rotation")]
    [Tooltip("Mirrors WorldHealthBar so the mana bar tilts to face the top-down camera the same way.")]
    public bool applyBillboardRotation = true;
    public Vector3 billboardEulerAngles = new Vector3(60f, 0f, 0f);

    private UnitHealth unitHealth;
    private Canvas barCanvas;

    private void Awake()
    {
        barCanvas = GetComponent<Canvas>();
        TryResolveMana();
        TryResolveHealth();
    }

    private void LateUpdate()
    {
        if (unitHealth == null)
        {
            TryResolveHealth();
        }

        if (unitMana == null && autoFindOnRoot)
        {
            TryResolveMana();
        }

        if (hideWhenDead && unitHealth != null && unitHealth.IsDead)
        {
            SetVisualsActive(false);
            SetBarCanvasVisible(false);

            if (applyBillboardRotation)
            {
                transform.rotation = Quaternion.Euler(billboardEulerAngles);
            }

            return;
        }

        SetBarCanvasVisible(true);

        bool hasMana = unitMana != null;

        if (hideWhenNoMana)
        {
            SetVisualsActive(hasMana);
        }

        if (hasMana)
        {
            float percent = unitMana.GetManaPercent();

            if (fillImage != null)
            {
                fillImage.fillAmount = percent;
                fillImage.color = manaColor;
            }

            if (manaText != null)
            {
                manaText.text = $"{Mathf.CeilToInt(unitMana.currentMana)} / {Mathf.CeilToInt(unitMana.maxMana)}";
            }
        }

        if (applyBillboardRotation)
        {
            transform.rotation = Quaternion.Euler(billboardEulerAngles);
        }
    }

    private void TryResolveMana()
    {
        if (unitMana != null)
        {
            return;
        }

        unitMana = GetComponentInParent<UnitMana>();
        if (unitMana == null)
        {
            SurvivorMana legacy = GetComponentInParent<SurvivorMana>();
            if (legacy != null)
            {
                unitMana = legacy.GetComponent<UnitMana>();
            }
        }
    }

    private void TryResolveHealth()
    {
        if (unitHealth != null)
        {
            return;
        }

        WorldHealthBar healthBar = GetComponent<WorldHealthBar>();
        if (healthBar != null && healthBar.unitHealth != null)
        {
            unitHealth = healthBar.unitHealth;
            return;
        }

        if (transform.parent != null)
        {
            unitHealth = transform.parent.GetComponentInParent<UnitHealth>();
        }
    }

    private void SetVisualsActive(bool active)
    {
        if (fillImage != null && fillImage.enabled != active)
        {
            fillImage.enabled = active;
        }

        if (manaText != null && manaText.enabled != active)
        {
            manaText.enabled = active;
        }
    }

    private void SetBarCanvasVisible(bool visible)
    {
        if (barCanvas != null && barCanvas.enabled != visible)
        {
            barCanvas.enabled = visible;
        }
    }
}
