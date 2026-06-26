using System;
using UnityEngine;

public class PlayerAttackTrigger : MonoBehaviour
{
    public string HitboxName;
    public Action<Collider, string> OnAttackTriggerEnter;

    private void OnTriggerEnter(Collider other)
    {
        OnAttackTriggerEnter?.Invoke(other, HitboxName);
    }
}
