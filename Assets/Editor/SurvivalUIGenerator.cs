#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using ForestSurvival.UI;

namespace ForestSurvival.EditorTools
{
    public static class SurvivalUIGenerator
    {
        [MenuItem("Tools/Forest Survival/Generate Survival UI Canvas")]
        public static GameObject GenerateSurvivalUI()
        {
            // 1. Find or create Canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            GameObject canvasGo;

            if (canvas == null)
            {
                canvasGo = new GameObject("Survival_Canvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasGo.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasGo, "Create Survival Canvas");
            }
            else
            {
                canvasGo = canvas.gameObject;
            }

            // 2. Ensure EventSystem exists
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<InputSystemUIInputModule>();
                Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
            }

            // Check if SurvivalHUD already exists
            Transform existingHUD = canvasGo.transform.Find("SurvivalHUD");
            if (existingHUD != null)
            {
                Undo.DestroyObjectImmediate(existingHUD.gameObject);
            }

            // 3. Create SurvivalHUD root container
            GameObject hudGo = new GameObject("SurvivalHUD");
            hudGo.transform.SetParent(canvasGo.transform, false);

            RectTransform hudRect = hudGo.AddComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0f, 0f);
            hudRect.anchorMax = new Vector2(0f, 0f);
            hudRect.pivot = new Vector2(0f, 0f);
            hudRect.anchoredPosition = new Vector2(35f, 35f);
            hudRect.sizeDelta = new Vector2(280f, 160f);

            // Add background panel for readability
            Image hudBg = hudGo.AddComponent<Image>();
            hudBg.color = new Color(0.08f, 0.08f, 0.1f, 0.75f);

            VerticalLayoutGroup layout = hudGo.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = hudGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 4. Create the 4 vital bars
            // Colors:
            // Health: #E63946 (Red)
            // Hunger: #F4A261 (Warm Orange)
            // Thirst: #00B4D8 (Cyan / Blue)
            // Stamina: #2EC4B6 (Teal / Green)
            Slider healthSlider = CreateVitalBar("HealthBar", hudGo.transform, "HEALTH", new Color(0.9f, 0.22f, 0.27f, 1f));
            Slider hungerSlider = CreateVitalBar("HungerBar", hudGo.transform, "HUNGER", new Color(0.96f, 0.63f, 0.38f, 1f));
            Slider thirstSlider = CreateVitalBar("ThirstBar", hudGo.transform, "THIRST", new Color(0.0f, 0.7f, 0.85f, 1f));
            Slider staminaSlider = CreateVitalBar("StaminaBar", hudGo.transform, "STAMINA", new Color(0.18f, 0.77f, 0.71f, 1f));

            // 5. Attach & configure SurvivalUIManager
            SurvivalUIManager manager = hudGo.AddComponent<SurvivalUIManager>();

            // Use SerializedObject to wire up private serialized fields cleanly
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("healthSlider").objectReferenceValue = healthSlider;
            so.FindProperty("hungerSlider").objectReferenceValue = hungerSlider;
            so.FindProperty("thirstSlider").objectReferenceValue = thirstSlider;
            so.FindProperty("staminaSlider").objectReferenceValue = staminaSlider;

            PlayerVitals vitals = Object.FindFirstObjectByType<PlayerVitals>();
            if (vitals != null)
            {
                so.FindProperty("playerVitals").objectReferenceValue = vitals;
            }

            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(hudGo, "Generate Survival HUD");
            Selection.activeGameObject = hudGo;

            Debug.Log("<color=#4CAF50><b>[Survival UI]</b></color> Survival HUD successfully generated in active scene!");
            return hudGo;
        }

        private static Slider CreateVitalBar(string name, Transform parent, string label, Color fillColor)
        {
            GameObject barGo = new GameObject(name);
            barGo.transform.SetParent(parent, false);

            LayoutElement barLayout = barGo.AddComponent<LayoutElement>();
            barLayout.minHeight = 22f;
            barLayout.preferredHeight = 22f;

            Slider slider = barGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;

            // Background
            GameObject bgGo = new GameObject("Background");
            bgGo.transform.SetParent(barGo.transform, false);
            RectTransform bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.18f, 0.9f);

            // Fill Area
            GameObject fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(barGo.transform, false);
            RectTransform fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = new Vector2(-4f, -4f);
            fillAreaRect.anchoredPosition = Vector2.zero;

            // Fill Image
            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            RectTransform fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            Image fillImg = fillGo.AddComponent<Image>();
            fillImg.color = fillColor;

            slider.fillRect = fillRect;
            slider.targetGraphic = fillImg;

            // Label Text (TMP)
            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(barGo.transform, false);
            RectTransform labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = new Vector2(-16f, 0f);

            TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 11;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return slider;
        }
    }
}
#endif
