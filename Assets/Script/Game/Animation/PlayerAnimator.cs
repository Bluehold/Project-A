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
    private CharacterController characterController;

    public Animator GetAnimator() => anim;

    private void Start()
    {
        anim = PlayerModel.GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
    }

    
    private void Update()
    {
        anim.SetFloat("Yvelocity", characterController.velocity.y);
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
    }
}
