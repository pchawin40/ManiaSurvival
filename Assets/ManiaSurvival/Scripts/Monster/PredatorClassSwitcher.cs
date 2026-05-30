using UnityEngine;

/// <summary>
/// Editor/dev hotkeys to cycle the three playable predator prototype classes.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PredatorClassManager))]
public class PredatorClassSwitcher : MonoBehaviour
{
    [Header("Dev Hotkeys")]
    public bool enableHotkeys = true;
    public KeyCode relentlessHookKey = KeyCode.F1;
    public KeyCode swarmOverlordKey = KeyCode.F2;
    public KeyCode dragonJuggernautKey = KeyCode.F3;

    private PredatorClassManager classManager;

    private void Awake()
    {
        classManager = GetComponent<PredatorClassManager>();
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!enableHotkeys || classManager == null)
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
#endif
    }
}
