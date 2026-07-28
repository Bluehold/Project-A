using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MonsterAnimatorHook : MonsterAnimationHook
{
    [SerializeField]
    private Animator Animator;

    [Header("Animation State")]
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

    [Header("Transition")]
    [SerializeField]
    private float CrossFadeTime = 0.08f;

    [SerializeField]
    private int AnimationLayer = 0;

    [Header("One Shot Animation")]
    [SerializeField]
    private float OneShotExitOffset = 0.02f;

    [SerializeField]
    private MonsterAnimationType DefaultAnimation =
        MonsterAnimationType.Idle;

    private readonly Dictionary<MonsterAnimationType, string>
        AnimationNames = new();

    private MonsterAnimationType CurrentAnimation;

    private MonsterAnimationType RequestedLoopAnimation;

    private bool HasCurrentAnimation;

    private bool IsPlayingOneShot;

    private Coroutine OneShotRoutine;

    private void Reset()
    {
        Animator = GetComponent<Animator>()
            ?? GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (Animator == null)
        {
            Animator = GetComponent<Animator>()
                ?? GetComponentInChildren<Animator>();
        }

        if (Animator != null)
        {
            Animator.enabled = true;
            Animator.speed = 1f;
        }

        RequestedLoopAnimation = DefaultAnimation;
    }

    public override void Play(
        
        MonsterAnimationType animationType)
    {

        Debug.LogWarning(
                $"{animationType}"
            );
        if (Animator == null)
        {
            Debug.LogWarning(
                $"Animator is missing on {name}"
            );

            return;
        }

        if (Animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning(
                $"Animator Controller is missing on " +
                $"{Animator.name}"
            );

            return;
        }

        Animator.enabled = true;
        Animator.speed = 1f;

        if (IsLoopAnimation(animationType))
        {
            PlayLoopAnimation(animationType);
            return;
        }

        PlayOneShotAnimation(animationType);
    }

    private void PlayLoopAnimation(
        MonsterAnimationType animationType)
    {
        RequestedLoopAnimation = animationType;

        if (IsPlayingOneShot)
        {
            return;
        }

        if (HasCurrentAnimation &&
            CurrentAnimation == animationType)
        {
            return;
        }

        PlayState(animationType);

        CurrentAnimation = animationType;
        HasCurrentAnimation = true;
    }

    private void PlayOneShotAnimation(
        MonsterAnimationType animationType)
    {
        if (IsPlayingOneShot)
        {
            return;
        }

        if (OneShotRoutine != null)
        {
            StopCoroutine(OneShotRoutine);
        }

        OneShotRoutine = StartCoroutine(
            PlayOneShotRoutine(animationType)
        );
    }

    private IEnumerator PlayOneShotRoutine(
        MonsterAnimationType animationType)
    {
        IsPlayingOneShot = true;

        PlayState(animationType);

        CurrentAnimation = animationType;
        HasCurrentAnimation = true;

        yield return null;

        AnimatorStateInfo stateInfo =
            Animator.GetCurrentAnimatorStateInfo(
                AnimationLayer
            );

        float animationLength =
            stateInfo.length;

        if (animationLength <= 0f)
        {
            animationLength = 0.1f;
        }

        float waitTime =
            Mathf.Max(
                0f,
                animationLength - OneShotExitOffset
            );

        yield return new WaitForSeconds(waitTime);

        IsPlayingOneShot = false;

        PlayState(RequestedLoopAnimation);

        CurrentAnimation =
            RequestedLoopAnimation;

        HasCurrentAnimation = true;

        OneShotRoutine = null;
    }

    private void PlayState(
        MonsterAnimationType animationType)
    {
        string stateName =
            GetAnimationName(animationType);

        if (string.IsNullOrWhiteSpace(stateName))
        {
            Debug.LogWarning(
                $"Animation state name is empty: " +
                $"{animationType}"
            );

            return;
        }

        if (AnimationLayer < 0 ||
            AnimationLayer >= Animator.layerCount)
        {
            Debug.LogWarning(
                $"Invalid Animator layer: " +
                $"{AnimationLayer}"
            );

            return;
        }

        string fullStateName =
            GetFullStateName(stateName);

        int stateHash =
            Animator.StringToHash(
                fullStateName
            );

        if (!Animator.HasState(
                AnimationLayer,
                stateHash))
        {
            Debug.LogWarning(
                $"Animator state not found: " +
                $"{fullStateName}"
            );

            return;
        }

        Animator.CrossFade(
            stateHash,
            CrossFadeTime,
            AnimationLayer,
            0f
        );
    }

    private bool IsLoopAnimation(
        MonsterAnimationType animationType)
    {
        return animationType switch
        {
            MonsterAnimationType.Idle => true,
            MonsterAnimationType.Move => true,
            MonsterAnimationType.Run => true,
            _ => false
        };
    }

    public override void Stop()
    {
        if (OneShotRoutine != null)
        {
            StopCoroutine(
                OneShotRoutine
            );

            OneShotRoutine = null;
        }

        IsPlayingOneShot = false;

        RequestedLoopAnimation =
            DefaultAnimation;

        PlayState(DefaultAnimation);

        CurrentAnimation =
            DefaultAnimation;

        HasCurrentAnimation = true;
    }

    public override bool IsPlaying()
    {
        return Animator != null &&
               Animator.enabled &&
               Animator.runtimeAnimatorController != null &&
               Animator.speed > 0f;
    }

    public override void SetSpeed(
        float speed)
    {
        if (Animator != null)
        {
            Animator.speed =
                Mathf.Max(0f, speed);
        }
    }

    public override void SetAnimationName(
        MonsterAnimationType animationType,
        string animationName)
    {
        if (string.IsNullOrWhiteSpace(
                animationName))
        {
            return;
        }

        AnimationNames[
            animationType
        ] = animationName;

        HasCurrentAnimation = false;
    }

    public void SetAnimator(
        Animator animator)
    {
        Animator = animator;

        HasCurrentAnimation = false;

        IsPlayingOneShot = false;

        if (Animator != null)
        {
            Animator.enabled = true;
            Animator.speed = 1f;
        }
    }

    private string GetAnimationName(
        MonsterAnimationType animationType)
    {
        if (AnimationNames.TryGetValue(
                animationType,
                out string customName) &&
            !string.IsNullOrWhiteSpace(
                customName))
        {
            return customName;
        }

        return animationType switch
        {
            MonsterAnimationType.Idle =>
                IdleState,

            MonsterAnimationType.Move =>
                MoveState,

            MonsterAnimationType.Run =>
                RunState,

            MonsterAnimationType.Attack =>
                AttackState,

            MonsterAnimationType.Groggy =>
                GroggyState,

            MonsterAnimationType.Grab =>
                GrabState,

            MonsterAnimationType.Execute =>
                ExecuteState,

            MonsterAnimationType.KnockBack =>
                KnockBackState,

            MonsterAnimationType.KnockDown =>
                KnockDownState,

            MonsterAnimationType.Recover =>
                RecoverState,

            MonsterAnimationType.Die =>
                DieState,

            _ => IdleState
        };
    }

    private string GetFullStateName(
        string stateName)
    {
        string layerName =
            Animator.GetLayerName(
                AnimationLayer
            );

        return $"{layerName}.{stateName}";
    }
}