using UnityEngine;

public class MonsterAnimationHook : MonoBehaviour
{
    public virtual void Play(MonsterAnimationType animationType)
    {

    }

    public virtual void Stop()
    {

    }

    public virtual bool IsPlaying()
    {
        return false;
    }

    public virtual void SetSpeed(float speed)
    {

    }

    public virtual void SetAnimationName(MonsterAnimationType animationType, string animationName)
    {

    }
}