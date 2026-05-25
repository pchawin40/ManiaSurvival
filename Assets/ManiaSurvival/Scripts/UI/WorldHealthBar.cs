using System.Reflection;
using TMPro;
using UnityEngine;

public class WorldHealthBar : MonoBehaviour
{
    [Header("Bar Parts")]
    public Transform backgroundBar;
    public Transform fillBar;
    public TextMeshPro hpText;

    [Header("Placement")]
    [Tooltip("Extra local offset added after the bar is placed above the unit.")]
    public Vector3 extraOffset = Vector3.zero;
    public bool autoPosition = true;

    [Header("Colors")]
    public Color highHealthColor = Color.green;
    public Color mediumHealthColor = new Color(1f, 0.55f, 0f, 1f);
    public Color lowHealthColor = Color.red;

    private Camera mainCamera;
    private Transform unitRoot;
    private SurvivorHealth survivorHealth;
    private Component genericHealthSource;
    private Renderer[] fillRenderers;
    private Vector3 fillBaseScale;
    private Vector3 fillBaseLocalPosition;
    private bool warnedMissingHealth;
    private bool warnedMissingCamera;
    private bool warnedMissingFill;
    private bool warnedMissingText;

    private MethodInfo getCurrentHealthMethod;
    private MethodInfo getMaxHealthMethod;
    private PropertyInfo currentHealthProperty;
    private PropertyInfo maxHealthProperty;
    private FieldInfo currentHealthField;
    private FieldInfo maxHealthField;

    private void Awake()
    {
        mainCamera = Camera.main;
        unitRoot = transform.parent;

        AutoAssignParts();
        CacheBarDefaults();
        ResolveHealthSource();
    }

