using UnityEngine;

public class MonsterFlyingMovement : BaseMonsterMovement
{
    [SerializeField]
    private float MoveSpeed = 4f;

    [SerializeField]
    private float StopDistance = 0.2f;

    private Vector3? TargetPosition;

    public override void Initialize(BaseMonster monster)
    {
        base.Initialize(monster);
        MoveSpeed = monster.MoveSpeedValue;
    }

    public override void Tick(float deltaTime)
    {
        if (Monster == null || Monster.CurrentState != MonsterState.Normal || !TargetPosition.HasValue)
        {
            return;
        }

        Vector3 direction = TargetPosition.Value - transform.position;
        if (direction.sqrMagnitude <= StopDistance * StopDistance)
        {
            TargetPosition = null;
            return;
        }

        transform.position += direction.normalized * MoveSpeed * deltaTime;
        Monster.PlayAnimation(MonsterAnimationType.Move);
    }

    public override void MoveTo(Vector3 destination, float speedScale = 1f)
    {
        TargetPosition = destination;
        MoveSpeed = Monster != null ? Monster.MoveSpeedValue * speedScale : MoveSpeed * speedScale;
    }

    public override void Stop()
    {
        TargetPosition = null;
    }

    public override void SetMoveSpeed(float speed)
    {
        MoveSpeed = speed;
    }
}
