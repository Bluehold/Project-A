using UnityEngine;

public enum FrontGrabAnimationType
{
    None,
    Grab,
    Execute,
    KnockBack,
    KnockDown,
    Recover
}

public struct FrontGrabData
{
    // 플레이어가 이동할 고정 위치
    public Vector3 Position;

    // 플레이어가 바라볼 방향
    public Quaternion Rotation;

    // 실행할 애니메이션 종류
    public FrontGrabAnimationType AnimationType;

    public FrontGrabData(
        Vector3 position,
        Quaternion rotation,
        FrontGrabAnimationType animationType)
    {
        Position = position;
        Rotation = rotation;
        AnimationType = animationType;
    }
}