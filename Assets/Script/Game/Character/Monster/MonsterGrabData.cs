using UnityEngine;

[System.Serializable]
public class MonsterGrabData
{
    [Header("Groggy")]

    public float MaxGroggy = 100f;

    public float CurrentGroggy;

    [Header("Execution")]

    public float ExecutionDamage = 500f;

    public float KnockBackForce = 5f;

    public float KnockDownTime = 3f;

    [Header("Grab")]

    public float GrabDistance = 2f;

    [Range(0f, 1f)]
    public float GrabViewDot = 0.8f;
}