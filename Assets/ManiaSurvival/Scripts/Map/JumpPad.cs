using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Launch pad that dashes units forward with a slight visual arc. CharacterController safe.
/// </summary>
[DisallowMultipleComponent]
public class JumpPad : MonoBehaviour
{
    [Header("Launch")]
    public float launchForwardForce = 7f;
    public float launchUpForce = 2.5f;
    public float launchDuration = 0.18f;

    [Header("Targets")]
    public bool affectsSurvivors = true;
    public bool affectsPredator = true;
    public float cooldownPerUnit = 1f;

    [Header("Debug")]
    public bool enableDebugLogs;

    private readonly Dictionary<int, float> nextUseTimeByUnit = new Dictionary<int, float>();

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || other.isTrigger)
        {
            return;
        }

        UnitHealth unit = other.GetComponentInParent<UnitHealth>();
        if (unit == null || unit.IsDead)
        {
            return;
        }

        if (!CanAffectUnit(unit))
        {
            return;
        }

        int unitId = unit.GetInstanceID();
        nextUseTimeByUnit.TryGetValue(unitId, out float nextUse);
        if (Time.time < nextUse)
        {
            return;
        }

        nextUseTimeByUnit[unitId] = Time.time + Mathf.Max(0.1f, cooldownPerUnit);
        Vector3 launchDir = transform.forward;
        launchDir.y = 0f;
        if (launchDir.sqrMagnitude <= 0.001f)
        {
            launchDir = unit.transform.forward;
            launchDir.y = 0f;
        }

        if (launchDir.sqrMagnitude <= 0.001f)
        {
            launchDir = Vector3.forward;
        }

        launchDir.Normalize();
        StartCoroutine(LaunchUnitRoutine(unit, launchDir));

        SpawnUsePulse();
        if (enableDebugLogs)
        {
            Debug.Log("[JumpPad] Launched " + unit.name);
        }
    }

    private bool CanAffectUnit(UnitHealth unit)
    {
        if (unit.CompareTag("Survivor"))
        {
            return affectsSurvivors;
        }

        if (unit.CompareTag("Monster") || unit.CompareTag("Predator"))
        {
            return affectsPredator;
        }

        return false;
    }

    private IEnumerator LaunchUnitRoutine(UnitHealth unit, Vector3 direction)
    {
        if (unit == null)
        {
            yield break;
        }

        CharacterController controller = unit.GetComponent<CharacterController>();
        SurvivorMovement survivorMove = unit.GetComponent<SurvivorMovement>();
        MonsterPlayerMovement predatorMove = unit.GetComponent<MonsterPlayerMovement>();
        OfflineSurvivorBotAI bot = unit.GetComponent<OfflineSurvivorBotAI>();

        bool controllerWasEnabled = controller != null && controller.enabled;
        bool survivorWasEnabled = survivorMove != null && survivorMove.enabled;
        bool botWasEnabled = bot != null && bot.enabled;

        if (survivorMove != null)
        {
            survivorMove.ApplyExternalMovementLock(launchDuration + 0.05f);
        }

        if (bot != null)
        {
            bot.enabled = false;
        }

        Vector3 start = unit.transform.position;
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, launchDuration);

        while (elapsed < duration && unit != null && !unit.IsDead)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float arc = Mathf.Sin(t * Mathf.PI) * launchUpForce;
            Vector3 flat = start + direction * (launchForwardForce * t);
            Vector3 target = flat;
            target.y = start.y + arc;

            if (ArenaBounds.Instance != null)
            {
                target = ArenaBounds.Instance.ClampPosition(target);
            }

            if (controller != null && controllerWasEnabled && controller.enabled)
            {
                Vector3 delta = target - unit.transform.position;
                controller.Move(delta);
            }
            else
            {
                unit.transform.position = target;
            }

            yield return null;
        }

        if (bot != null)
        {
            bot.enabled = botWasEnabled;
        }

        if (survivorMove != null)
        {
            survivorMove.enabled = survivorWasEnabled;
        }

        if (predatorMove != null)
        {
            predatorMove.ClearAbilitySpeedMultiplier();
        }
    }

    private void SpawnUsePulse()
    {
        GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pulse.name = "JumpPadPulse";
        Collider col = pulse.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        pulse.transform.position = transform.position + Vector3.up * 0.06f;
        pulse.transform.localScale = new Vector3(1.8f, 0.05f, 1.8f);
        Renderer renderer = pulse.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.25f, 0.95f, 1f, 0.55f);
            renderer.material = mat;
        }

        Destroy(pulse, 0.35f);
    }

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
        }

        col.isTrigger = true;
    }
}
