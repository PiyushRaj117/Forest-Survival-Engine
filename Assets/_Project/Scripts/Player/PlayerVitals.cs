using System.Collections;
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
    public float hungerDecayRate = 0.5f;

    [Header("Thirst Settings")]
    public float maxThirst = 100f;
    public float currentThirst;
    public float thirstDecayRate = 0.8f;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 15f;
    public float staminaRegenDelay = 1.5f;

    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 3.0f;
    private Vector3 spawnPoint;
    private bool isDead;

    private float staminaRegenTimer;
    private bool isUsingStamina;

    public bool HasStamina => currentStamina > 3f;

    // Events for UI Updates
    public UnityAction<float, float> OnHealthChanged;
    public UnityAction<float, float> OnHungerChanged;
    public UnityAction<float, float> OnThirstChanged;
    public UnityAction<float, float> OnStaminaChanged;

    private void Start()
    {
        spawnPoint = transform.position;
        currentHealth = maxHealth;
        currentHunger = maxHunger;
        currentThirst = maxThirst;
        currentStamina = maxStamina;

        NotifyAllVitals();
    }

    public void NotifyAllVitals()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
        OnThirstChanged?.Invoke(currentThirst, maxThirst);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    private void Update()
    {
        if (isDead) return;

        HandleDecay();
        HandleStaminaRegen();
    }

    private void HandleDecay()
    {
        if (currentHunger > 0)
        {
            currentHunger -= hungerDecayRate * Time.deltaTime;
            OnHungerChanged?.Invoke(currentHunger, maxHunger);
        }
        else
        {
            TakeDamage(1f * Time.deltaTime);
        }

        if (currentThirst > 0)
        {
            currentThirst -= thirstDecayRate * Time.deltaTime;
            OnThirstChanged?.Invoke(currentThirst, maxThirst);
        }
        else
        {
            TakeDamage(2f * Time.deltaTime);
        }
    }

    private void HandleStaminaRegen()
    {
        if (staminaRegenTimer > 0f)
        {
            staminaRegenTimer -= Time.deltaTime;
            return;
        }

        isUsingStamina = false;

        if (currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + (staminaRegenRate * Time.deltaTime));
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }

    public bool UseStamina(float amount)
    {
        if (currentStamina <= 0f) return false;

        currentStamina = Mathf.Clamp(currentStamina - amount, 0f, maxStamina);
        staminaRegenTimer = staminaRegenDelay;
        isUsingStamina = true;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        return currentStamina > 0f;
    }

    public void StopUsingStamina()
    {
        isUsingStamina = false;
    }

    public bool ConsumeStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            staminaRegenTimer = staminaRegenDelay;
            isUsingStamina = true;
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            return true;
        }
        return false;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Eat(float amount)
    {
        if (isDead) return;
        currentHunger = Mathf.Clamp(currentHunger + amount, 0, maxHunger);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    public void Drink(float amount)
    {
        if (isDead) return;
        currentThirst = Mathf.Clamp(currentThirst + amount, 0, maxThirst);
        OnThirstChanged?.Invoke(currentThirst, maxThirst);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        CustomPlayerController controller = GetComponent<CustomPlayerController>();
        if (controller != null)
        {
            controller.OnDeath();
        }

        Debug.Log("<color=red><b>[Player]</b></color> Player died! Respawning in " + respawnDelay + " seconds...");
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        currentHealth = maxHealth;
        currentHunger = maxHunger;
        currentThirst = maxThirst;
        currentStamina = maxStamina;
        isDead = false;

        CustomPlayerController controller = GetComponent<CustomPlayerController>();
        if (controller != null)
        {
            controller.OnRespawn(spawnPoint);
        }

        NotifyAllVitals();
        Debug.Log("<color=green><b>[Player]</b></color> Player respawned at starting point!");
    }
}
