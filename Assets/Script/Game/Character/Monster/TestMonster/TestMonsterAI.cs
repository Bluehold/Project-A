using UnityEngine;

public class TestMonsterAI : BaseMonsterAI
{
    [Header("Behavior")]
    [SerializeField]
    private float DetectionRange = 8f;

    [SerializeField]
    private float PatrolRadius = 8f;

    [SerializeField]
    private float PatrolMinDistanceFromSpawn = 2f;

    [SerializeField]
    private float PatrolReachedDistance = 0.5f;

    [SerializeField]
    private float AttackRange = 2f;

    [SerializeField]
    private float AttackCooldown = 1.2f;

    [SerializeField]
    private float AttackWindupTime = 0.35f;

    [SerializeField]
    private float AttackRecoveryTime = 0.55f;

    [SerializeField]
    private float PatrolIdleDelay = 2f;

    [SerializeField]
    private LayerMask TargetMask = -1;

    [SerializeField]
    private bool OnlyTargetPlayer = true;

    private Transform Target;
    private float AttackTimer;
    private float PatrolIdleTimer;
    private float AttackSequenceTimer;
    private Vector3 StartPosition;
    private Vector3 CurrentPatrolPoint;
    private bool HasPatrolTarget;
    private bool IsPatrolIdle;
    private bool IsAttacking;
    private AIState CurrentState = AIState.Idle;
    private AttackSequenceState CurrentAttackSequence = AttackSequenceState.Ready;

    private AIState CurrentBehaviorState => CurrentState;

    private enum AIState
    {
        Idle,
        Chase,
        Attack,
        Patrol
    }

    private enum AttackSequenceState
    {
        Ready,
        Windup,
        Recovering
    }

    public void SetDetectionRange(float range)
    {
        DetectionRange = range;
    }

    public void SetAttackRange(float range)
    {
        AttackRange = range;
    }

    public void SetTargetMask(LayerMask mask)
    {
        TargetMask = mask;
    }

    public override void Initialize(BaseMonster monster)
    {
        base.Initialize(monster);
        StartPosition = monster.transform.position;
    }

    public override void Tick(BaseMonster monster, float deltaTime)
    {
        if (monster == null)
        {
            return;
        }

        AttackTimer += deltaTime;

        if (Target == null)
        {
            Target = FindNearestTarget();
        }

        if (Target == null)
        {
            CurrentAttackSequence = AttackSequenceState.Ready;
            IsAttacking = false;
            AttackSequenceTimer = 0f;
            monster.SetRunAnimation(false);
            RunPatrol(monster, deltaTime);
            return;
        }

        float distance = Vector3.Distance(monster.transform.position, Target.position);

        if (distance <= AttackRange)
        {
            CurrentState = AIState.Attack;
            monster.SetRunAnimation(false);
            monster.StopMovement();

            if (CurrentAttackSequence == AttackSequenceState.Ready)
            {
                CurrentAttackSequence = AttackSequenceState.Windup;
                AttackSequenceTimer = AttackWindupTime;
                IsAttacking = true;
            }

            if (CurrentAttackSequence == AttackSequenceState.Windup)
            {
                AttackSequenceTimer -= deltaTime;
                if (AttackSequenceTimer <= 0f)
                {
                    PerformAttack(monster);
                    CurrentAttackSequence = AttackSequenceState.Recovering;
                    AttackSequenceTimer = AttackRecoveryTime;
                }

                return;
            }

            if (CurrentAttackSequence == AttackSequenceState.Recovering)
            {
                AttackSequenceTimer -= deltaTime;
                if (AttackSequenceTimer <= 0f)
                {
                    CurrentAttackSequence = AttackSequenceState.Ready;
                    IsAttacking = false;
                }

                return;
            }

            return;
        }

        if (distance <= DetectionRange)
        {
            CurrentState = AIState.Chase;
            monster.SetRunAnimation(true);
            monster.SetMoveDestination(Target.position);
            return;
        }

        CurrentState = AIState.Patrol;
        CurrentAttackSequence = AttackSequenceState.Ready;
        IsAttacking = false;
        AttackSequenceTimer = 0f;
        monster.SetRunAnimation(false);
        RunPatrol(monster, deltaTime);
    }

    public override void Stop()
    {
        CurrentState = AIState.Idle;
        Target = null;
        AttackTimer = 0f;
        PatrolIdleTimer = 0f;
        AttackSequenceTimer = 0f;
        IsPatrolIdle = false;
        HasPatrolTarget = false;
        IsAttacking = false;
        CurrentAttackSequence = AttackSequenceState.Ready;
    }

    private void RunPatrol(BaseMonster monster, float deltaTime)
    {
        if (IsPatrolIdle)
        {
            PatrolIdleTimer += deltaTime;
            monster.StopMovement();

            if (PatrolIdleTimer >= PatrolIdleDelay)
            {
                IsPatrolIdle = false;
                PatrolIdleTimer = 0f;
                HasPatrolTarget = false;
            }

            return;
        }

        if (!HasPatrolTarget)
        {
            CurrentPatrolPoint = GetRandomPatrolPoint(monster.transform.position);
            monster.SetMoveDestination(CurrentPatrolPoint);
            HasPatrolTarget = true;
            return;
        }

        if (Vector3.Distance(monster.transform.position, CurrentPatrolPoint) <= PatrolReachedDistance)
        {
            IsPatrolIdle = true;
            PatrolIdleTimer = 0f;
            monster.StopMovement();
        }
    }

    private Vector3 GetRandomPatrolPoint(Vector3 currentPosition)
    {
        Vector3 candidate = StartPosition + Random.insideUnitSphere * PatrolRadius;
        candidate.y = currentPosition.y;

        if (Vector3.Distance(candidate, StartPosition) < PatrolMinDistanceFromSpawn)
        {
            candidate = StartPosition + (candidate - StartPosition).normalized * PatrolMinDistanceFromSpawn;
        }

        return candidate;
    }

    private Transform FindNearestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, DetectionRange, TargetMask);

        Transform nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (hit.transform == transform)
            {
                continue;
            }

            if (OnlyTargetPlayer && hit.GetComponentInParent<BasePlayer>() == null)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, hit.transform.position);
            if (distance < nearestDistance)
            {
                nearest = hit.transform;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private void PerformAttack(BaseMonster monster)
    {
        Debug.Log($"{monster.name} attacks target: {Target?.name}");
        monster.PlayAnimation(MonsterAnimationType.Attack);
    }
}
