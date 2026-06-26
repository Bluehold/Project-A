using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputSystem_Actions Inputs;

    private void Awake()
    {
        Inputs = new InputSystem_Actions();
        Inputs.Enable();
    }

    private void OnDestroy()
    {
        Inputs.Disable();
    }
}
