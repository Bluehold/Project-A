//코드로 짜는것은 엄청난 복잡성에 비해 비효율적 비헤이비어트리 이용예정

using UnityEngine;
using UnityEngine.AI;

public class TestMonsterAI : BaseMonsterAI
{
    [Header("Detection")]
    [SerializeField]
    private float DetectionRange = 8f;

    [SerializeField]
    private float TargetLostRange = 14f;

    [SerializeField]
    private float AttackRange = 2f;

    [SerializeField]
    private LayerMask TargetMask = -1;

    [SerializeField]
    private bool OnlyTargetPlayer = true;

    [Header("Patrol")]
    [SerializeField]
    private float PatrolRadius = 8f;

    [SerializeField]
    private float PatrolMinDistanceFromSpawn = 2f;

    [SerializeField]
    private float PatrolReachedDistance = 0.6f;

    [SerializeField]
    private float PatrolIdleDelay = 2f;

    [Header("Chase")]
    [SerializeField]
    private float ChaseDestinationUpdateDistance = 0.5f;

    [Header("Combat Distance")]
    [SerializeField]
    private float PreferredCombatDistance = 1.7f;

    [SerializeField]
    private float TooCloseDistance = 1.1f;

    [SerializeField]
    private float ChaseDistance = 2.8f;

    [SerializeField]
    private float StrafeDistance = 2.0f;

    [SerializeField]
    private float RetreatDistance = 2.2f;

    [SerializeField]
    private float CombatDistanceTolerance = 0.25f;

    [Header("Combat Timing")]
    [SerializeField]
    private float MinimumDecisionTime = 0.5f;

    [SerializeField]
    private float MaximumDecisionTime = 1.2f;

    [SerializeField]
    private float AttackAnimationTime = 1f;

    [SerializeField]
    private float AttackRecoveryTime = 0.5f;

    [Header("Combat Probability")]
    [SerializeField]
    [Range(0f, 1f)]
    private float AttackChance = 0.45f;

    [SerializeField]
    [Range(0f, 1f)]
    private float StrafeChance = 0.35f;

    [SerializeField]
    [Range(0f, 1f)]
    private float WaitChance = 0.20f;

    [Header("Movement")]
    [SerializeField]
    private float StrafeAngle = 70f;

    private Transform Target;

    private Vector3 StartPosition;

    private Vector3 CurrentPatrolPoint;

    private Vector3 CombatMovePoint;

    private Vector3 LastChaseDestination;

    private float StateTimer;

    private float PatrolIdleTimer;

    private bool HasPatrolTarget;

    private bool IsPatrolIdle;

    private bool HasCombatMovePoint;

    private bool HasChaseDestination;

    private bool HasStoppedForCurrentAction;

    private AIState CurrentState;

    private CombatState CurrentCombatState;

    private CombatAction CurrentCombatAction;

    private enum AIState
    {
        Patrol,
        Chase,
        Combat
    }

    private enum CombatState
    {
        Decide,
        Acting,
        Attacking,
        Recovering
    }

    private enum CombatAction
    {
        Wait,
        Strafe,
        Attack,
        Retreat
    }

    public void SetDetectionRange(
        float range)
    {
        DetectionRange = range;
    }

    public void SetAttackRange(
        float range)
    {
        AttackRange = range;
    }

    public void SetTargetMask(
        LayerMask mask)
    {
        TargetMask = mask;
    }

    public override void Initialize(
        BaseMonster monster)
    {
        base.Initialize(monster);

        StartPosition =
            monster.transform.position;

        CurrentState =
            AIState.Patrol;

        CurrentCombatState =
            CombatState.Decide;
    }

    public override void Tick(
        BaseMonster monster,
        float deltaTime)
    {
        if (monster == null)
        {
            return;
        }

        UpdateTarget(monster);

        switch (CurrentState)
        {
            case AIState.Patrol:

                UpdatePatrol(
                    monster,
                    deltaTime
                );

                break;

            case AIState.Chase:

                UpdateChase(
                    monster
                );

                break;

            case AIState.Combat:

                UpdateCombat(
                    monster,
                    deltaTime
                );

                break;
        }
    }

