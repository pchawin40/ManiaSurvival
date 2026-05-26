using UnityEngine;

[DisallowMultipleComponent]
public class SoulwoodAvatarUIBridge : MonoBehaviour
{
    public SoulwoodAvatarController activeAvatar;
    public SoulwoodAvatarController nearbyAvatar;
    public SurvivorSoulwoodAvatarAbility survivorAbility;
    public ManiaGameUI gameUI;
    public ManiaGameManager gameManager;
    public LocalRoleController localRoleController;

    public bool HasActiveAvatar => activeAvatar != null;
    public bool HasNearbyAvatar => nearbyAvatar != null;

    private void Awake()
    {
        if (survivorAbility == null)
        {
            survivorAbility = FindFirstObjectByType<SurvivorSoulwoodAvatarAbility>();
        }

        if (gameUI == null)
        {
            gameUI = FindFirstObjectByType<ManiaGameUI>();
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<ManiaGameManager>();
        }

        if (localRoleController == null)
        {
            localRoleController = FindFirstObjectByType<LocalRoleController>();
        }
    }

    public void SetActiveAvatar(SoulwoodAvatarController avatar)
    {
        activeAvatar = avatar;
        UpdateEnterButton();
    }

    public void ClearActiveAvatar(SoulwoodAvatarController avatar)
    {
        if (activeAvatar == avatar)
        {
            activeAvatar = null;
        }

        UpdateEnterButton();
    }

    public void SetNearbyAvatar(SoulwoodAvatarController avatar)
    {
        nearbyAvatar = avatar;
        Debug.Log("Nearby Soulwood Avatar set: " + (avatar != null ? avatar.name : "null"));
        UpdateEnterButton();
    }

    public void ClearNearbyAvatar(SoulwoodAvatarController avatar)
    {
        if (nearbyAvatar == avatar)
        {
            nearbyAvatar = null;
        }

        UpdateEnterButton();
    }

    public void ResetActiveAvatarState()
    {
        nearbyAvatar = null;
        activeAvatar = null;
        UpdateEnterButton();
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
            activeAvatar.EjectPlayerOnly();
        }
    }

    public void EnterSoulwoodButton()
    {
        if (survivorAbility != null && nearbyAvatar != null)
        {
            survivorAbility.EnterAvatar(nearbyAvatar);
        }
    }

    private void UpdateEnterButton()
    {
        if (gameUI == null)
        {
            return;
        }

        bool isPlaying = gameManager != null && gameManager.State == ManiaGameState.Playing;
        bool isSurvivorMode = localRoleController != null && localRoleController.controlMode == PlayerControlMode.SurvivorControlled;
        bool shouldShow = isPlaying && isSurvivorMode && nearbyAvatar != null && activeAvatar == null;
        if (shouldShow)
        {
            gameUI.ShowEnterSoulwoodButton();
        }
        else
        {
            gameUI.HideEnterSoulwoodButton();
        }
    }
}
