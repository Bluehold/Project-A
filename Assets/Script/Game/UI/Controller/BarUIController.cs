using UnityEngine;

public class BarUIController : MonoBehaviour
{
    public static BarUIController Instance;
    public BarRenderer HealthBar;
    public BarRenderer ManaBar;
    public BarRenderer StaminaBar;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Debug.LogError("[Awake] Instance cannot be more than one");
    }
}
