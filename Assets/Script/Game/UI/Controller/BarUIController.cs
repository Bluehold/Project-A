using UnityEngine;

public class BarUIController : MonoBehaviour
{
    public BarRenderer HealthBar;
    public BarRenderer ManaBar;
    public BarRenderer StaminaBar;

    [SerializeField]
    private BasePlayer Player;

    private void OnEnable()
    {
        if (Player != null)
        {
            Player.HealthChanged += SetHealthBar;
            Player.ManaChanged += SetManaBar;
            Player.StaminaChanged += SetStaminaBar;
            Player.HealthDropped += DrainHealthBar;
            Player.ManaDropped += DrainManaBar;
            Player.StaminaDropped += DrainStaminaBar;
        }
    }

    private void OnDisable()
    {
        if (Player != null)
        {
            Player.HealthChanged -= SetHealthBar;
            Player.ManaChanged -= SetManaBar;
            Player.StaminaChanged -= SetStaminaBar;
            Player.HealthDropped -= DrainHealthBar;
            Player.ManaDropped -= DrainManaBar;
            Player.StaminaDropped -= DrainStaminaBar;
        }
    }

    private void SetHealthBar(float health, float maxHealth)
    {
        HealthBar.SetValue(health / maxHealth);
    }

    private void SetManaBar(float mana, float maxMana)
    {
        ManaBar.SetValue(mana / maxMana);
    }

    private void SetStaminaBar(float stamina, float maxStamina)
    {
        StaminaBar.SetValue(stamina / maxStamina);
    }

    private void DrainHealthBar(float health, float maxHealth)
    {
        HealthBar.DrainBar(health / maxHealth);
    }

    private void DrainManaBar(float mana, float maxMana)
    {
        ManaBar.DrainBar(mana / maxMana);
    }

    private void DrainStaminaBar(float stamina, float maxStamina)
    {
        StaminaBar.DrainBar(stamina / maxStamina);
    }
}
