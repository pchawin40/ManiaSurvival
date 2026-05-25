using UnityEngine;
using UnityEngine.UI;

public class WorldHealthBar : MonoBehaviour
{
    public UnitHealth unitHealth;
    public Image fillImage;

    void Update()
    {
        if (unitHealth == null || fillImage == null) return;

        fillImage.fillAmount = unitHealth.GetHealthPercent();
    }
}