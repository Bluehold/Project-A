using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    public enum MovingState
    {
        Idle = 0,
        Walk = 1,
        Sprint = 2
    }

    [SerializeField]
    private GameObject PlayerModel;
    private Animator anim;
    private Rigidbody rb;

    public Animator GetAnimator() => anim;

    private void Start()
    {
        anim = PlayerModel.GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    
    private void Update()
    {
        anim.SetFloat("Yvelocity", rb.linearVelocity.y);
    }

    public void TriggerRoll()
    {
        anim.SetTrigger("Roll");
    }

    public void OnGround(bool value)
    {
        anim.SetBool("OnGround", value);
    }

    public void SetMovingState(MovingState state)
    {
        anim.SetInteger("MovingState", (int)state);

        if (state == MovingState.Idle)
        {
            anim.Play("Strut Walking", (int)state);
        }
    }

    public void PlayAttack(string stateName)
    {
        anim.CrossFade(stateName, 0.1f, 0, 0f);
    }
}
