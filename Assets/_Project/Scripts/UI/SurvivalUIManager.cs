using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ForestSurvival.UI
{
    /// <summary>
    /// Manages the Survival HUD (Health, Hunger, Thirst, Stamina bars).
    /// Subscribes to PlayerVitals events and provides smooth visual bar transitions.
    /// </summary>
    public class SurvivalUIManager : MonoBehaviour
    {
        [Header("Player Vitals Source")]
        [SerializeField] private PlayerVitals playerVitals;

        [Header("Sliders")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider hungerSlider;
        [SerializeField] private Slider thirstSlider;
        [SerializeField] private Slider staminaSlider;

        [Header("Value Texts (Optional)")]
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI hungerText;
        [SerializeField] private TextMeshProUGUI thirstText;
        [SerializeField] private TextMeshProUGUI staminaText;

        [Header("Animation Settings")]
        [SerializeField] private bool smoothTransitions = true;
        [SerializeField] private float smoothSpeed = 5f;

        private float targetHealth = 1f;
        private float targetHunger = 1f;
        private float targetThirst = 1f;
        private float targetStamina = 1f;

        private void Awake()
        {
            if (playerVitals == null)
            {
                playerVitals = FindFirstObjectByType<PlayerVitals>();
            }

            InitializeSliders();
        }

        private void OnEnable()
        {
            if (playerVitals != null)
            {
                playerVitals.OnHealthChanged += UpdateHealth;
                playerVitals.OnHungerChanged += UpdateHunger;
                playerVitals.OnThirstChanged += UpdateThirst;
                playerVitals.OnStaminaChanged += UpdateStamina;

                // Force initial refresh
                playerVitals.NotifyAllVitals();
            }
        }

        private void OnDisable()
        {
            if (playerVitals != null)
            {
                playerVitals.OnHealthChanged -= UpdateHealth;
                playerVitals.OnHungerChanged -= UpdateHunger;
                playerVitals.OnThirstChanged -= UpdateThirst;
                playerVitals.OnStaminaChanged -= UpdateStamina;
            }
        }

        private void InitializeSliders()
        {
            ConfigureSlider(healthSlider);
            ConfigureSlider(hungerSlider);
            ConfigureSlider(thirstSlider);
            ConfigureSlider(staminaSlider);
        }

        private void ConfigureSlider(Slider slider)
        {
            if (slider == null) return;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.interactable = false;
        }

        private void Update()
        {
            if (!smoothTransitions) return;

            AnimateSlider(healthSlider, targetHealth);
            AnimateSlider(hungerSlider, targetHunger);
            AnimateSlider(thirstSlider, targetThirst);
            AnimateSlider(staminaSlider, targetStamina);
        }

        private void AnimateSlider(Slider slider, float target)
        {
            if (slider == null) return;
            slider.value = Mathf.MoveTowards(slider.value, target, smoothSpeed * Time.deltaTime);
        }

        public void UpdateHealth(float current, float max)
        {
            targetHealth = max > 0 ? Mathf.Clamp01(current / max) : 0f;
            if (!smoothTransitions && healthSlider != null) healthSlider.value = targetHealth;
            if (healthText != null) healthText.text = $"{Mathf.CeilToInt(current)}";
        }

        public void UpdateHunger(float current, float max)
        {
            targetHunger = max > 0 ? Mathf.Clamp01(current / max) : 0f;
            if (!smoothTransitions && hungerSlider != null) hungerSlider.value = targetHunger;
            if (hungerText != null) hungerText.text = $"{Mathf.CeilToInt(current)}";
        }

        public void UpdateThirst(float current, float max)
        {
            targetThirst = max > 0 ? Mathf.Clamp01(current / max) : 0f;
            if (!smoothTransitions && thirstSlider != null) thirstSlider.value = targetThirst;
            if (thirstText != null) thirstText.text = $"{Mathf.CeilToInt(current)}";
        }

        public void UpdateStamina(float current, float max)
        {
            targetStamina = max > 0 ? Mathf.Clamp01(current / max) : 0f;
            if (!smoothTransitions && staminaSlider != null) staminaSlider.value = targetStamina;
            if (staminaText != null) staminaText.text = $"{Mathf.CeilToInt(current)}";
        }

        public void SetPlayerVitals(PlayerVitals vitals)
        {
            if (playerVitals != null)
            {
                playerVitals.OnHealthChanged -= UpdateHealth;
                playerVitals.OnHungerChanged -= UpdateHunger;
                playerVitals.OnThirstChanged -= UpdateThirst;
                playerVitals.OnStaminaChanged -= UpdateStamina;
            }

            playerVitals = vitals;

            if (isActiveAndEnabled && playerVitals != null)
            {
                playerVitals.OnHealthChanged += UpdateHealth;
                playerVitals.OnHungerChanged += UpdateHunger;
                playerVitals.OnThirstChanged += UpdateThirst;
                playerVitals.OnStaminaChanged += UpdateStamina;
                playerVitals.NotifyAllVitals();
            }
        }
    }
}
