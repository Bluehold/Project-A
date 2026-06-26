using System;
using UnityEngine;

public class ModelAnimation : MonoBehaviour
{
    public event Action<string> AnimationEnd;

    public void OnRollEnd()
    {
        AnimationEnd?.Invoke("Roll");
    }
}
