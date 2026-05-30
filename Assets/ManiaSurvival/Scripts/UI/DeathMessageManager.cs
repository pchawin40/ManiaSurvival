using System.Collections;
using TMPro;
using UnityEngine;

public enum DeathCause
{
    Unknown,
    PredatorAttack,
    SurvivorAttack,
    Lava,
    Hazard,
    NPC,
    Fall,
    Ability
}

/// <summary>
/// Global kill-feed on ManiaCanvas (top-center). Attach to ManiaCanvas and assign deathMessageText,
/// or leave empty to auto-create a bold TMP label on the main gameplay HUD.
/// </summary>
[DisallowMultipleComponent]
public class DeathMessageManager : MonoBehaviour
{
    public static DeathMessageManager Instance { get; private set; }

    [Header("UI")]
    public TMP_Text deathMessageText;

    [Header("Timing")]
    public float deathMessageDuration = 3f;
    public float fadeDuration = 0.6f;
    public float punchDuration = 0.25f;

    [Header("Style")]
    public float deathMessageFontSize = 34f;
    public Color predatorKillColor = new Color(1f, 0.42f, 0.12f, 1f);
    public Color survivorKillColor = new Color(0.35f, 0.85f, 1f, 1f);
    public Color hazardKillColor = new Color(0.85f, 0.45f, 1f, 1f);
    public Color genericKillColor = new Color(1f, 0.95f, 0.55f, 1f);

    [Header("Predator killed Survivor")]
    public string[] predatorKilledSurvivorMessages =
    {
        "{killer} turned {dead} into campfire seasoning!",
        "{dead} got deleted by {killer}!",
        "{killer} made {dead} reconsider their life choices.",
        "{dead} learned {killer} does not do hugs."
    };

    [Header("Survivor killed Predator")]
    public string[] survivorKilledPredatorMessages =
    {
        "{dead} got humbled by {killer}!",
        "{killer} hurt {dead}'s feelings!",
        "{dead} forgot survivors fight back.",
        "{killer} sent {dead} to timeout!"
    };

    [Header("Hazard / Lava")]
    public string[] hazardDeathMessages =
    {
        "{dead} forgot lava is hot!",
        "{dead} took a scenic trip through the hazard zone.",
        "{killer} and gravity teamed up on {dead}.",
        "{dead} discovered the map has teeth."
    };

    [Header("Generic fallback")]
    public string[] genericDeathMessages =
    {
        "{dead} has left the match.",
        "Something happened to {dead}. Nobody clapped.",
        "{dead} respawned in our hearts (not really)."
    };

    private Coroutine showRoutine;
    private Color messageBaseColor = Color.white;
    private RectTransform messageRect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        EnsureMessageText();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowDeathMessage(UnitHealth deadUnit, GameObject damageSource, DeathCause cause = DeathCause.Unknown)
    {
        if (deadUnit == null)
        {
            return;
        }

        if (cause == DeathCause.Unknown)
        {
            cause = InferCause(deadUnit, damageSource);
        }

        string deadName = GetUnitDisplayName(deadUnit);
        string killerName = GetSourceDisplayName(damageSource);
        string message = PickMessage(cause, deadName, killerName);

        Debug.Log("[DeathFeed] " + message);
        EnsureMessageText();

        if (deathMessageText == null)
        {
            return;
        }

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
        }