    private void UpdateTarget(
        BaseMonster monster)
    {
        if (Target == null)
        {
            Target =
                FindNearestTarget();

            if (Target != null)
            {
                EnterChase(monster);
            }

            return;
        }

        float distance =
            GetTargetDistance(
                monster
            );

        if (distance >
            TargetLostRange)
        {
            Target = null;

            EnterPatrol(
                monster
            );

            return;
        }

        if (CurrentState ==
            AIState.Patrol)
        {
            EnterChase(
                monster
            );

            return;
        }

        if (CurrentState ==
                AIState.Combat &&
            distance >
                ChaseDistance)
        {
            EnterChase(
                monster
            );

            return;
        }

        if (CurrentState ==
                AIState.Chase &&
            distance <=
                ChaseDistance)
        {
            EnterCombat(
                monster
            );
        }
    }

    private void UpdatePatrol(
        BaseMonster monster,
        float deltaTime)
    {
        monster.SetRunAnimation(
            false
        );

        if (IsPatrolIdle)
        {
            PatrolIdleTimer +=
                deltaTime;

            if (PatrolIdleTimer >=
                PatrolIdleDelay)
            {
                PatrolIdleTimer = 0f;

                IsPatrolIdle =
                    false;

                HasPatrolTarget =
                    false;
            }

            return;
        }

        if (!HasPatrolTarget)
        {
            CurrentPatrolPoint =
                GetRandomPatrolPoint();

            monster.SetMoveDestination(
                CurrentPatrolPoint
            );

            HasPatrolTarget =
                true;

            return;
        }

        float distance =
            Vector3.Distance(
                monster.transform.position,
                CurrentPatrolPoint
            );

        if (distance <=
            PatrolReachedDistance)
        {
            monster.StopMovement();

            IsPatrolIdle = true;

            PatrolIdleTimer = 0f;
        }
    }

    private void UpdateChase(
        BaseMonster monster)
    {
        if (Target == null)
        {
            EnterPatrol(
                monster
            );

            return;
        }

        monster.SetRunAnimation(
            true
        );

        bool destinationChanged =
            !HasChaseDestination ||
            Vector3.SqrMagnitude(
                Target.position -
                LastChaseDestination
            ) >
            ChaseDestinationUpdateDistance *
            ChaseDestinationUpdateDistance;

        if (destinationChanged)
        {
            monster.SetMoveDestination(
                Target.position
            );

            LastChaseDestination =
                Target.position;

            HasChaseDestination =
                true;
        }
    }

    private void UpdateCombat(
        BaseMonster monster,
        float deltaTime)
    {
        if (Target == null)
        {
            EnterPatrol(
                monster
            );

            return;
        }

        FaceTarget(
            monster
        );

        float distance =
            GetTargetDistance(
                monster
            );

        if (CurrentCombatState ==
            CombatState.Attacking)
        {
            StateTimer -=
                deltaTime;

            if (StateTimer <= 0f)
            {
                CurrentCombatState =
                    CombatState.Recovering;

                StateTimer =
                    AttackRecoveryTime;
            }

            return;
        }

        if (CurrentCombatState ==
            CombatState.Recovering)
        {
            StateTimer -=
                deltaTime;

            if (StateTimer <= 0f)
            {
                CurrentCombatState =
                    CombatState.Decide;

                HasStoppedForCurrentAction =
                    false;
            }

            return;
        }

        if (CurrentCombatState ==
            CombatState.Acting)
        {
            UpdateCombatAction(
                monster,
                deltaTime
            );

            return;
        }

        if (distance <
            TooCloseDistance)
        {
            StartRetreat(
                monster
            );

            return;
        }

        if (distance >
            PreferredCombatDistance +
            CombatDistanceTolerance)
        {
            MoveToPreferredDistance(
                monster
            );

            return;
        }

        UpdateCombatDecision(
            monster
        );
    }

