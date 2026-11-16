using UnityEngine;
using UnityEngine.UI;

namespace ForestSurvival.UI
{
    /// <summary>
    /// Sons of the Forest style Survival HUD.
    /// Manages the circular GPS tracker vitals:
    /// - Left Arc: Health (Crimson Red, 0 to 180 degrees counter-clockwise)
    /// - Right Arc: Stamina (Teal/Cyan Green, 0 to 180 degrees clockwise)
    /// - Top Badges: Thirst (Water drop) and Hunger (Meat/Food) circular 360 dials
    /// </summary>
    public class SOTF_SurvivalHUD : MonoBehaviour
    {
        [Header("Vitals Source")]
        [SerializeField] private PlayerVitals playerVitals;

        [Header("Dual Radial Arc Bars (Left & Right)")]
        [Tooltip("Left semi-circle arc for Health (Clockwise = False, Max = 0.5)")]
        [SerializeField] private Image healthArc;

        [Tooltip("Right semi-circle arc for Stamina (Clockwise = True, Max = 0.5)")]
        [SerializeField] private Image staminaArc;

        [Header("Top Orbital Badges (360 Radial Fill)")]
        [Tooltip("Circular ring fill for Hunger")]
        [SerializeField] private Image hungerRing;

        [Tooltip("Circular ring fill for Thirst")]
        [SerializeField] private Image thirstRing;

        [Tooltip("Circular ring fill for Muscle/Strength (Optional)")]
        [SerializeField] private Image muscleRing;

        [Header("Smooth Transitions")]
        [SerializeField] private bool smoothTransitions = true;
        [SerializeField] private float smoothSpeed = 4f;

        [Header("Critical Pulse Alert")]
        [SerializeField] private bool pulseOnCritical = true;
        [SerializeField] private float pulseThreshold = 0.25f; // < 25% health

        private float targetHealthArc = 0.5f;
        private float targetStaminaArc = 0.5f;
        private float targetHungerRing = 1.0f;
        private float targetThirstRing = 1.0f;

        private Color initialHealthColor;
        private bool isSubscribed;

        private void Awake()
        {
            if (playerVitals == null)
            {
                playerVitals = FindFirstObjectByType<PlayerVitals>();
            }

            if (healthArc != null)
            {
                initialHealthColor = healthArc.color;
                healthArc.type = Image.Type.Filled;
                healthArc.fillMethod = Image.FillMethod.Radial360;
                healthArc.fillOrigin = (int)Image.Origin360.Top;
                healthArc.fillClockwise = false;
            }

            if (staminaArc != null)
            {
                staminaArc.type = Image.Type.Filled;
                staminaArc.fillMethod = Image.FillMethod.Radial360;
                staminaArc.fillOrigin = (int)Image.Origin360.Top;
                staminaArc.fillClockwise = true;
            }

            if (hungerRing != null)
            {
                hungerRing.type = Image.Type.Filled;
                hungerRing.fillMethod = Image.FillMethod.Radial360;
                hungerRing.fillOrigin = (int)Image.Origin360.Top;
                hungerRing.fillClockwise = true;
            }

            if (thirstRing != null)
            {
                thirstRing.type = Image.Type.Filled;
                thirstRing.fillMethod = Image.FillMethod.Radial360;
                thirstRing.fillOrigin = (int)Image.Origin360.Top;
                thirstRing.fillClockwise = true;
            }
        }

        private void OnEnable()
        {
            SubscribeToVitals();
        }

        private void OnDisable()
        {
            UnsubscribeFromVitals();
        }

        private void SubscribeToVitals()
        {
            if (isSubscribed || playerVitals == null) return;

            playerVitals.OnHealthChanged += OnHealthChanged;
            playerVitals.OnStaminaChanged += OnStaminaChanged;
            playerVitals.OnHungerChanged += OnHungerChanged;
            playerVitals.OnThirstChanged += OnThirstChanged;
            isSubscribed = true;

            // Trigger initial sync
            playerVitals.NotifyAllVitals();
        }

        private void UnsubscribeFromVitals()
        {
            if (!isSubscribed || playerVitals == null) return;

            playerVitals.OnHealthChanged -= OnHealthChanged;
            playerVitals.OnStaminaChanged -= OnStaminaChanged;
            playerVitals.OnHungerChanged -= OnHungerChanged;
            playerVitals.OnThirstChanged -= OnThirstChanged;
            isSubscribed = false;
        }

        private void Update()
        {
            AnimateFills();
            HandleCriticalPulse();
        }

        private void AnimateFills()
        {
            float dt = Time.deltaTime * smoothSpeed;

            if (healthArc != null)
            {
                healthArc.fillAmount = smoothTransitions
                    ? Mathf.MoveTowards(healthArc.fillAmount, targetHealthArc, dt * 0.5f)
                    : targetHealthArc;
            }

            if (staminaArc != null)
            {
                staminaArc.fillAmount = smoothTransitions
                    ? Mathf.MoveTowards(staminaArc.fillAmount, targetStaminaArc, dt * 0.5f)
                    : targetStaminaArc;
            }

            if (hungerRing != null)
            {
                hungerRing.fillAmount = smoothTransitions
                    ? Mathf.MoveTowards(hungerRing.fillAmount, targetHungerRing, dt)
                    : targetHungerRing;
            }

            if (thirstRing != null)
            {
                thirstRing.fillAmount = smoothTransitions
                    ? Mathf.MoveTowards(thirstRing.fillAmount, targetThirstRing, dt)
                    : targetThirstRing;
            }
        }

        private void HandleCriticalPulse()
        {
            if (!pulseOnCritical || healthArc == null) return;

            // Health is under threshold (scaled to half-arc)
            if (targetHealthArc < (pulseThreshold * 0.5f))
            {
                float pulse = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f;
                healthArc.color = Color.Lerp(initialHealthColor, Color.white, pulse * 0.7f);
            }
            else
            {
                healthArc.color = initialHealthColor;
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            float pct = max > 0 ? Mathf.Clamp01(current / max) : 0f;
            targetHealthArc = pct * 0.5f; // Left semi-circle is 50% of the 360 circle
        }

        private void OnStaminaChanged(float current, float max)
        {
            float pct = max > 0 ? Mathf.Clamp01(current / max) : 0f;
            targetStaminaArc = pct * 0.5f; // Right semi-circle is 50% of the 360 circle
        }

        private void OnHungerChanged(float current, float max)
        {
            targetHungerRing = max > 0 ? Mathf.Clamp01(current / max) : 0f;
        }

        private void OnThirstChanged(float current, float max)
        {
            targetThirstRing = max > 0 ? Mathf.Clamp01(current / max) : 0f;
        }
    }
}
