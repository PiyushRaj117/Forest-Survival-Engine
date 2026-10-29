using UnityEngine;
using UnityEngine.Events;

public class PlayerVitals : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Hunger Settings")]
    public float maxHunger = 100f;
    public float currentHunger;
    public float hungerDecayRate = 0.5f; // Depletes per second

    [Header("Thirst Settings")]
    public float maxThirst = 100f;
    public float currentThirst;
    public float thirstDecayRate = 0.8f; // Depletes per second

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 10f;

    // Events for UI Updates (Decoupled Design for clean architecture)
    public UnityAction<float, float> OnHealthChanged;
    public UnityAction<float, float> OnHungerChanged;
    public UnityAction<float, float> OnThirstChanged;
    public UnityAction<float, float> OnStaminaChanged;

    private void Start()
    {
        currentHealth = maxHealth;
        currentHunger = maxHunger;
        currentThirst = maxThirst;
        currentStamina = maxStamina;
    }

    private void Update()
    {
        HandleDecay();
    }

    private void HandleDecay()
    {
        // Hunger & Thirst Decay over time
        if (currentHunger > 0)
        {
            currentHunger -= hungerDecayRate * Time.deltaTime;
            OnHungerChanged?.Invoke(currentHunger, maxHunger);
        }
        else
        {
            TakeDamage(1f * Time.deltaTime); // Starvation damage
        }

        if (currentThirst > 0)
        {
            currentThirst -= thirstDecayRate * Time.deltaTime;
            OnThirstChanged?.Invoke(currentThirst, maxThirst);
        }
        else
        {
            TakeDamage(2f * Time.deltaTime); // Dehydration damage
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Eat(float amount)
    {
        currentHunger = Mathf.Clamp(currentHunger + amount, 0, maxHunger);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    public void Drink(float amount)
    {
        currentThirst = Mathf.Clamp(currentThirst + amount, 0, maxThirst);
        OnThirstChanged?.Invoke(currentThirst, maxThirst);
    }

    private void Die()
    {
        Debug.Log("Player has died!");
    }
}