    private void UpdateCombatDecision(
        BaseMonster monster)
    {
        if (CurrentCombatState !=
            CombatState.Decide)
        {
            return;
        }

        SelectCombatAction(
            monster
        );
    }

    private void UpdateCombatAction(
        BaseMonster monster,
        float deltaTime)
    {
        StateTimer -=
            deltaTime;

        if (StateTimer <= 0f)
        {
            HasCombatMovePoint =
                false;

            HasStoppedForCurrentAction =
                false;

            CurrentCombatState =
                CombatState.Decide;
        }
    }

    private void SelectCombatAction(
        BaseMonster monster)
    {
        float randomValue =
            Random.value;

        float attackLimit =
            AttackChance;

        float strafeLimit =
            AttackChance +
            StrafeChance;

        if (randomValue <
            attackLimit)
        {
            StartAttack(
                monster
            );

            return;
        }

        if (randomValue <
            strafeLimit)
        {
            StartStrafe(
                monster
            );

            return;
        }

        StartWait(
            monster
        );
    }

    private void StartAttack(
        BaseMonster monster)
    {
        monster.StopMovement();

        monster.PlayAnimation(
            MonsterAnimationType.Attack
        );

        CurrentCombatAction =
            CombatAction.Attack;

        CurrentCombatState =
            CombatState.Attacking;

        StateTimer =
            AttackAnimationTime;

        Debug.Log(
            $"{monster.name} selected Attack"
        );
    }

    private void StartStrafe(
        BaseMonster monster)
    {
        Vector3 direction =
            Target.position -
            monster.transform.position;

        direction.y = 0f;

        Vector3 sideDirection =
            Quaternion.Euler(
                0f,
                Random.value < 0.5f
                    ? -StrafeAngle
                    : StrafeAngle,
                0f
            ) *
            direction.normalized;

        CombatMovePoint =
            monster.transform.position +
            sideDirection *
            StrafeDistance;

        if (NavMesh.SamplePosition(
                CombatMovePoint,
                out NavMeshHit hit,
                2f,
                NavMesh.AllAreas))
        {
            CombatMovePoint =
                hit.position;
        }

        monster.SetRunAnimation(
            false
        );

        monster.SetMoveDestination(
            CombatMovePoint
        );

        HasCombatMovePoint =
            true;

        CurrentCombatAction =
            CombatAction.Strafe;

        CurrentCombatState =
            CombatState.Acting;

        StateTimer =
            Random.Range(
                MinimumDecisionTime,
                MaximumDecisionTime
            );
    }

    private void StartWait(
        BaseMonster monster)
    {
        monster.StopMovement();

        HasStoppedForCurrentAction =
            true;

        CurrentCombatAction =
            CombatAction.Wait;

        CurrentCombatState =
            CombatState.Acting;

        StateTimer =
            Random.Range(
                MinimumDecisionTime,
                MaximumDecisionTime
            );
    }

    private void StartRetreat(
        BaseMonster monster)
    {
        Vector3 direction =
            monster.transform.position -
            Target.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <
            0.001f)
        {
            direction =
                -monster.transform.forward;
        }

        CombatMovePoint =
            monster.transform.position +
            direction.normalized *
            RetreatDistance;

        if (NavMesh.SamplePosition(
                CombatMovePoint,
                out NavMeshHit hit,
                2f,
                NavMesh.AllAreas))
        {
            CombatMovePoint =
                hit.position;
        }

        monster.SetRunAnimation(
            false
        );

        monster.SetMoveDestination(
            CombatMovePoint
        );

        HasCombatMovePoint =
            true;

        CurrentCombatAction =
            CombatAction.Retreat;

        CurrentCombatState =
            CombatState.Acting;

