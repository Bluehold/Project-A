using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private InputSystem_Actions inputs;

    public Vector2 MoveInput { get; private set; }
    public bool IsSprinting { get; private set; }

    public bool SprintRequested { get; private set; }
    public bool AttackRequested { get; private set; }
    public bool Skill1Requested { get; private set; }
    public bool Skill2Requested { get; private set; }

    public bool IsCameraDragPressed { get; private set; }
    public Vector2 LookInput { get; private set; }
    public float ScrollInput { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("[Awake] Instance already exists");
            Destroy(gameObject);
        }

        Instance = this;


        inputs = new InputSystem_Actions();
        inputs.Enable();
    }

    private void OnDestroy()
    {
        inputs.Disable();
    }

    private void Update()
    {
        MoveInput = inputs.Player.Move.ReadValue<Vector2>();

        IsSprinting = inputs.Player.Sprint.IsPressed();

        if (inputs.Player.Sprint.WasPressedThisFrame())
            SprintRequested = true;

        if (inputs.Player.Attack.WasPressedThisFrame())
            AttackRequested = true;

        if (inputs.Player.Skill1.WasPressedThisFrame())
            Skill1Requested = true;

        if (inputs.Player.Skill2.WasPressedThisFrame())
            Skill2Requested = true;

        IsCameraDragPressed = inputs.Player.CameraDrag.IsPressed();
        LookInput = inputs.Player.Look.ReadValue<Vector2>();

        ScrollInput = inputs.Player.CameraZoom.ReadValue<float>();

    }

    public bool ConsumeSprint()
    {
        if (SprintRequested == false)
            return false;

        SprintRequested = false;
        return true;
    }

    public bool ConsumeAttack()
    {
        if (AttackRequested == false)
            return false;

        AttackRequested = false;
        return true;
    }

    public bool ConsumeSkill1()
    {
        if (Skill1Requested == false)
            return false;

        Skill1Requested = false;
        return true;
    }

    public bool ConsumeSkill2()
    {
        if (Skill2Requested == false)
            return false;

        Skill2Requested = false;
        return true;
    }
}
