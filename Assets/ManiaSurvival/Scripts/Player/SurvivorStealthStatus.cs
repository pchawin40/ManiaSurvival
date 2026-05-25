using System.Collections;
using UnityEngine;

public class SurvivorStealthStatus : MonoBehaviour
{
    [Header("Stealth")]
    public bool IsInvisible { get; private set; }

    private Coroutine invisibilityRoutine;

    public void SetInvisible(bool isInvisible)
    {
        IsInvisible = isInvisible;
    }

    public void ApplyInvisibility(float duration)
    {
        if (invisibilityRoutine != null)
        {
            StopCoroutine(invisibilityRoutine);
        }

        invisibilityRoutine = StartCoroutine(InvisibilityRoutine(duration));
    }

    private IEnumerator InvisibilityRoutine(float duration)
    {
        IsInvisible = true;

        yield return new WaitForSeconds(Mathf.Max(0f, duration));

        IsInvisible = false;
        invisibilityRoutine = null;
    }
}
