//ToDo: 방향 회전 추가, 속도감지 기반의 애미네이션 세팅, Move To 함수의 개선, 이동 잠금

using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class MonsterGroundMovement : BaseMonsterMovement
{
    [Header("Movement")]
    [SerializeField]
    private float MoveSpeed = 3f;

    [SerializeField]
    private float StopDistance = 0.15f;

    [SerializeField]
    private float TurnSpeed = 720f;

    [SerializeField]
    private float DestinationUpdateDistance = 0.4f;

    [SerializeField]
    private float Acceleration = 20f;

    [SerializeField]
    private float Deceleration = 24f;

    [SerializeField]
    private float MinSpeedForAnimation = 0.15f;

    [Header("Animation")]
    [SerializeField]
    private float MovementStartSpeed = 0.05f;

    [SerializeField]
    private float RunIdleDelay = 0.6f;

    [SerializeField]
    private float WalkIdleDelayMin = 4f;

    [SerializeField]
    private float WalkIdleDelayMax = 8f;

    private NavMeshAgent Agent;

    private MonsterAnimationType CurrentAnimation;

    private bool HasCurrentAnimation;

    private bool HasMoveCommand;

    private bool IsMovementLocked;

    private bool IsMovementStopping;

    private Vector3 LastDestination;

    private bool HasLastDestination;

    private float StopTimer;

    private float StopDelay;

    private GroundMovementMode CurrentMovementMode = GroundMovementMode.Walk;

    private GroundMovePhase CurrentMovePhase = GroundMovePhase.Idle;

    private float DesiredSpeed;

    private float CurrentSpeed;

    private float RequestedStopDistance;

    private bool HasPendingBrake;

    private float BrakeTimer;

    private float BrakeDuration;

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();

        Agent.updateRotation = true;
        Agent.updateUpAxis = true;
        Agent.autoBraking = false;
    }

    public override void Initialize(BaseMonster monster)
    {
        base.Initialize(monster);

        MoveSpeed = monster.MoveSpeedValue;
        DesiredSpeed = MoveSpeed;
        CurrentSpeed = 0f;

        Agent.speed = 0f;
        Agent.stoppingDistance = StopDistance;
        Agent.angularSpeed = TurnSpeed;
        Agent.acceleration = Acceleration;

        Agent.isStopped = true;

        HasMoveCommand = false;
        IsMovementStopping = false;
        HasPendingBrake = false;
        HasLastDestination = false;
        StopTimer = 0f;
        StopDelay = 0f;
        RequestedStopDistance = StopDistance;
        BrakeTimer = 0f;
        BrakeDuration = 0f;

        HasCurrentAnimation = false;
        SetPhase(GroundMovePhase.Idle);
    }

    public override void Tick(float deltaTime)
    {
        if (Monster == null)
        {
            return;
        }

        if (Monster.CurrentState !=
            MonsterState.Normal)
        {
            StopAgentOnly();
            return;
        }

        if (Agent == null ||
            !Agent.isOnNavMesh)
        {
            return;
        }

        if (IsMovementLocked)
        {
            SetPhase(GroundMovePhase.Locked);
            return;
        }

        if (HasPendingBrake)
        {
            BrakeTimer += deltaTime;
            CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, 0f, Deceleration * deltaTime);
            Agent.speed = CurrentSpeed;
            SetPhase(GroundMovePhase.Braking);

            if (CurrentSpeed <= 0.001f || BrakeTimer >= BrakeDuration)
            {
                HasPendingBrake = false;
                HasMoveCommand = false;
                HasLastDestination = false;
                Agent.isStopped = true;
                Agent.ResetPath();
                CurrentSpeed = 0f;
                Agent.speed = 0f;
                SetPhase(GroundMovePhase.Idle);
            }

            return;
        }

        if (!HasMoveCommand)
        {
            if (CurrentSpeed > 0f)
            {
                CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, 0f, Deceleration * deltaTime);
                Agent.speed = CurrentSpeed;
                SetPhase(GroundMovePhase.Braking);
                return;
            }

            if (CurrentMovePhase != GroundMovePhase.Idle)
            {
                SetPhase(GroundMovePhase.Idle);
            }

            return;
        }

        bool isActuallyMoving = Agent.velocity.sqrMagnitude > MovementStartSpeed * MovementStartSpeed;
        bool hasRemainingPath = Agent.hasPath && !Agent.pathPending && Agent.remainingDistance > RequestedStopDistance + 0.03f;

        if (hasRemainingPath || isActuallyMoving)
        {
            float accelRate = CurrentSpeed < DesiredSpeed ? Acceleration : Deceleration;
            CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, DesiredSpeed, accelRate * deltaTime);
            Agent.speed = CurrentSpeed;
            Agent.isStopped = false;
            SetPhase(CurrentSpeed < DesiredSpeed * 0.5f ? GroundMovePhase.Accelerating : GroundMovePhase.Moving);
            UpdateMovementAnimation();
            return;
        }

        if (!IsMovementStopping)
        {
            IsMovementStopping = true;
            StopTimer = 0f;
            StopDelay = CurrentMovementMode == GroundMovementMode.Run
                ? RunIdleDelay
                : Random.Range(WalkIdleDelayMin, WalkIdleDelayMax);
        }

        StopTimer += deltaTime;

        if (StopTimer >= StopDelay)
        {
            HasMoveCommand = false;
            HasLastDestination = false;
            IsMovementStopping = false;
            StopTimer = 0f;
            StopDelay = 0f;
            CurrentSpeed = 0f;
            Agent.speed = 0f;
            Agent.isStopped = true;
            SetPhase(GroundMovePhase.Idle);
            UpdateMovementAnimation();
            return;
        }

        CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, 0f, Deceleration * deltaTime);
        Agent.speed = CurrentSpeed;
        SetPhase(GroundMovePhase.Braking);
        UpdateMovementAnimation();
    }

    public override void MoveTo(Vector3 destination, float speedScale = 1f)
    {
        MoveTo(destination, speedScale, StopDistance, true);
    }

    public override void MoveTo(Vector3 destination, float speedScale, float stopDistanceOverride, bool useBrake)
    {
        if (Monster == null || Agent == null || !Agent.isOnNavMesh)
        {
            return;
        }

        if (IsMovementLocked)
        {
            return;
        }

        float targetSpeed = Mathf.Max(0.01f, Monster.MoveSpeedValue * speedScale);
        MoveSpeed = targetSpeed;
        DesiredSpeed = targetSpeed;
        RequestedStopDistance = stopDistanceOverride > 0f ? stopDistanceOverride : StopDistance;
        HasPendingBrake = false;
        BrakeTimer = 0f;
        BrakeDuration = 0f;
        HasMoveCommand = true;
        IsMovementStopping = false;
        StopTimer = 0f;
        StopDelay = 0f;

        bool destinationChanged = !HasLastDestination || Vector3.SqrMagnitude(destination - LastDestination) > DestinationUpdateDistance * DestinationUpdateDistance;

        if (destinationChanged)
        {
            Agent.isStopped = false;
            Agent.SetDestination(destination);
            LastDestination = destination;
            HasLastDestination = true;
        }

        CurrentSpeed = Mathf.Max(CurrentSpeed, 0.05f);
        Agent.speed = CurrentSpeed;
        SetPhase(GroundMovePhase.Accelerating);
        UpdateMovementAnimation();
    }

    public override void SetMovementMode(GroundMovementMode mode)
    {
        CurrentMovementMode = mode;
        UpdateMovementAnimation();
    }

    public override void MoveToMaintainDistance(Vector3 targetPosition, float desiredDistance, float speedScale = 1f)
    {
        if (Monster == null || Agent == null || !Agent.isOnNavMesh)
        {
            return;
        }

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = transform.forward;
        }

        Vector3 destination = targetPosition - direction.normalized * desiredDistance;
        MoveTo(destination, speedScale, 0.05f, true);
    }

    public override void MoveAwayFrom(Vector3 targetPosition, float distance, float speedScale = 1f)
    {
        if (Monster == null || Agent == null || !Agent.isOnNavMesh)
        {
            return;
        }

        Vector3 direction = transform.position - targetPosition;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = -transform.forward;
        }

        Vector3 destination = transform.position + direction.normalized * distance;
        MoveTo(destination, speedScale, 0.05f, true);
    }

    public override void RotateToward(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion rotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, TurnSpeed * Time.deltaTime);
    }

    public override void StopWithBrake(float brakeTime = 0.2f)
    {
        if (Agent == null)
        {
            return;
        }

        HasPendingBrake = true;
        BrakeTimer = 0f;
        BrakeDuration = brakeTime;
        HasMoveCommand = false;
        IsMovementStopping = true;
        StopTimer = 0f;
        StopDelay = brakeTime;
        DesiredSpeed = 0f;
    }

    public override void StopImmediately()
    {
        if (Agent == null)
        {
            return;
        }

        Agent.isStopped = true;

        if (Agent.hasPath)
        {
            Agent.ResetPath();
        }

        HasMoveCommand = false;
        HasPendingBrake = false;
        IsMovementStopping = false;
        HasLastDestination = false;
        StopTimer = 0f;
        StopDelay = 0f;
        CurrentSpeed = 0f;
        DesiredSpeed = 0f;
        Agent.speed = 0f;
        SetPhase(GroundMovePhase.Idle);
    }

    public override void Stop()
    {
        StopImmediately();
    }

    public void SetMovementLock(bool isLocked)
    {
        if (IsMovementLocked == isLocked)
        {
            return;
        }

        IsMovementLocked = isLocked;

        if (IsMovementLocked)
        {
            StopImmediately();
        }
    }

    public override void SetMoveSpeed(float speed)
    {
        MoveSpeed = speed;
        DesiredSpeed = speed;

        if (Agent != null)
        {
            Agent.speed = CurrentSpeed;
        }
    }

    public override bool IsMovingOrBraking()
    {
        return CurrentMovePhase == GroundMovePhase.Accelerating ||
               CurrentMovePhase == GroundMovePhase.Moving ||
               CurrentMovePhase == GroundMovePhase.Braking;
    }

    public override float GetCurrentSpeed()
    {
        return CurrentSpeed;
    }

    public override GroundMovePhase GetMovePhase()
    {
        return CurrentMovePhase;
    }

    private void StopAgentOnly()
    {
        if (Agent == null ||
            !Agent.isOnNavMesh)
        {
            return;
        }

        if (!Agent.isStopped)
        {
            Agent.isStopped = true;
        }

        if (Agent.hasPath)
        {
            Agent.ResetPath();
        }

        HasMoveCommand = false;
        HasPendingBrake = false;
        IsMovementStopping = true;

        HasLastDestination = false;

        StopTimer = 0f;
        StopDelay = 0f;
    }

    private void UpdateMovementAnimation()
    {
        if (Monster == null)
        {
            return;
        }

        if (CurrentMovePhase == GroundMovePhase.Idle || CurrentSpeed <= MinSpeedForAnimation)
        {
            SetMovementAnimation(MonsterAnimationType.Idle);
            return;
        }

        MonsterAnimationType animation = CurrentMovementMode == GroundMovementMode.Run || Monster.IsUsingRunAnimation
            ? MonsterAnimationType.Run
            : MonsterAnimationType.Move;

        SetMovementAnimation(animation);
    }

    private void SetPhase(GroundMovePhase phase)
    {
        if (CurrentMovePhase == phase)
        {
            return;
        }

        CurrentMovePhase = phase;
        UpdateMovementAnimation();
    }

    private void SetMovementAnimation(
        MonsterAnimationType animationType,
        bool force = false)
    {
        if (!force &&
            HasCurrentAnimation &&
            CurrentAnimation ==
            animationType)
        {
            return;
        }

        CurrentAnimation =
            animationType;

        HasCurrentAnimation =
            true;

        Monster.PlayAnimation(
            animationType
        );
    }
}