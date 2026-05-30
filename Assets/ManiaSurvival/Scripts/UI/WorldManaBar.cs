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

    [Header("Rotation")]
    [Tooltip("Mirrors WorldHealthBar so the mana bar tilts to face the top-down camera the same way.")]
    public bool applyBillboardRotation = true;
    public Vector3 billboardEulerAngles = new Vector3(60f, 0f, 0f);

    private void Awake()
    {
        TryResolveMana();
    }

    private void LateUpdate()
    {
        if (unitMana == null && autoFindOnRoot)
        {
            TryResolveMana();
        }

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
}
