using UnityEngine;

/// <summary>
/// Safe prototype animation trigger helpers. Missing Animator/triggers never throw.
/// Survivor controller uses Attack for Primary. Predator uses MeleeAttack + Ability1-3.
/// </summary>
public static class UnitAnimationHelper
{
    public static class Survivor
    {
        public static readonly int Primary = Animator.StringToHash("Attack");
        public static readonly int Ability2 = Animator.StringToHash("Ability2");
        public static readonly int Ability3 = Animator.StringToHash("Ability3");
        public static readonly int Ultimate = Animator.StringToHash("Ultimate");
        public static readonly int Hit = Animator.StringToHash("Hit");
        public static readonly int Death = Animator.StringToHash("Death");
        public static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
    }

    public static class Predator
    {
        public static readonly int Primary = Animator.StringToHash("MeleeAttack");
        public static readonly int Ability2 = Animator.StringToHash("Ability1");
        public static readonly int Ability3 = Animator.StringToHash("Ability2");
        public static readonly int Ultimate = Animator.StringToHash("Ultimate");
        public static readonly int Hit = Animator.StringToHash("Hit");
        public static readonly int Death = Animator.StringToHash("Death");
        public static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
    }

    public static bool TrySetAnimatorTrigger(
        Animator animator,
        int triggerHash,
        string triggerLabel = null,
        bool logWhenMissing = false,
        Object logContext = null)
    {
        if (animator == null || !animator.isActiveAndEnabled)
        {
            if (logWhenMissing)
            {
                Debug.LogWarning("[Anim] Missing/inactive Animator for trigger "
                    + (triggerLabel ?? triggerHash.ToString())
                    + (logContext != null ? " on " + logContext.name : string.Empty));
            }

            return false;
        }

        if (!animator.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (!HasTriggerParameter(animator, triggerHash))
        {
            if (logWhenMissing)
            {
                Debug.LogWarning("[Anim] Trigger not found: "
                    + (triggerLabel ?? triggerHash.ToString())
                    + " on " + animator.gameObject.name);
            }

            return false;
        }

        animator.SetTrigger(triggerHash);

        if (logWhenMissing)
        {
            Debug.Log("[Anim] Trigger " + (triggerLabel ?? triggerHash.ToString())
                + " on " + animator.gameObject.name);
        }

        return true;
    }

    public static bool TrySetAnimatorFloat(
        Animator animator,
        int floatHash,
        float value,
        bool logWhenMissing = false)
    {
        if (animator == null || !animator.isActiveAndEnabled || !animator.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (!HasFloatParameter(animator, floatHash))
        {
            return false;
        }

        animator.SetFloat(floatHash, value);
        return true;
    }

    private static bool HasTriggerParameter(Animator animator, int hash)
    {
        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Trigger
                && parameters[i].nameHash == hash)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasFloatParameter(Animator animator, int hash)
    {
        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Float
                && parameters[i].nameHash == hash)
            {
                return true;
            }
        }

        return false;
    }
}
