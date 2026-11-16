using UnityEngine;

namespace ForestSurvival.Environment
{
    public class DayNightCycle : MonoBehaviour
    {
        [Header("Time Configuration")]
        [Tooltip("Length of a full 24-hour in-game day in real-time seconds")]
        [SerializeField] private float dayDurationInSeconds = 180f;

        [Range(0f, 24f)]
        [SerializeField] private float currentHour = 10f;

        [Header("Light References")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Light moonLight;

        [Header("Skybox Settings")]
        [SerializeField] private Material daySkyboxMaterial;
        [SerializeField] private Material nightSkyboxMaterial;
        [SerializeField] private float skyRotationSpeed = 0.4f;

        [Header("Ambient Lighting Colors")]
        [SerializeField] private Color dayAmbient = new Color(0.65f, 0.65f, 0.7f);
        [SerializeField] private Color duskAmbient = new Color(0.35f, 0.22f, 0.28f);
        [SerializeField] private Color nightAmbient = new Color(0.04f, 0.05f, 0.09f);

        [Header("Sun Colors")]
        [SerializeField] private Color sunDayColor = new Color(1.0f, 0.98f, 0.92f);
        [SerializeField] private Color sunSunsetOrange = new Color(1.0f, 0.48f, 0.16f);
        [SerializeField] private Color sunDuskRed = new Color(0.75f, 0.22f, 0.25f);

        [Header("Fog Settings")]
        [SerializeField] private bool enableFogControl = true;
        [SerializeField] private Color dayFog = new Color(0.72f, 0.82f, 0.95f);
        [SerializeField] private Color sunsetFog = new Color(0.85f, 0.45f, 0.3f);
        [SerializeField] private Color nightFog = new Color(0.02f, 0.03f, 0.06f);

        public float NormalizedTime => currentHour / 24f;
        public float CurrentHour => currentHour;
        public bool IsNight => currentHour < 5.5f || currentHour > 19.5f;

        private Material currentRuntimeSkybox;
        private float skyboxRotation = 0f;

        private void Awake()
        {
            if (sunLight == null)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (Light l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        sunLight = l;
                        break;
                    }
                }
            }

            SetupMoonLight();
            FindSkyboxesIfMissing();
        }

        private void FindSkyboxesIfMissing()
        {
            if (daySkyboxMaterial == null)
            {
#if UNITY_EDITOR
                daySkyboxMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/DaySkyboxMat.mat");
#endif
            }
            if (nightSkyboxMaterial == null)
            {
#if UNITY_EDITOR
                nightSkyboxMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/NightGalaxySkybox.mat");
#endif
            }
        }

        private void SetupMoonLight()
        {
            if (moonLight == null && sunLight != null)
            {
                Transform existing = transform.Find("MoonLight");
                if (existing != null)
                {
                    moonLight = existing.GetComponent<Light>();
                }
                else
                {
                    GameObject moonObj = new GameObject("MoonLight");
                    moonObj.transform.SetParent(transform);
                    moonLight = moonObj.AddComponent<Light>();
                    moonLight.type = LightType.Directional;
                    moonLight.color = new Color(0.35f, 0.45f, 0.75f);
                    moonLight.intensity = 0.15f;
                    moonLight.shadows = LightShadows.Soft;
                }
            }
        }

        private void Update()
        {
            UpdateTime();
            UpdateSunAndMoon();
            UpdateSkyboxAndAmbient();
        }

        private void UpdateTime()
        {
            if (dayDurationInSeconds <= 0f) return;
            float hoursPerSecond = 24f / dayDurationInSeconds;
            currentHour += hoursPerSecond * Time.deltaTime;
            if (currentHour >= 24f) currentHour -= 24f;
        }

        private void UpdateSunAndMoon()
        {
            float norm = NormalizedTime;
            // Sun angle: 0h = -90 (midnight), 6h = 0 (sunrise), 12h = 90 (noon), 18h = 180 (sunset)
            float sunAngle = (norm * 360f) - 90f;

            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

                // Smooth sun intensity calculation
                float sunIntensity = 0f;
                Color sunColor = sunDayColor;

                if (currentHour >= 5.0f && currentHour < 7.0f)
                {
                    // Sunrise: 5h to 7h
                    float t = Mathf.InverseLerp(5.0f, 7.0f, currentHour);
                    sunIntensity = Mathf.SmoothStep(0f, 1.25f, t);
                    sunColor = Color.Lerp(sunSunsetOrange, sunDayColor, t);
                }
                else if (currentHour >= 7.0f && currentHour < 16.5f)
                {
                    // Full Day: 7h to 16.5h
                    sunIntensity = 1.25f;
                    sunColor = sunDayColor;
                }
                else if (currentHour >= 16.5f && currentHour < 18.5f)
                {
                    // Golden Hour to Sunset: 16.5h to 18.5h
                    float t = Mathf.InverseLerp(16.5f, 18.5f, currentHour);
                    sunIntensity = Mathf.SmoothStep(1.25f, 0.4f, t);
                    sunColor = Color.Lerp(sunDayColor, sunSunsetOrange, t);
                }
                else if (currentHour >= 18.5f && currentHour < 19.8f)
                {
                    // Sunset to Dusk: 18.5h to 19.8h
                    float t = Mathf.InverseLerp(18.5f, 19.8f, currentHour);
                    sunIntensity = Mathf.SmoothStep(0.4f, 0f, t);
                    sunColor = Color.Lerp(sunSunsetOrange, sunDuskRed, t);
                }
                else
                {
                    // Night: 19.8h to 5.0h
                    sunIntensity = 0f;
                }

                sunLight.intensity = sunIntensity;
                sunLight.color = sunColor;
                sunLight.enabled = sunIntensity > 0.005f;
            }

