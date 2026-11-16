using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using ForestSurvival.UI;

public static class MainMenuSceneRebuilder
{
    [MenuItem("Tools/Rebuild Forest Survival Main Menu", priority = 2)]
    public static void Rebuild()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.skybox = null;
        RenderSettings.ambientLight = new Color(0.08f, 0.12f, 0.16f);

        // 1. Camera
        var camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        var cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.05f, 0.08f);

        // 2. Core Managers
        new GameObject("AudioManager", typeof(AudioManager));
        new GameObject("GlobalUIHolder", typeof(GlobalUIManager));
        var menuMgrObj = new GameObject("MainMenuManager", typeof(MainMenuManager));
        var menuMgr = menuMgrObj.GetComponent<MainMenuManager>();

        // 3. EventSystem
        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

        // 4. MainMenuCanvas
        var canvasObj = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Dark Forest Background
        var bgObj = CreateUIElement("Background", canvasObj.transform);
        StretchFull(bgObj.GetComponent<RectTransform>());
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.07f, 0.10f, 1f);

        // Title: FOREST SURVIVAL
        var titleObj = CreateUIElement("TitleText", canvasObj.transform);
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.72f);
        titleRect.anchorMax = new Vector2(0.5f, 0.72f);
        titleRect.sizeDelta = new Vector2(800, 100);
        var titleText = titleObj.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 54;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.text = "FOREST SURVIVAL";
        titleText.color = new Color(0f, 0.95f, 1f, 1f);
        var titleOutline = titleObj.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0f, 0.35f, 0.5f, 0.8f);
        titleOutline.effectDistance = new Vector2(2, -2);

        // Subtitle
        var subObj = CreateUIElement("SubtitleText", canvasObj.transform);
        var subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 0.64f);
        subRect.anchorMax = new Vector2(0.5f, 0.64f);
        subRect.sizeDelta = new Vector2(600, 40);
        var subText = subObj.AddComponent<Text>();
        subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subText.fontSize = 18;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.text = "ISLAND ADVENTURE & SURVIVAL ENGINE";
        subText.color = new Color(0.65f, 0.75f, 0.85f, 0.8f);

        // Button Group Container
        var btnGroupObj = CreateUIElement("ButtonsGroup", canvasObj.transform);
        var btnGroupRect = btnGroupObj.GetComponent<RectTransform>();
        btnGroupRect.anchorMin = new Vector2(0.5f, 0.38f);
        btnGroupRect.anchorMax = new Vector2(0.5f, 0.38f);
        btnGroupRect.sizeDelta = new Vector2(340, 220);

        // Buttons: PLAY, SETTINGS, QUIT
        CreateMenuButton("PlayButton", "PLAY", new Vector2(0, 70), btnGroupObj.transform, () => menuMgr.PlayGame());
        CreateMenuButton("SettingsButton", "SETTINGS", new Vector2(0, 0), btnGroupObj.transform, () => menuMgr.OpenSettings());
        CreateMenuButton("QuitButton", "QUIT", new Vector2(0, -70), btnGroupObj.transform, () => menuMgr.QuitGame());

        // 5. LoadingCanvas
        var loadingCanvasObj = new GameObject("LoadingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(SceneLoader));
        var loadCanvas = loadingCanvasObj.GetComponent<Canvas>();
        loadCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        loadCanvas.sortingOrder = 100;
        var loadScaler = loadingCanvasObj.GetComponent<CanvasScaler>();
        loadScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        loadScaler.referenceResolution = new Vector2(1920, 1080);

        var loadBg = CreateUIElement("LoadingBg", loadingCanvasObj.transform);
        StretchFull(loadBg.GetComponent<RectTransform>());
        var loadBgImg = loadBg.AddComponent<Image>();
        loadBgImg.color = new Color(0.02f, 0.04f, 0.07f, 0.98f);

        var loadTextObj = CreateUIElement("LoadingText", loadingCanvasObj.transform);
        var loadTextRect = loadTextObj.GetComponent<RectTransform>();
        loadTextRect.anchorMin = new Vector2(0.5f, 0.52f);
        loadTextRect.anchorMax = new Vector2(0.5f, 0.52f);
        loadTextRect.sizeDelta = new Vector2(500, 60);
        var loadText = loadTextObj.AddComponent<Text>();
        loadText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        loadText.fontSize = 28;
        loadText.fontStyle = FontStyle.Bold;
        loadText.alignment = TextAnchor.MiddleCenter;
        loadText.text = "LOADING FOREST...";
        loadText.color = new Color(0f, 0.95f, 1f, 1f);

        // Progress Bar Frame
        var barFrame = CreateUIElement("ProgressBarFrame", loadingCanvasObj.transform);
        var frameRect = barFrame.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 0.44f);
        frameRect.anchorMax = new Vector2(0.5f, 0.44f);
        frameRect.sizeDelta = new Vector2(520, 24);
        var frameImg = barFrame.AddComponent<Image>();
        frameImg.color = new Color(0.08f, 0.12f, 0.18f, 1f);
        var frameOutline = barFrame.AddComponent<Outline>();
        frameOutline.effectColor = new Color(0f, 0.8f, 1f, 0.5f);

        var barFill = CreateUIElement("ProgressBarFill", barFrame.transform);
        var fillRect = barFill.GetComponent<RectTransform>();
        StretchFull(fillRect);
        var fillImg = barFill.AddComponent<Image>();
        fillImg.color = new Color(0f, 0.95f, 1f, 1f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0.65f;

        loadingCanvasObj.SetActive(false);

        // Save Scene
        string scenePath = "Assets/_Project/Scenes/MainMenu.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("<color=green>[MainMenuRebuilder]</color> Forest Survival MainMenu reconstructed successfully!");
    }

    private static GameObject CreateUIElement(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    private static void CreateMenuButton(string name, string label, Vector2 pos, Transform parent, UnityEngine.Events.UnityAction action)
    {
        var btnObj = CreateUIElement(name, parent);
        var rect = btnObj.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(300, 52);

        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.08f, 0.14f, 0.22f, 0.92f);
        var outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0.85f, 1f, 0.55f);
        outline.effectDistance = new Vector2(1.5f, 1.5f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.highlightedColor = new Color(0f, 0.85f, 1f, 0.35f);
        cb.pressedColor = new Color(0f, 0.6f, 0.85f, 0.6f);
        btn.colors = cb;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);

        var textObj = CreateUIElement("Text", btnObj.transform);
        StretchFull(textObj.GetComponent<RectTransform>());
        var text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = label;
        text.color = Color.white;
    }
}
