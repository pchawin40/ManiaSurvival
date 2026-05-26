using UnityEngine;

[DisallowMultipleComponent]
public class SoulwoodAvatarUIBridge : MonoBehaviour
{
    public SoulwoodAvatarController activeAvatar;
    public ManiaGameUI gameUI;

    public bool HasActiveAvatar => activeAvatar != null;

    private void Awake()
    {
        if (gameUI == null)
        {
            gameUI = FindFirstObjectByType<ManiaGameUI>();
        }
    }

    public void SetActiveAvatar(SoulwoodAvatarController avatar)
    {
        activeAvatar = avatar;
        if (gameUI != null)
        {
            gameUI.ShowAvatarPanel();
        }
    }

    public void ClearActiveAvatar(SoulwoodAvatarController avatar)
    {
        if (activeAvatar == avatar)
        {
            activeAvatar = null;
            if (gameUI != null)
            {
                gameUI.ShowSurvivorPanel();
            }
        }
    }

    public void AvatarAttackButton()
    {
        if (activeAvatar != null)
        {
            activeAvatar.TryAttack();
        }
    }

    public void AvatarEjectButton()
    {
        if (activeAvatar != null)
        {
            activeAvatar.Eject();
        }
    }
}
