using UnityEngine;

public class BarUIController : MonoBehaviour
{
    public static BarUIController Instance { get; private set; }

    public BarRenderer HealthBar;
    public BarRenderer ManaBar;
    public BarRenderer StaminaBar;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("[Awake] Instance already exists");
            Destroy(gameObject);
        }

        Instance = this;
    }

    public void SetHealthBar(float health, float maxHealth)
    {
        HealthBar.SetValue(health / maxHealth);
    }

    public void SetManaBar(float mana, float maxMana)
    {
        ManaBar.SetValue(mana / maxMana);
    }

    public void SetStaminaBar(float stamina, float maxStamina)
    {
        StaminaBar.SetValue(stamina / maxStamina);
    }

    public void DrainHealthBar(float health, float maxHealth)
    {
        HealthBar.DrainBar(health / maxHealth);
    }

    public void DrainManaBar(float mana, float maxMana)
    {
        ManaBar.DrainBar(mana / maxMana);
    }

    public void DrainStaminaBar(float stamina, float maxStamina)
    {
        StaminaBar.DrainBar(stamina / maxStamina);
    }
}
