using UnityEngine;

[DisallowMultipleComponent]
public class TestMonster : BaseMonster
{
    [Header("Test Monster")]
    [SerializeField]
    private float DefaultMoveSpeed = 2.5f;

    [SerializeField]
    private float DetectionRange = 8f;

    [SerializeField]
    private float AttackRange = 1.8f;

    [SerializeField]
    private LayerMask TargetMask = -1;

    [Header("Animation")]
    [SerializeField]
    private RuntimeAnimatorController AnimatorController;

    [SerializeField]
    private string IdleAnimationName = "Idle";

    [SerializeField]
    private string WalkAnimationName = "Walk";

    [SerializeField]
    private string RunAnimationName = "Run";

    [SerializeField]
    private string AttackAnimationName = "Attack";

    private void Start()
    {
        EnsureDefaultSetup();
    }

    private void EnsureDefaultSetup()
    {
        if (GetComponent<MonsterAnimationHook>() == null)
        {
            gameObject.AddComponent<MonsterAnimatorHook>();
        }

        Animator animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }

        if (AnimatorController != null)
        {
            animator.runtimeAnimatorController = AnimatorController;
        }

        MonsterAnimatorHook animatorHookComponent = GetComponent<MonsterAnimatorHook>();
        if (animatorHookComponent != null)
        {
            if (GetComponent<BaseMonster>()?.MonsterModelTransform != null)
            {
                Animator modelAnimator = GetComponent<BaseMonster>().MonsterModelTransform.GetComponent<Animator>() ?? GetComponent<BaseMonster>().MonsterModelTransform.GetComponentInChildren<Animator>();
                if (modelAnimator != null)
                {
                    animator = modelAnimator;
                }
            }

            animatorHookComponent.SetAnimator(animator);
        }

        if (GetComponent<BaseMonsterAI>() == null)
        {
            gameObject.AddComponent<TestMonsterAI>();
        }

        if (GetComponent<BaseMonsterMovement>() == null)
        {
            gameObject.AddComponent<MonsterGroundMovement>();
        }

        SetMoveSpeed(DefaultMoveSpeed);

        TestMonsterAI monsterAi = GetComponent<TestMonsterAI>();
        if (monsterAi != null)
        {
            monsterAi.SetDetectionRange(DetectionRange);
            monsterAi.SetAttackRange(AttackRange);
            monsterAi.SetTargetMask(TargetMask);
        }

        GetComponent<BaseMonster>()?.SetMoveSpeed(DefaultMoveSpeed);

        MonsterAnimatorHook animatorHook = GetComponent<MonsterAnimatorHook>();
        if (animatorHook is MonsterAnimatorHook hook)
        {
            hook.SetAnimationName(MonsterAnimationType.Idle, IdleAnimationName);
            hook.SetAnimationName(MonsterAnimationType.Move, WalkAnimationName);
            hook.SetAnimationName(MonsterAnimationType.Run, RunAnimationName);
            hook.SetAnimationName(MonsterAnimationType.Attack, AttackAnimationName);
        }

        GetComponent<BaseMonster>()?.PlayAnimation(MonsterAnimationType.Idle);
    }
}
