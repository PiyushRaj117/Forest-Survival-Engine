#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using ForestSurvival.UI;

namespace ForestSurvival.EditorTools
{
    public static class SOTF_HUDGenerator
    {
        private const string TexturesFolder = SOTF_TextureGenerator.TexturesFolder;

        [MenuItem("Tools/Forest Survival/Generate Sons of the Forest Style HUD")]
        public static GameObject GenerateSOTFHUD()
        {
            // 1. Ensure textures exist
            if (!File.Exists(TexturesFolder + "/SOTF_Arc_Ring.png"))
            {
                SOTF_TextureGenerator.GenerateAllTextures();
            }

            // 2. Find or create Canvas
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

            // 3. Ensure EventSystem exists
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<InputSystemUIInputModule>();
                Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
            }

            // Remove older HUD instances if present
            Transform existingSotf = canvasGo.transform.Find("SOTF_GPS_Tracker_HUD");
            if (existingSotf != null)
            {
                Undo.DestroyObjectImmediate(existingSotf.gameObject);
            }

            // Also optionally disable/remove old rectangular SurvivalHUD to avoid overlap
            Transform existingOldHud = canvasGo.transform.Find("SurvivalHUD");
            if (existingOldHud != null)
            {
                existingOldHud.gameObject.SetActive(false);
            }

            // 4. Create Main Tracker HUD Root (Bottom-Left corner)
            GameObject hudGo = new GameObject("SOTF_GPS_Tracker_HUD");
            hudGo.transform.SetParent(canvasGo.transform, false);

            RectTransform hudRect = hudGo.AddComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0f, 0f); // Bottom-Left
            hudRect.anchorMax = new Vector2(0f, 0f);
            hudRect.pivot = new Vector2(0f, 0f);
            hudRect.anchoredPosition = new Vector2(45f, 45f);
            hudRect.sizeDelta = new Vector2(250f, 250f);

            // Center of the GPS circle relative to bottom-left root
            Vector2 trackerCenter = new Vector2(125f, 125f);

            // Sprites
            Sprite circleSprite = LoadSprite(TexturesFolder + "/SOTF_Circle_Filled.png");
            Sprite ringSprite = LoadSprite(TexturesFolder + "/SOTF_Arc_Ring.png");
            Sprite notchedBezelSprite = LoadSprite(TexturesFolder + "/SOTF_Notched_Bezel.png");
            Sprite badgeRingSprite = LoadSprite(TexturesFolder + "/SOTF_Badge_Ring.png");
            Sprite badgeBgSprite = LoadSprite(TexturesFolder + "/SOTF_Badge_Bg.png");
            Sprite waterIcon = LoadSprite(TexturesFolder + "/SOTF_Icon_Water.png");
            Sprite meatIcon = LoadSprite(TexturesFolder + "/SOTF_Icon_Meat.png");
            Sprite muscleIcon = LoadSprite(TexturesFolder + "/SOTF_Icon_Muscle.png");
            Sprite moonIcon = LoadSprite(TexturesFolder + "/SOTF_Icon_Moon.png");
            Sprite radarBgSprite = LoadSprite(TexturesFolder + "/SOTF_Tracker_Bg.png");

            // --- Layer 1: Left Notched Bezel (Dark segmented outer border) ---
            CreateImage(hudGo.transform, "NotchedBezel_Left", notchedBezelSprite, new Vector2(236f, 236f), trackerCenter, Color.white);

            // --- Layer 2: Outer Arc Background Track (Dark rim) ---
            CreateImage(hudGo.transform, "Arc_Background_Track", ringSprite, new Vector2(212f, 212f), trackerCenter, new Color(0.1f, 0.11f, 0.12f, 0.95f));

            // --- Layer 3: Health Arc (Left Side, Red #E63946) ---
            Image healthArcImg = CreateRadialImage(hudGo.transform, "Health_Arc", ringSprite, new Vector2(212f, 212f), trackerCenter,
                new Color(0.92f, 0.18f, 0.22f, 1f), clockwise: false, initialFill: 0.5f);

            // --- Layer 4: Stamina Arc (Right Side, Teal/Cyan #00F5D4) ---
            Image staminaArcImg = CreateRadialImage(hudGo.transform, "Stamina_Arc", ringSprite, new Vector2(212f, 212f), trackerCenter,
                new Color(0.0f, 0.96f, 0.83f, 1f), clockwise: true, initialFill: 0.5f);

            // --- Layer 5: Inner GPS / Radar Map Center ---
            CreateImage(hudGo.transform, "Radar_Map_Center", radarBgSprite != null ? radarBgSprite : circleSprite,
                new Vector2(174f, 174f), trackerCenter, Color.white);

