using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a compact world-space HP bar for summons and small units.
/// </summary>
public static class MiniWorldHealthBarBuilder
{
    private static Sprite cachedWhiteSprite;

    public static WorldHealthBar Attach(
        UnitHealth unitHealth,
        float worldWidth = 0.6f,
        float worldHeight = 0.08f,
        float yOffset = 0.8f)
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

        worldWidth = Mathf.Clamp(worldWidth, 0.35f, 1.2f);
        worldHeight = Mathf.Clamp(worldHeight, 0.05f, 0.2f);
        yOffset = Mathf.Max(0.4f, yOffset);

        GameObject anchor = new GameObject("MiniHealthBar", typeof(RectTransform));
        anchor.transform.SetParent(unitHealth.transform, false);
        anchor.transform.localPosition = new Vector3(0f, yOffset, 0f);
        anchor.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);

        Canvas canvas = anchor.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 120;
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            canvas.worldCamera = mainCamera;
        }

        RectTransform canvasRect = anchor.GetComponent<RectTransform>();
        const float pixelsPerWorldUnit = 100f;
        canvasRect.sizeDelta = new Vector2(worldWidth * pixelsPerWorldUnit, worldHeight * pixelsPerWorldUnit);
        canvasRect.localScale = Vector3.one * (1f / pixelsPerWorldUnit);

        Image background = CreateBarImage(anchor.transform, "Background", new Color(0.08f, 0.08f, 0.08f, 0.92f));
        StretchRect(background.rectTransform);

        Image fill = CreateBarImage(anchor.transform, "Fill", new Color(0.35f, 0.95f, 0.25f, 1f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(-1f, -1f);

        WorldHealthBar bar = anchor.AddComponent<WorldHealthBar>();
        bar.unitHealth = unitHealth;
        bar.fillImage = fill;
        bar.healthText = null;
        bar.hideWhenDead = true;
        bar.applyBillboardRotation = false;
        bar.enableDamageFlash = true;
        bar.highHealthColor = new Color(0.35f, 0.95f, 0.25f, 1f);
        bar.mediumHealthColor = new Color(0.95f, 0.78f, 0.18f, 1f);
        bar.lowHealthColor = new Color(1f, 0.35f, 0.22f, 1f);

        return bar;
    }

    private static Image CreateBarImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.sprite = GetWhiteSprite();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Sprite GetWhiteSprite()
    {
        if (cachedWhiteSprite != null)
        {
            return cachedWhiteSprite;
        }

        cachedWhiteSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        if (cachedWhiteSprite == null)
        {
            cachedWhiteSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        }

        if (cachedWhiteSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            cachedWhiteSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        return cachedWhiteSprite;
    }
}