        showRoutine = StartCoroutine(ShowMessageRoutine(message, cause));
    }

    private IEnumerator ShowMessageRoutine(string message, DeathCause cause)
    {
        deathMessageText.text = message;
        deathMessageText.fontSize = deathMessageFontSize;
        deathMessageText.fontStyle = FontStyles.Bold;
        deathMessageText.gameObject.SetActive(true);

        messageBaseColor = GetColorForCause(cause);
        messageBaseColor.a = 1f;
        deathMessageText.color = messageBaseColor;

        if (messageRect == null && deathMessageText is TextMeshProUGUI)
        {
            messageRect = deathMessageText.rectTransform;
        }

        if (messageRect != null)
        {
            messageRect.localScale = Vector3.one;
            yield return PunchScaleRoutine(messageRect);
        }

        float hold = Mathf.Max(0.5f, deathMessageDuration - fadeDuration - punchDuration);
        yield return new WaitForSeconds(hold);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeDuration <= 0f ? 1f : elapsed / fadeDuration;
            Color c = messageBaseColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            deathMessageText.color = c;
            yield return null;
        }

        deathMessageText.text = string.Empty;
        deathMessageText.color = messageBaseColor;
        if (messageRect != null)
        {
            messageRect.localScale = Vector3.one;
        }

        showRoutine = null;
    }

    private IEnumerator PunchScaleRoutine(RectTransform rect)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, punchDuration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = 1f + 0.14f * Mathf.Sin(t * Mathf.PI);
            rect.localScale = Vector3.one * scale;
            yield return null;
        }

        rect.localScale = Vector3.one;
    }

    private Color GetColorForCause(DeathCause cause)
    {
        switch (cause)
        {
            case DeathCause.PredatorAttack:
                return predatorKillColor;
            case DeathCause.SurvivorAttack:
                return survivorKillColor;
            case DeathCause.Lava:
            case DeathCause.Hazard:
            case DeathCause.NPC:
            case DeathCause.Fall:
                return hazardKillColor;
            default:
                return genericKillColor;
        }
    }

    private void EnsureMessageText()
    {
        if (deathMessageText != null)
        {
            messageRect = deathMessageText.rectTransform;
            return;
        }

        ManiaGameUI ui = FindFirstObjectByType<ManiaGameUI>();
        if (ui != null && ui.deathFeedText != null)
        {
            deathMessageText = ui.deathFeedText;
            messageRect = deathMessageText.rectTransform;
            return;
        }

        Transform parent = null;
        if (ui != null && ui.gameplayHUD != null)
        {
            parent = ui.gameplayHUD.transform;
        }
        else
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                parent = canvas.transform;
            }
        }

        if (parent == null)
        {
            return;
        }

        GameObject host = new GameObject("GlobalDeathFeedText");
        host.transform.SetParent(parent, false);
        RectTransform rect = host.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.82f);
        rect.anchorMax = new Vector2(0.5f, 0.82f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1100f, 96f);
        rect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI tmp = host.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = deathMessageFontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = genericKillColor;
        tmp.text = string.Empty;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;

        deathMessageText = tmp;
        messageRect = rect;

        if (ui != null)
        {
            ui.deathFeedText = tmp;
        }
    }

    private DeathCause InferCause(UnitHealth deadUnit, GameObject damageSource)
    {
        if (damageSource == null)
        {
            return DeathCause.Unknown;
        }

        if (damageSource.GetComponentInParent<HellfirePitDamageZone>() != null
            || damageSource.GetComponentInParent<HellPitHazard>() != null
            || damageSource.name.ToLowerInvariant().Contains("hellfire")
            || damageSource.name.ToLowerInvariant().Contains("lava"))
        {
            return DeathCause.Lava;
        }

        if (damageSource.GetComponentInParent<VoidKillZone>() != null
            || damageSource.GetComponentInParent<TornadoHazard>() != null
            || damageSource.GetComponentInParent<WaterZone>() != null)
        {
            return DeathCause.Hazard;
        }

        if (damageSource.GetComponentInParent<NPCChaosCaster>() != null)
        {
            return DeathCause.NPC;
        }

        bool deadIsSurvivor = deadUnit.CompareTag("Survivor");
        bool deadIsPredator = deadUnit.CompareTag("Monster") || deadUnit.CompareTag("Predator");
        bool sourceIsSurvivor = damageSource.CompareTag("Survivor")
            || damageSource.GetComponentInParent<SurvivorClassManager>() != null;
        bool sourceIsPredator = damageSource.CompareTag("Monster")
            || damageSource.CompareTag("Predator")
            || damageSource.GetComponentInParent<PredatorClassManager>() != null;

        if (deadIsSurvivor && sourceIsPredator)
        {
            return DeathCause.PredatorAttack;
        }

        if (deadIsPredator && sourceIsSurvivor)
        {
            return DeathCause.SurvivorAttack;
        }

        return DeathCause.Unknown;
    }

    private string PickMessage(DeathCause cause, string deadName, string killerName)
    {
        string[] pool;
        switch (cause)
        {
            case DeathCause.PredatorAttack:
                pool = predatorKilledSurvivorMessages;
                break;
            case DeathCause.SurvivorAttack:
                pool = survivorKilledPredatorMessages;
                break;
            case DeathCause.Lava:
            case DeathCause.Hazard:
            case DeathCause.NPC:
            case DeathCause.Fall:
                pool = hazardDeathMessages;
                break;
            default:
                pool = genericDeathMessages;
                break;
        }

        if (pool == null || pool.Length == 0)
        {
            pool = genericDeathMessages;
        }

        string template = pool[Random.Range(0, pool.Length)];
        return template
            .Replace("{dead}", deadName)
            .Replace("{killer}", killerName);
    }

    private string GetUnitDisplayName(UnitHealth unit)
    {
        if (unit == null)
        {
            return "Someone";
        }

        PredatorClassManager predator = unit.GetComponent<PredatorClassManager>();
        if (predator != null)
        {
            return FormatPredatorClassName(predator.activeClass);
        }

        SurvivorClassManager survivor = unit.GetComponent<SurvivorClassManager>();
        if (survivor != null)
        {
            return FormatSurvivorClassName(survivor.activeClass);
        }

        string objectName = unit.gameObject.name;
        if (objectName.Contains("Survivor"))
        {
            return "Survivor";
        }

        if (objectName.Contains("Monster") || objectName.Contains("Predator"))
        {
            return "Predator";
        }

        return objectName;
    }

    private string GetSourceDisplayName(GameObject source)
    {
        if (source == null)
        {
            return "the map";
        }

        UnitHealth health = source.GetComponentInParent<UnitHealth>();
        if (health != null)
        {
            return GetUnitDisplayName(health);
        }

        if (source.GetComponentInParent<HellfirePitDamageZone>() != null
            || source.GetComponentInParent<HellPitHazard>() != null)
        {
            return "the lava";
        }

        if (source.GetComponentInParent<TornadoHazard>() != null)
        {
            return "a tornado";
        }

        if (source.GetComponentInParent<NPCChaosCaster>() != null)
        {
            return "a wild NPC";
        }

        return source.name;
    }

    private static string FormatPredatorClassName(PredatorClass predatorClass)
    {
        switch (predatorClass)
        {
            case PredatorClass.RelentlessHook: return "Relentless Hook";
            default: return predatorClass.ToString();
        }
    }

    private static string FormatSurvivorClassName(SurvivorClass survivorClass)
    {
        switch (survivorClass)
        {
            case SurvivorClass.Medic: return "Medic";
            case SurvivorClass.Warden: return "Warden";
            case SurvivorClass.Weaver: return "Weaver";
            default: return survivorClass.ToString();
        }
    }
}