            // Player Direction Cursor (Yellow arrow in center)
            CreatePlayerArrow(hudGo.transform, trackerCenter);

            // --- Layer 6: Orbital Badges (Curved along top-right) ---
            // Badge positions arched along top of the circle
            Image muscleRing = CreateOrbitalBadge(hudGo.transform, "Badge_Muscle", trackerCenter + new Vector2(-48f, 118f),
                badgeBgSprite, badgeRingSprite, muscleIcon, new Color(0.92f, 0.22f, 0.26f, 1f));

            Image thirstRing = CreateOrbitalBadge(hudGo.transform, "Badge_Thirst", trackerCenter + new Vector2(4f, 126f),
                badgeBgSprite, badgeRingSprite, waterIcon, new Color(0.0f, 0.85f, 0.95f, 1f));

            Image hungerRing = CreateOrbitalBadge(hudGo.transform, "Badge_Hunger", trackerCenter + new Vector2(56f, 110f),
                badgeBgSprite, badgeRingSprite, meatIcon, new Color(0.98f, 0.55f, 0.16f, 1f));

            Image moonRing = CreateOrbitalBadge(hudGo.transform, "Badge_Moon", trackerCenter + new Vector2(98f, 76f),
                badgeBgSprite, badgeRingSprite, moonIcon, new Color(0.2f, 0.88f, 0.72f, 1f));

            // --- Layer 7: Attach SOTF_SurvivalHUD Component ---
            SOTF_SurvivalHUD controller = hudGo.AddComponent<SOTF_SurvivalHUD>();

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("healthArc").objectReferenceValue = healthArcImg;
            so.FindProperty("staminaArc").objectReferenceValue = staminaArcImg;
            so.FindProperty("hungerRing").objectReferenceValue = hungerRing;
            so.FindProperty("thirstRing").objectReferenceValue = thirstRing;
            so.FindProperty("muscleRing").objectReferenceValue = muscleRing;

            PlayerVitals vitals = Object.FindFirstObjectByType<PlayerVitals>();
            if (vitals != null)
            {
                so.FindProperty("playerVitals").objectReferenceValue = vitals;
            }

            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(hudGo, "Generate SOTF Survival HUD");
            Selection.activeGameObject = hudGo;

            Debug.Log("<color=#00F5D4><b>[SOTF HUD]</b></color> Sons of the Forest style Tracker HUD successfully created!");
            return hudGo;
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 anchoredPos, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Image CreateRadialImage(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 anchoredPos, Color color, bool clockwise, float initialFill)
        {
            Image img = CreateImage(parent, name, sprite, size, anchoredPos, color);
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)Image.Origin360.Top;
            img.fillClockwise = clockwise;
            img.fillAmount = initialFill;
            return img;
        }

        private static Image CreateOrbitalBadge(Transform parent, string name, Vector2 pos, Sprite bgSprite, Sprite ringSprite, Sprite iconSprite, Color ringColor)
        {
            GameObject badgeGo = new GameObject(name);
            badgeGo.transform.SetParent(parent, false);

            RectTransform badgeRt = badgeGo.AddComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0f, 0f);
            badgeRt.anchorMax = new Vector2(0f, 0f);
            badgeRt.pivot = new Vector2(0.5f, 0.5f);
            badgeRt.anchoredPosition = pos;
            badgeRt.sizeDelta = new Vector2(40f, 40f);

            // Dark background disc
            CreateImage(badgeGo.transform, "Bg", bgSprite, new Vector2(36f, 36f), Vector2.zero, new Color(0.12f, 0.13f, 0.15f, 0.95f));

            // Background ring track
            CreateImage(badgeGo.transform, "Track", ringSprite, new Vector2(38f, 38f), Vector2.zero, new Color(0.2f, 0.22f, 0.25f, 0.5f));

            // Radial fill ring
            Image fillRing = CreateRadialImage(badgeGo.transform, "FillRing", ringSprite, new Vector2(38f, 38f), Vector2.zero, ringColor, clockwise: true, initialFill: 1.0f);

            // Center Icon
            CreateImage(badgeGo.transform, "Icon", iconSprite, new Vector2(20f, 20f), Vector2.zero, Color.white);

            return fillRing;
        }

        private static void CreatePlayerArrow(Transform parent, Vector2 centerPos)
        {
            GameObject arrowGo = new GameObject("PlayerArrow");
            arrowGo.transform.SetParent(parent, false);

            RectTransform rt = arrowGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = centerPos;
            rt.sizeDelta = new Vector2(16f, 20f);

            Image img = arrowGo.AddComponent<Image>();
            img.color = new Color(1f, 0.8f, 0.1f, 1f); // Vibrant tactical yellow/orange
            img.raycastTarget = false;
        }
    }
}
#endif
