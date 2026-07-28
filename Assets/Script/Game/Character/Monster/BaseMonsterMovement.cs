using UnityEngine;

public enum GroundMovementMode
{
    Walk,
    Run
}

public enum GroundMovePhase
{
    Idle,
    Accelerating,
    Moving,
    Braking,
    Locked
}

public abstract class BaseMonsterMovement : MonoBehaviour
{
    protected BaseMonster Monster;

    public virtual void Initialize(BaseMonster monster)
    {
        Monster = monster;
    }

    public virtual void Tick(float deltaTime)
    {
    }

    public virtual void MoveTo(Vector3 destination, float speedScale = 1f)
    {
    }

    public virtual void MoveTo(Vector3 destination, float speedScale, float stopDistanceOverride, bool useBrake)
    {
    }

    public virtual void MoveToMaintainDistance(Vector3 targetPosition, float desiredDistance, float speedScale = 1f)
    {
    }

    public virtual void MoveAwayFrom(Vector3 targetPosition, float distance, float speedScale = 1f)
    {
    }

    public virtual void RotateToward(Vector3 targetPosition)
    {
    }

    public virtual void SetMovementMode(GroundMovementMode mode)
    {
    }

    public virtual void Stop()
    {
    }

    public virtual void StopWithBrake(float brakeTime = 0.2f)
    {
    }

    public virtual void StopImmediately()
    {
    }

    public virtual void SetMoveSpeed(float speed)
    {
    }

    public virtual bool IsMovingOrBraking()
    {
        return false;
    }

    public virtual float GetCurrentSpeed()
    {
        return 0f;
    }

    public virtual GroundMovePhase GetMovePhase()
    {
        return GroundMovePhase.Idle;
    }
}
