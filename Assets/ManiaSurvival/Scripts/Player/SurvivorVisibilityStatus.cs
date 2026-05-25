using System.Collections;
using UnityEngine;

public class SurvivorVisibilityStatus : MonoBehaviour
{
    [Header("Visibility")]
    public bool IsHiddenFromMonster => hiddenCount > 0;

    private int hiddenCount;

    public void HideFromMonster(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        hiddenCount++;
        StartCoroutine(HideForDuration(duration));
    }

    public void SetHiddenFromMonster(bool hidden)
    {
        if (hidden)
        {
            hiddenCount++;
            return;
        }

        hiddenCount = Mathf.Max(0, hiddenCount - 1);
    }

    private IEnumerator HideForDuration(float duration)
    {
        yield return new WaitForSeconds(duration);

        hiddenCount = Mathf.Max(0, hiddenCount - 1);
    }
}