            if (moonLight != null)
            {
                moonLight.transform.rotation = Quaternion.Euler(sunAngle + 180f, 170f, 0f);

                float moonIntensity = 0f;
                if (currentHour >= 19.5f || currentHour <= 5.5f)
                {
                    float factor = 1f;
                    if (currentHour >= 19.5f && currentHour <= 21.0f)
                        factor = Mathf.InverseLerp(19.5f, 21.0f, currentHour);
                    else if (currentHour >= 4.0f && currentHour <= 5.5f)
                        factor = Mathf.InverseLerp(5.5f, 4.0f, currentHour);

                    moonIntensity = Mathf.Lerp(0f, 0.16f, factor);
                }

                moonLight.intensity = moonIntensity;
                moonLight.enabled = moonIntensity > 0.005f;
            }
        }

        private void UpdateSkyboxAndAmbient()
        {
            // Smooth Ambient Color
            Color targetAmbient;
            Color targetFog;

            if (currentHour >= 6.5f && currentHour <= 16.5f)
            {
                targetAmbient = dayAmbient;
                targetFog = dayFog;
            }
            else if (currentHour > 16.5f && currentHour <= 19.0f)
            {
                float t = Mathf.InverseLerp(16.5f, 19.0f, currentHour);
                targetAmbient = Color.Lerp(dayAmbient, duskAmbient, t);
                targetFog = Color.Lerp(dayFog, sunsetFog, t);
            }
            else if (currentHour > 19.0f && currentHour <= 20.2f)
            {
                float t = Mathf.InverseLerp(19.0f, 20.2f, currentHour);
                targetAmbient = Color.Lerp(duskAmbient, nightAmbient, t);
                targetFog = Color.Lerp(sunsetFog, nightFog, t);
            }
            else if (currentHour > 20.2f || currentHour < 4.8f)
            {
                targetAmbient = nightAmbient;
                targetFog = nightFog;
            }
            else
            {
                // Dawn: 4.8h to 6.5h
                float t = Mathf.InverseLerp(4.8f, 6.5f, currentHour);
                targetAmbient = Color.Lerp(nightAmbient, dayAmbient, t);
                targetFog = Color.Lerp(nightFog, dayFog, t);
            }

            RenderSettings.ambientLight = targetAmbient;

            if (enableFogControl && RenderSettings.fog)
            {
                RenderSettings.fogColor = targetFog;
            }

            // Smooth Skybox Transition without sudden image pop
            Material desiredMat = IsNight ? nightSkyboxMaterial : daySkyboxMaterial;

            if (desiredMat != null)
            {
                if (RenderSettings.skybox != desiredMat)
                {
                    RenderSettings.skybox = desiredMat;
                }

                // Smooth Exposure Fade so swap happens at minimum brightness
                float exposure = 1.0f;
                if (currentHour >= 18.5f && currentHour <= 20.2f)
                {
                    // Dusk fade
                    float t = Mathf.InverseLerp(18.5f, 20.2f, currentHour);
                    // Dips to 0.1 at 19.35, then returns to 1.0
                    exposure = Mathf.Lerp(1.0f, 0.15f, Mathf.Sin(t * Mathf.PI));
                }
                else if (currentHour >= 4.5f && currentHour <= 6.2f)
                {
                    // Dawn fade
                    float t = Mathf.InverseLerp(4.5f, 6.2f, currentHour);
                    exposure = Mathf.Lerp(1.0f, 0.15f, Mathf.Sin(t * Mathf.PI));
                }

                if (RenderSettings.skybox.HasProperty("_Exposure"))
                {
                    RenderSettings.skybox.SetFloat("_Exposure", exposure);
                }

                // Slow atmospheric rotation
                skyboxRotation += skyRotationSpeed * Time.deltaTime;
                if (RenderSettings.skybox.HasProperty("_Rotation"))
                {
                    RenderSettings.skybox.SetFloat("_Rotation", skyboxRotation % 360f);
                }
            }
        }
    }
}
