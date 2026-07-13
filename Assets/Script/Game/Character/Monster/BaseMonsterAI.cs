using UnityEngine;

public abstract class BaseMonsterAI : MonoBehaviour
{
    protected BaseMonster Monster;

    public virtual void Initialize(BaseMonster monster)
    {
        Monster = monster;
    }

    public virtual void Tick(BaseMonster monster, float deltaTime)
    {
    }

    public virtual void Stop()
    {
    }

    public virtual void OnStateChanged(MonsterState state)
    {
    }
}
