using UnityEngine;

[DisallowMultipleComponent]
public class MonsterGroundMovement : BaseMonsterMovement
{
    [SerializeField]
    private float MoveSpeed = 3f;

    [SerializeField]
    private float StopDistance = 0.1f;

    [SerializeField]
    private float TurnSpeed = 720f;

    [SerializeField]
    private bool UseGravity = true;

    [SerializeField]
    private LayerMask GroundMask = -1;

    private Vector3? TargetPosition;
    private CharacterController CharacterController;
    private Vector3 Velocity;

    private void Awake()
    {
        CharacterController = GetComponent<CharacterController>();

        if (CharacterController == null)
        {
            CharacterController = gameObject.AddComponent<CharacterController>();
        }
    }

    public override void Initialize(BaseMonster monster)
    {
        base.Initialize(monster);
        MoveSpeed = monster.MoveSpeedValue;
    }

    public override void Tick(float deltaTime)
    {
        if (Monster == null || Monster.CurrentState != MonsterState.Normal)
        {
            return;
        }

        Vector3 desiredVelocity = Vector3.zero;

        if (TargetPosition.HasValue)
        {
            Vector3 currentPosition = transform.position;
            Vector3 direction = TargetPosition.Value - currentPosition;
            direction.y = 0f;

            if (direction.sqrMagnitude <= StopDistance * StopDistance)
            {
                TargetPosition = null;
            }
            else
            {
                desiredVelocity = direction.normalized * MoveSpeed;
            }
        }

        if (UseGravity)
        {
            if (CharacterController != null && CharacterController.isGrounded)
            {
                Velocity.y = -2f;
            }
            else
            {
                Velocity.y += Physics.gravity.y * deltaTime;
            }
        }

        Vector3 finalVelocity = desiredVelocity + Velocity;
        CharacterController?.Move(finalVelocity * deltaTime);

        MonsterAnimationType animationType = MonsterAnimationType.Idle;

        if (desiredVelocity.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredVelocity, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, TurnSpeed * deltaTime);
            animationType = Monster.IsUsingRunAnimation ? MonsterAnimationType.Run : MonsterAnimationType.Move;
        }

        Monster?.PlayAnimation(animationType);
    }

    public override void MoveTo(Vector3 destination, float speedScale = 1f)
    {
        TargetPosition = destination;
        MoveSpeed = Monster != null ? Monster.MoveSpeedValue * speedScale : MoveSpeed * speedScale;
    }

    public override void Stop()
    {
        TargetPosition = null;
        Velocity = Vector3.zero;
    }

    public override void SetMoveSpeed(float speed)
    {
        MoveSpeed = speed;
    }
}
