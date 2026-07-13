using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MonsterAnimatorHook : MonsterAnimationHook
{
    [SerializeField]
    private Animator Animator;

    [SerializeField]
    private string IdleState = "Idle";

    [SerializeField]
    private string MoveState = "Move";

    [SerializeField]
    private string RunState = "Run";

    [SerializeField]
    private string AttackState = "Attack";

    [SerializeField]
    private string GroggyState = "Groggy";

    [SerializeField]
    private string GrabState = "Grab";

    [SerializeField]
    private string ExecuteState = "Execute";

    [SerializeField]
    private string KnockBackState = "KnockBack";

    [SerializeField]
    private string KnockDownState = "KnockDown";

    [SerializeField]
    private string RecoverState = "Recover";

    [SerializeField]
    private string DieState = "Die";

    private readonly System.Collections.Generic.Dictionary<MonsterAnimationType, string> AnimationNames = new();
    private MonsterAnimationType LastRequestedAnimationType = MonsterAnimationType.Idle;
    private string LastRequestedStateName = string.Empty;
    private Coroutine PendingAnimationRoutine;
    private bool IsInitialized;

    private void Reset()
    {
        Animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (Animator == null)
        {
            Animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        }

        if (Animator != null && Animator.runtimeAnimatorController != null)
        {
            IsInitialized = true;
        }
    }

    public override void Play(MonsterAnimationType animationType)
    {
        string stateName = GetAnimationName(animationType);

        if (string.IsNullOrEmpty(stateName))
        {
            return;
        }

        Animator targetAnimator = ResolveAnimator(stateName);
        if (targetAnimator == null)
        {
            Debug.LogWarning($"No Animator found for animation state: {stateName} on {name}");
            return;
        }

        Debug.Log($"Trying play animation: {stateName} ({animationType}) on {name} via {targetAnimator.transform.name}");

        if (!HasState(targetAnimator, stateName))
        {
            Debug.LogWarning($"Animator state not found: {stateName} on {name} via {targetAnimator.transform.name}");
            return;
        }

        if (!IsInitialized && targetAnimator != null)
        {
            targetAnimator.Rebind();
            targetAnimator.Update(0f);
            IsInitialized = true;
        }

        if (LastRequestedAnimationType == animationType && LastRequestedStateName == stateName)
        {
            return;
        }

        LastRequestedAnimationType = animationType;
        LastRequestedStateName = stateName;

        if (PendingAnimationRoutine != null)
        {
            StopCoroutine(PendingAnimationRoutine);
        }

        PendingAnimationRoutine = StartCoroutine(PlayStateAfterFrame(targetAnimator, stateName));
        Animator = targetAnimator;
        Debug.Log($"Queued animation: {stateName} on {targetAnimator.transform.name}");
    }

    private IEnumerator PlayStateAfterFrame(Animator targetAnimator, string stateName)
    {
        yield return null;

        if (targetAnimator == null)
        {
            yield break;
        }

        targetAnimator.speed = 1f;
        targetAnimator.Play(stateName, 0, 0f);
        targetAnimator.Update(0f);
        targetAnimator.Rebind();
        targetAnimator.Update(0f);
        PendingAnimationRoutine = null;
        Debug.Log($"Animation played: {stateName} on {targetAnimator.transform.name}");
    }

    public override void Stop()
    {
        if (Animator != null)
        {
            Animator.speed = 0f;
        }
    }

    public override bool IsPlaying()
    {
        return Animator != null && Animator.runtimeAnimatorController != null;
    }

    public override void SetSpeed(float speed)
    {
        if (Animator != null)
        {
            Animator.speed = speed;
        }
    }

    public override void SetAnimationName(MonsterAnimationType animationType, string animationName)
    {
        if (string.IsNullOrWhiteSpace(animationName))
        {
            return;
        }

        AnimationNames[animationType] = animationName;
    }

    private string GetAnimationName(MonsterAnimationType animationType)
    {
        if (AnimationNames.TryGetValue(animationType, out string customName) && !string.IsNullOrWhiteSpace(customName))
        {
            return customName;
        }

        return animationType switch
        {
            MonsterAnimationType.Idle => IdleState,
            MonsterAnimationType.Move => MoveState,
            MonsterAnimationType.Run => RunState,
            MonsterAnimationType.Attack => AttackState,
            MonsterAnimationType.Groggy => GroggyState,
            MonsterAnimationType.Grab => GrabState,
            MonsterAnimationType.Execute => ExecuteState,
            MonsterAnimationType.KnockBack => KnockBackState,
            MonsterAnimationType.KnockDown => KnockDownState,
            MonsterAnimationType.Recover => RecoverState,
            MonsterAnimationType.Die => DieState,
            _ => IdleState
        };
    }

    public void SetAnimator(Animator animator)
    {
        Animator = animator;
    }

    private Animator ResolveAnimator(string stateName)
    {
        if (Animator != null && HasState(Animator, stateName))
        {
            return Animator;
        }

        Animator[] animators = GetComponentsInChildren<Animator>(true);
        foreach (Animator candidate in animators)
        {
            if (candidate == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(stateName) && HasState(candidate, stateName))
            {
                Animator = candidate;
                return candidate;
            }
        }

        if (Animator != null)
        {
            return Animator;
        }

        foreach (Animator candidate in animators)
        {
            if (candidate != null && candidate.runtimeAnimatorController != null)
            {
                Animator = candidate;
                return candidate;
            }
        }

        if (animators.Length > 0)
        {
            Animator = animators[0];
            return Animator;
        }

        return null;
    }

    private bool HasState(string stateName)
    {
        return HasState(Animator, stateName);
    }

    private bool HasState(Animator targetAnimator, string stateName)
    {
        if (targetAnimator == null || targetAnimator.runtimeAnimatorController == null)
        {
            return false;
        }

        var controller = targetAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
        if (controller == null)
        {
            return !string.IsNullOrEmpty(stateName);
        }

        foreach (var layer in controller.layers)
        {
            foreach (var state in layer.stateMachine.states)
            {
                if (state.state.name == stateName)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