    private void OnEnable()
    {
        mainCamera = Camera.main;
        unitRoot = transform.parent;

        AutoAssignParts();
        CacheBarDefaults();
        ResolveHealthSource();
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                if (!warnedMissingCamera)
                {
                    warnedMissingCamera = true;
                    Debug.LogWarning("[WorldHealthBar] No Main Camera found. Tag the active camera as MainCamera.", this);
                }
                return;
            }
        }

        if (unitRoot == null)
        {
            unitRoot = transform.parent;
        }

        ResolveHealthSource();
        AutoAssignParts();

        if (unitRoot == null)
        {
            return;
        }

        if (survivorHealth == null && genericHealthSource == null)
        {
            if (!warnedMissingHealth)
            {
                warnedMissingHealth = true;
                Debug.LogWarning("[WorldHealthBar] No health component found on the unit parent. Add SurvivorHealth now, or a future MonsterHealth later.", this);
            }

            return;
        }

        UpdatePlacement();
        FaceCamera();
        UpdateBar();
    }

    private void AutoAssignParts()
    {
        if (backgroundBar == null && transform.childCount > 0)
        {
            backgroundBar = transform.GetChild(0);
        }

        if (fillBar == null && backgroundBar != null && backgroundBar.childCount > 0)
        {
            fillBar = backgroundBar.GetChild(0);
        }

        if (hpText == null)
        {
            hpText = GetComponentInChildren<TextMeshPro>(true);
        }

        if (fillBar != null && fillRenderers == null)
        {
            fillRenderers = fillBar.GetComponentsInChildren<Renderer>(true);
        }

        if (fillBar == null && !warnedMissingFill)
        {
            warnedMissingFill = true;
            Debug.LogWarning("[WorldHealthBar] Missing fillBar. Create a child 3D object for the fill and assign it here.", this);
        }

        if (hpText == null && !warnedMissingText)
        {
            warnedMissingText = true;
            Debug.LogWarning("[WorldHealthBar] Missing hpText. Add a TextMeshPro component to the prefab and assign it here.", this);
        }
    }

    private void CacheBarDefaults()
    {
        if (fillBar != null)
        {
            fillBaseScale = fillBar.localScale;
            fillBaseLocalPosition = fillBar.localPosition;
        }

    }

    private void ResolveHealthSource()
    {
        if (unitRoot == null)
        {
            survivorHealth = null;
            genericHealthSource = null;
            return;
        }

        SurvivorHealth foundSurvivorHealth = GetComponentInParent<SurvivorHealth>();
        if (foundSurvivorHealth != null)
        {
            survivorHealth = foundSurvivorHealth;
            genericHealthSource = null;
            return;
        }

        survivorHealth = null;
        genericHealthSource = null;
        getCurrentHealthMethod = null;
        getMaxHealthMethod = null;
        currentHealthProperty = null;
        maxHealthProperty = null;
        currentHealthField = null;
        maxHealthField = null;

        MonoBehaviour[] components = unitRoot.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < components.Length; i++)
        {
            MonoBehaviour component = components[i];
            if (component == null || component == this || component.transform == transform || component.transform.IsChildOf(transform))
            {
                continue;
            }

            if (TryCacheHealthMembers(component))
            {
                genericHealthSource = component;
                return;
            }
        }
    }

    private bool TryCacheHealthMembers(Component component)
    {
        System.Type type = component.GetType();

        getCurrentHealthMethod = type.GetMethod("GetCurrentHealth", BindingFlags.Instance | BindingFlags.Public);
        getMaxHealthMethod = type.GetMethod("GetMaxHealth", BindingFlags.Instance | BindingFlags.Public);

        currentHealthProperty = type.GetProperty("CurrentHealth", BindingFlags.Instance | BindingFlags.Public);
        if (currentHealthProperty == null)
        {
            currentHealthProperty = type.GetProperty("currentHealth", BindingFlags.Instance | BindingFlags.Public);
        }

        maxHealthProperty = type.GetProperty("MaxHealth", BindingFlags.Instance | BindingFlags.Public);
        if (maxHealthProperty == null)
        {
            maxHealthProperty = type.GetProperty("maxHealth", BindingFlags.Instance | BindingFlags.Public);
        }

        currentHealthField = type.GetField("CurrentHealth", BindingFlags.Instance | BindingFlags.Public);
        if (currentHealthField == null)
        {
            currentHealthField = type.GetField("currentHealth", BindingFlags.Instance | BindingFlags.Public);
        }

        maxHealthField = type.GetField("MaxHealth", BindingFlags.Instance | BindingFlags.Public);
        if (maxHealthField == null)
        {
            maxHealthField = type.GetField("maxHealth", BindingFlags.Instance | BindingFlags.Public);
        }

        return HasHealthMembers();
    }

    private bool HasHealthMembers()
    {
        bool hasCurrent =
            getCurrentHealthMethod != null ||
            currentHealthProperty != null ||
            currentHealthField != null;

        bool hasMax =
            getMaxHealthMethod != null ||
            maxHealthProperty != null ||
            maxHealthField != null;

        return hasCurrent && hasMax;
    }

    private void UpdatePlacement()
    {
        if (!autoPosition || unitRoot == null)
        {
            return;
        }

        float calculatedHeight = GetCalculatedHeight();
        transform.localPosition = new Vector3(0f, calculatedHeight + extraOffset.y, 0f);
    }

    private float GetCalculatedHeight()
    {
        CharacterController controller = unitRoot.GetComponent<CharacterController>();
        if (controller != null)
        {
            return controller.center.y + controller.height * 0.5f;
        }

        if (TryGetRendererBounds(unitRoot, out Bounds bounds))
        {
            return bounds.max.y - unitRoot.position.y;
        }

        return 2f;
    }

    private bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        bounds = default;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.transform == transform || renderer.transform.IsChildOf(transform))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void FaceCamera()
    {
        if (mainCamera == null || backgroundBar == null)
        {
            return;
        }

        Vector3 lookDirection = backgroundBar.position - mainCamera.transform.position;
        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        backgroundBar.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
    }

    private void UpdateBar()
    {
        if (!TryGetHealthValues(out int currentHealth, out int maxHealth))
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth);
        maxHealth = Mathf.Max(0, maxHealth);

        if (maxHealth <= 0)
        {
            SetFillScale(0f);
            SetFillColor(lowHealthColor);

            if (hpText != null)
            {
                hpText.text = "0";
            }

            return;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        float healthPercent = (float)currentHealth / maxHealth;

        SetFillScale(healthPercent);
        SetFillColor(GetHealthColor(healthPercent));

        if (hpText != null)
        {
            hpText.text = currentHealth.ToString();
        }
    }

    private void SetFillScale(float healthPercent)
    {
        if (fillBar == null)
        {
            return;
        }

        float clampedPercent = Mathf.Clamp01(healthPercent);
        Vector3 scale = fillBaseScale;
        scale.x = fillBaseScale.x * clampedPercent;
        fillBar.localScale = scale;

        Vector3 position = fillBaseLocalPosition;
        position.x = fillBaseLocalPosition.x - (fillBaseScale.x - scale.x) * 0.5f;
        fillBar.localPosition = position;
    }

    private void SetFillColor(Color color)
    {
        if (fillRenderers == null)
        {
            return;
        }

        for (int i = 0; i < fillRenderers.Length; i++)
        {
            Renderer renderer = fillRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (renderer.material.HasProperty("_BaseColor"))
            {
                renderer.material.color = color;
            }
            else if (renderer.material.HasProperty("_Color"))
            {
                renderer.material.color = color;
            }
        }
    }

    private bool TryGetHealthValues(out int currentHealth, out int maxHealth)
    {
        currentHealth = 0;
        maxHealth = 0;

        if (survivorHealth != null)
        {
            currentHealth = survivorHealth.GetCurrentHealth();
            maxHealth = survivorHealth.GetMaxHealth();
            return true;
        }

        if (genericHealthSource == null)
        {
            return false;
        }

        if (getCurrentHealthMethod != null)
        {
            currentHealth = (int)getCurrentHealthMethod.Invoke(genericHealthSource, null);
        }
        else if (currentHealthProperty != null)
        {
            currentHealth = (int)currentHealthProperty.GetValue(genericHealthSource);
        }
        else if (currentHealthField != null)
        {
            currentHealth = (int)currentHealthField.GetValue(genericHealthSource);
        }
        else
        {
            return false;
        }

        if (getMaxHealthMethod != null)
        {
            maxHealth = (int)getMaxHealthMethod.Invoke(genericHealthSource, null);
        }
        else if (maxHealthProperty != null)
        {
            maxHealth = (int)maxHealthProperty.GetValue(genericHealthSource);
        }
        else if (maxHealthField != null)
        {
            maxHealth = (int)maxHealthField.GetValue(genericHealthSource);
        }
        else
        {
            return false;
        }

        return true;
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
}
