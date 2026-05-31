using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a compact world-space HP bar for summons and small units.
/// </summary>
public static class MiniWorldHealthBarBuilder
{
    public static WorldHealthBar Attach(
        UnitHealth unitHealth,
        float widthScale = 0.52f,
        float heightOffset = 0.72f)
    {
        if (unitHealth == null)
        {
            return null;
        }

        Transform existing = unitHealth.transform.Find("MiniHealthBar");
        if (existing != null)
        {
            WorldHealthBar existingBar = existing.GetComponent<WorldHealthBar>();
            if (existingBar != null)
            {
                existingBar.unitHealth = unitHealth;
                return existingBar;
            }
        }

        GameObject anchor = new GameObject("MiniHealthBar", typeof(RectTransform));
        anchor.transform.SetParent(unitHealth.transform, false);
        anchor.transform.localPosition = new Vector3(0f, heightOffset, 0f);

        Canvas canvas = anchor.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 45;

        RectTransform canvasRect = anchor.GetComponent<RectTransform>();
        float barWidth = 96f * Mathf.Clamp(widthScale, 0.35f, 0.75f);
        float barHeight = 10f;
        canvasRect.sizeDelta = new Vector2(barWidth, barHeight);
        canvasRect.localScale = Vector3.one * 0.011f;

        Image background = CreateBarImage(anchor.transform, "Background", new Color(0.1f, 0.12f, 0.14f, 0.9f));
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image fill = CreateBarImage(anchor.transform, "Fill", new Color(0.45f, 0.92f, 0.28f, 1f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        WorldHealthBar bar = anchor.AddComponent<WorldHealthBar>();
        bar.unitHealth = unitHealth;
        bar.fillImage = fill;
        bar.healthText = null;
        bar.hideWhenDead = true;
        bar.applyBillboardRotation = true;
        bar.billboardEulerAngles = new Vector3(60f, 0f, 0f);
        bar.enableDamageFlash = true;
        bar.highHealthColor = new Color(0.45f, 0.92f, 0.28f, 1f);
        bar.mediumHealthColor = new Color(0.95f, 0.78f, 0.18f, 1f);
        bar.lowHealthColor = new Color(1f, 0.35f, 0.22f, 1f);

        return bar;
    }

    private static Image CreateBarImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
}