        StateTimer =
            Random.Range(
                0.4f,
                0.8f
            );
    }

    private void MoveToPreferredDistance(
        BaseMonster monster)
    {
        Vector3 direction =
            monster.transform.position -
            Target.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <
            0.001f)
        {
            direction =
                -monster.transform.forward;
        }

        Vector3 destination =
            Target.position +
            direction.normalized *
            PreferredCombatDistance;

        if (NavMesh.SamplePosition(
                destination,
                out NavMeshHit hit,
                2f,
                NavMesh.AllAreas))
        {
            destination =
                hit.position;
        }

        monster.SetRunAnimation(
            false
        );

        monster.SetMoveDestination(
            destination
        );
    }

    private float GetTargetDistance(
        BaseMonster monster)
    {
        Vector3 monsterPosition =
            monster.transform.position;

        Vector3 targetPosition =
            Target.position;

        monsterPosition.y = 0f;

        targetPosition.y = 0f;

        return Vector3.Distance(
            monsterPosition,
            targetPosition
        );
    }

    private void FaceTarget(
        BaseMonster monster)
    {
        Vector3 direction =
            Target.position -
            monster.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <
            0.001f)
        {
            return;
        }

        Quaternion rotation =
            Quaternion.LookRotation(
                direction.normalized
            );

        monster.transform.rotation =
            Quaternion.RotateTowards(
                monster.transform.rotation,
                rotation,
                720f *
                Time.deltaTime
            );
    }

    private void EnterPatrol(
        BaseMonster monster)
    {
        CurrentState =
            AIState.Patrol;

        CurrentCombatState =
            CombatState.Decide;

        HasPatrolTarget =
            false;

        HasCombatMovePoint =
            false;

        HasChaseDestination =
            false;

        IsPatrolIdle =
            false;

        PatrolIdleTimer =
            0f;

        StateTimer =
            0f;

        monster.SetRunAnimation(
            false
        );

        monster.StopMovement();
    }

    private void EnterChase(
        BaseMonster monster)
    {
        CurrentState =
            AIState.Chase;

        HasCombatMovePoint =
            false;

        HasChaseDestination =
            false;

        IsPatrolIdle =
            false;

        StateTimer =
            0f;

        monster.SetRunAnimation(
            true
        );
    }

    private void EnterCombat(
        BaseMonster monster)
    {
        CurrentState =
            AIState.Combat;

        CurrentCombatState =
            CombatState.Decide;

        HasCombatMovePoint =
            false;

        HasChaseDestination =
            false;

        StateTimer =
            0f;

        monster.SetRunAnimation(
            false
        );

        monster.StopMovement();
    }

    private Vector3 GetRandomPatrolPoint()
    {
        Vector2 randomCircle =
            Random.insideUnitCircle;

        Vector3 direction =
            new Vector3(
                randomCircle.x,
                0f,
                randomCircle.y
            );

        if (direction.sqrMagnitude <
            0.001f)
        {
            direction =
                Vector3.forward;
        }

        float distance =
            Random.Range(
                PatrolMinDistanceFromSpawn,
                PatrolRadius
            );

        Vector3 candidate =
            StartPosition +
            direction.normalized *
            distance;

        if (NavMesh.SamplePosition(
                candidate,
                out NavMeshHit hit,
                PatrolRadius,
                NavMesh.AllAreas))
        {
            return hit.position;
        }

        return StartPosition;
    }

    private Transform FindNearestTarget()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                DetectionRange,
                TargetMask
            );

        Transform nearest =
            null;

        float nearestDistance =
            float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            if (hit.transform ==
                transform)
            {
                continue;
            }

            if (OnlyTargetPlayer &&
                hit.GetComponentInParent<
                    BasePlayer>() == null)
            {
                continue;
            }

            float distance =
                Vector3.Distance(
                    transform.position,
                    hit.transform.position
                );

            if (distance <
                nearestDistance)
            {
                nearestDistance =
                    distance;

                nearest =
                    hit.transform;
            }
        }

        return nearest;
    }

    public override void Stop()
    {
        Target = null;

        CurrentState =
            AIState.Patrol;

        CurrentCombatState =
            CombatState.Decide;

        HasPatrolTarget =
            false;

        HasCombatMovePoint =
            false;

        HasChaseDestination =
            false;

        IsPatrolIdle =
            false;

        PatrolIdleTimer =
            0f;

        StateTimer =
            0f;
    }
}