using UnityEngine;

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

    public virtual void Stop()
    {
    }

    public virtual void SetMoveSpeed(float speed)
    {
    }
}
