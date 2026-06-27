using UnityEngine;

public static class MonsterUtility
{
    public static bool IsTargetInFront(
        Transform observer,
        Transform target,
        float dot)
    {
        Vector3 direction =
            (target.position - observer.position).normalized;

        return Vector3.Dot(
            observer.forward,
            direction) >= dot;
    }

    public static float Distance(
        Transform a,
        Transform b)
    {
        return Vector3.Distance(
            a.position,
            b.position);
    }
}