using UnityEngine;

/// <summary>
/// Editor/dev hotkeys to switch between the six playable predator prototype classes.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PredatorClassManager))]
public class PredatorClassSwitcher : MonoBehaviour
{
    [Header("Dev Hotkeys")]
    public bool enablePredatorClassDebugHotkeys = true;
    [Tooltip("Legacy alias for enablePredatorClassDebugHotkeys.")]
    public bool enableHotkeys = true;
    public KeyCode relentlessHookKey = KeyCode.F1;
    public KeyCode swarmOverlordKey = KeyCode.F2;
    public KeyCode dragonJuggernautKey = KeyCode.F3;
    public KeyCode shadowStalkerKey = KeyCode.F4;
    public KeyCode ironColossusKey = KeyCode.F5;
    public KeyCode plagueGardenerKey = KeyCode.F6;

    private PredatorClassManager classManager;

    private void Awake()
    {
        classManager = GetComponent<PredatorClassManager>();
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!AreHotkeysEnabled() || classManager == null)
        {
            return;
        }

        if (Input.GetKeyDown(relentlessHookKey))
        {
            classManager.SetPredatorClass(PredatorClass.RelentlessHook);
        }
        else if (Input.GetKeyDown(swarmOverlordKey))
        {
            classManager.SetPredatorClass(PredatorClass.SwarmOverlord);
        }
        else if (Input.GetKeyDown(dragonJuggernautKey))
        {
            classManager.SetPredatorClass(PredatorClass.Juggernaut);
        }
        else if (Input.GetKeyDown(shadowStalkerKey))
        {
            classManager.SetPredatorClass(PredatorClass.ShadowStalker);
        }
        else if (Input.GetKeyDown(ironColossusKey))
        {
            classManager.SetPredatorClass(PredatorClass.IronColossus);
        }
        else if (Input.GetKeyDown(plagueGardenerKey))
        {
            classManager.SetPredatorClass(PredatorClass.PlagueGardener);
        }
#endif
    }

    private bool AreHotkeysEnabled()
    {
        return enablePredatorClassDebugHotkeys && enableHotkeys;
    }
}
