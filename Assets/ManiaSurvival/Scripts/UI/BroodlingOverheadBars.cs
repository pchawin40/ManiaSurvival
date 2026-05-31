using UnityEngine;

/// <summary>
/// Lightweight world-space HP + lifetime bars for Swarm broodlings (primitive quads).
/// </summary>
[DisallowMultipleComponent]
public class BroodlingOverheadBars : MonoBehaviour
{
    private const float BarGap = 0.02f;
    private const float FillZOffset = 0.002f;

    [Header("Visibility")]
    public bool showHpBar = true;
    public bool showLifetimeBar = true;

    [Header("Layout")]
    public Vector3 barOffset = Vector3.up * 0.85f;
    public float hpBarWidth = 0.65f;
    public float hpBarHeight = 0.08f;
    public float timerBarHeight = 0.05f;

    [Header("Rotation")]
    public bool useBillboardToCamera = true;

    private BroodlingMinion broodling;
    private UnitHealth unitHealth;
    private Transform hpFillTransform;
    private Transform timerFillTransform;
    private float hpFillWidth;
    private float timerFillWidth;
    private bool hpBarReady;
    private bool timerBarReady;

    private readonly Color hpFillColor = new Color(0.35f, 0.95f, 0.25f, 1f);
    private readonly Color timerFillColor = new Color(0.95f, 0.55f, 0.12f, 1f);
    private readonly Color timerDimmedColor = new Color(0.55f, 0.35f, 0.08f, 0.55f);
    private readonly Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.92f);

    public static BroodlingOverheadBars Attach(BroodlingMinion minion, UnitHealth health, PredatorClassManager owner = null)
    {
        if (minion == null)
        {
            Debug.LogWarning("[SwarmBarsERROR] Missing BroodlingMinion reference, skipping overhead bars.");
            return null;
        }

        if (health == null)
        {
            Debug.LogWarning("[SwarmBarsERROR] Missing UnitHealth on " + minion.name + ", skipping overhead bars.");
            return null;
        }

        Debug.Log("[SwarmBars] Creating runtime overhead bars for " + minion.name + ".");

        string barsName = "BroodlingBars_" + minion.name;
        Transform existing = minion.transform.Find(barsName);
        if (existing != null)
        {
            BroodlingOverheadBars existingBars = existing.GetComponent<BroodlingOverheadBars>();
            if (existingBars != null)
            {
                existingBars.broodling = minion;
                existingBars.unitHealth = health;
                return existingBars;
            }
        }

        GameObject anchor = new GameObject(barsName);
        anchor.transform.SetParent(minion.transform, false);

        BroodlingOverheadBars bars = anchor.AddComponent<BroodlingOverheadBars>();
        bars.broodling = minion;
        bars.unitHealth = health;
        bars.ConfigureFromOwner(owner);
        bars.BuildBars();
        Debug.Log("[SwarmBars] HP bar ready=" + bars.hpBarReady + ", Timer bar ready=" + bars.timerBarReady + ".");
        return bars;
    }

    public void ConfigureFromOwner(PredatorClassManager owner)
    {
        if (owner == null)
        {
            return;
        }

        barOffset = Vector3.up * owner.broodlingHealthBarYOffset;
        hpBarWidth = owner.broodlingHealthBarWidth;
        hpBarHeight = owner.broodlingHealthBarHeight;
        timerBarHeight = 0.05f;
    }

    private void BuildBars()
    {
        hpBarWidth = Mathf.Clamp(hpBarWidth, 0.35f, 1.2f);
        hpBarHeight = Mathf.Clamp(hpBarHeight, 0.05f, 0.2f);
        timerBarHeight = Mathf.Clamp(timerBarHeight, 0.04f, 0.15f);
        transform.localPosition = barOffset;

        float timerCenterY = 0f;
        float hpCenterY = timerBarHeight * 0.5f + BarGap + hpBarHeight * 0.5f;

        if (showHpBar)
        {
            CreateBarRow("HpBar", hpCenterY, hpBarWidth, hpBarHeight, backgroundColor, hpFillColor, out hpFillTransform, out hpFillWidth);
            hpBarReady = hpFillTransform != null;
            if (!hpBarReady)
            {
                Debug.LogWarning("[SwarmBarsERROR] Missing HP fill on " + broodling.name + ", using fallback.");
            }
        }

        if (showLifetimeBar)
        {
            CreateBarRow("TimerBar", timerCenterY, hpBarWidth, timerBarHeight, backgroundColor, timerFillColor, out timerFillTransform, out timerFillWidth);
            timerBarReady = timerFillTransform != null;
            if (!timerBarReady)
            {
                Debug.LogWarning("[SwarmBarsERROR] Missing timer fill on " + broodling.name + ", using fallback.");
            }
        }
    }

    private void CreateBarRow(
        string rowName,
        float centerY,
        float width,
        float height,
        Color bgColor,
        Color fillColor,
        out Transform fillTransform,
        out float fillWidth)
    {
        fillTransform = null;
        fillWidth = width;

        GameObject row = new GameObject(rowName);
        row.transform.SetParent(transform, false);
        row.transform.localPosition = new Vector3(0f, centerY, 0f);

        CreateQuad(row.transform, "Background", Vector3.zero, width, height, bgColor);

        GameObject fillObj = CreateQuad(row.transform, "Fill", Vector3.zero, width, height, fillColor);
        if (fillObj == null)
        {
            return;
        }

        fillTransform = fillObj.transform;
        fillTransform.localPosition = new Vector3(0f, 0f, -FillZOffset);
    }

    private static GameObject CreateQuad(Transform parent, string name, Vector3 localPos, float width, float height, Color color)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        if (quad == null)
        {
            Debug.LogWarning("[SwarmBarsERROR] Failed to create quad for " + name + ".");
            return null;
        }

        quad.name = name;
        Collider collider = quad.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = localPos;
        quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localScale = new Vector3(width, height, 1f);

        Renderer renderer = quad.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateUnlitMaterial(color);
        }

        return quad;
    }

    private static Material CreateUnlitMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    private void LateUpdate()
    {
        if (broodling == null || unitHealth == null)
        {
            SetVisible(false);
            return;
        }

        if (unitHealth.IsDead || broodling.IsExpiring)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        if (showHpBar && hpFillTransform != null)
        {
            float fill = unitHealth.GetHealthPercent();
            ApplyHorizontalFill(hpFillTransform, hpFillWidth, hpBarHeight, fill);
            SetFillColor(hpFillTransform, hpFillColor);
        }

        if (showLifetimeBar && timerFillTransform != null)
        {
            float fill = broodling.Lifetime01;
            ApplyHorizontalFill(timerFillTransform, timerFillWidth, timerBarHeight, fill);
            SetFillColor(timerFillTransform, broodling.IsHatched ? timerFillColor : timerDimmedColor);
        }

        if (useBillboardToCamera)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 forward = cam.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
                }
            }
        }
    }

    private static void ApplyHorizontalFill(Transform fillTransform, float width, float height, float fill01)
    {
        fill01 = Mathf.Clamp01(fill01);
        fillTransform.localScale = new Vector3(width * fill01, height, 1f);
        fillTransform.localPosition = new Vector3(-width * 0.5f * (1f - fill01), 0f, -FillZOffset);
    }

    private static void SetFillColor(Transform fillTransform, Color color)
    {
        Renderer renderer = fillTransform.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            renderer.sharedMaterial.color = color;
        }
    }

    private void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
