using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ForestSurvival.UI
{
    public class NeonPauseMenuUI : MonoBehaviour
    {
        public static NeonPauseMenuUI Instance { get; private set; }

        [Header("Menu State")]
        [SerializeField] private bool isPaused = false;
        public bool IsPaused => isPaused;

        // UI Roots
        private GameObject menuRoot;

        // Tabs Content Panels
        private GameObject audioContentPanel;
        private GameObject videoContentPanel;
        private GameObject controlsContentPanel;
        private GameObject gameplayContentPanel;

        // Tab Images
        private Image imgTabAudio;
        private Image imgTabVideo;
        private Image imgTabControls;
        private Image imgTabGameplay;

        // Audio Sliders
        private Slider sliderMaster;
        private Slider sliderMusic;
        private Slider sliderSFX;
        private Slider sliderVoice;

        private Text txtMasterVal;
        private Text txtMusicVal;
        private Text txtSFXVal;
        private Text txtVoiceVal;

        // Bottom Action Buttons
        private Button btnResume;
        private Button btnMainMenu;
        private Text txtResumeBtn;

        // Colors matching Reference Image
        private readonly Color colNeonCyan = new Color(0f, 0.95f, 1f, 1f);
        private readonly Color colDarkBg = new Color(0.04f, 0.06f, 0.12f, 0.96f);
        private readonly Color colTabActive = new Color(0.08f, 0.16f, 0.30f, 0.95f);
        private readonly Color colTabInactive = new Color(0.06f, 0.08f, 0.14f, 0.85f);
        private readonly Color colCardBg = new Color(0.08f, 0.11f, 0.19f, 0.88f);
        private readonly Color colSliderFill = new Color(0f, 0.82f, 0.95f, 1f);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            EnsureUIGenerated();
            CloseMenuInstant();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            isPaused = false;
            Time.timeScale = 1f;

            // Remove any legacy pause menu in scene
            HideLegacyPauseOverlays();

            EnsureUIGenerated();
            CloseMenuInstant();

            if (scene.name == "MainMenu")
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (txtResumeBtn != null) txtResumeBtn.text = "BACK TO MENU";
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                if (txtResumeBtn != null) txtResumeBtn.text = "RESUME GAME";
            }
        }

        private void HideLegacyPauseOverlays()
        {
            // Find old raw PauseMenuOverlay or PauseMenu if present in canvas and disable it
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                Transform oldPause = c.transform.Find("PauseMenuOverlay");
                if (oldPause != null) oldPause.gameObject.SetActive(false);

                Transform oldPause2 = c.transform.Find("PauseMenu");
                if (oldPause2 != null) oldPause2.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            string currentScene = SceneManager.GetActiveScene().name;

            // ESC toggles menu in gameplay scenes
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentScene != "MainMenu")
                {
                    if (isPaused)
                    {
                        ResumeGame();
                    }
                    else
                    {
                        PauseGame();
                    }
                }
                else
                {
                    if (menuRoot != null && menuRoot.activeSelf)
                    {
                        CloseMenu();
                    }
                }
            }
        }

        public void PauseGame()
        {
            HideLegacyPauseOverlays();
            EnsureUIGenerated();
            isPaused = true;
            Time.timeScale = 0f;

            if (menuRoot != null) menuRoot.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SelectTab(0);
        }

        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;

            if (menuRoot != null) menuRoot.SetActive(false);

            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != "MainMenu")
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void OpenFromMainMenu()
        {
            HideLegacyPauseOverlays();
            EnsureUIGenerated();
            if (txtResumeBtn != null) txtResumeBtn.text = "BACK TO MENU";
            if (menuRoot != null) menuRoot.SetActive(true);
            SelectTab(0);
        }

        public void CloseMenu()
        {
            ResumeGame();
        }

        private void CloseMenuInstant()
        {
            if (menuRoot != null) menuRoot.SetActive(false);
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            isPaused = false;
            if (menuRoot != null) menuRoot.SetActive(false);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SceneManager.LoadScene("MainMenu");
        }

        #region Tab Navigation
        public void SelectTab(int index)
        {
            if (audioContentPanel != null) audioContentPanel.SetActive(index == 0);
            if (videoContentPanel != null) videoContentPanel.SetActive(index == 1);
            if (controlsContentPanel != null) controlsContentPanel.SetActive(index == 2);
            if (gameplayContentPanel != null) gameplayContentPanel.SetActive(index == 3);

            UpdateTabHighlight(imgTabAudio, index == 0);
            UpdateTabHighlight(imgTabVideo, index == 1);
            UpdateTabHighlight(imgTabControls, index == 2);
            UpdateTabHighlight(imgTabGameplay, index == 3);
        }

        private void UpdateTabHighlight(Image tabImg, bool isActive)
        {
            if (tabImg == null) return;
            tabImg.color = isActive ? colTabActive : colTabInactive;
            Outline outline = tabImg.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = isActive;
                outline.effectColor = colNeonCyan;
            }
        }
        #endregion

        #region Settings Callbacks
        public void OnMasterVolumeChanged(float val)
        {
            AudioListener.volume = val;
            PlayerPrefs.SetFloat("MasterVolume", val);
            if (txtMasterVal != null) txtMasterVal.text = Mathf.RoundToInt(val * 100f) + "%";
        }

        public void OnMusicVolumeChanged(float val)
        {
            PlayerPrefs.SetFloat("MusicVolume", val);
            if (txtMusicVal != null) txtMusicVal.text = Mathf.RoundToInt(val * 100f) + "%";
        }

        public void OnSFXVolumeChanged(float val)
        {
            PlayerPrefs.SetFloat("SFXVolume", val);
            if (txtSFXVal != null) txtSFXVal.text = Mathf.RoundToInt(val * 100f) + "%";
        }

        public void OnVoiceVolumeChanged(float val)
        {
            PlayerPrefs.SetFloat("VoiceVolume", val);
            if (txtVoiceVal != null) txtVoiceVal.text = Mathf.RoundToInt(val * 100f) + "%";
        }

        public void SetQualityLevel(int level)
        {
            QualitySettings.SetQualityLevel(level, true);
        }

        public void ToggleFullscreen(bool isFull)
        {
            Screen.fullScreen = isFull;
        }
        #endregion

        #region Runtime UI Construction
        public void EnsureUIGenerated()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Rebuild if layout needs refresh
            Transform existing = canvas.transform.Find("Neon_Settings_Menu_Starter");
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 1. Fullscreen Dimmer Overlay
            menuRoot = new GameObject("Neon_Settings_Menu_Starter", typeof(RectTransform));
            menuRoot.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = menuRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;

            Image dimmer = menuRoot.AddComponent<Image>();
            dimmer.color = new Color(0.02f, 0.03f, 0.07f, 0.78f);

            // 2. Main Neon Settings Container (860 x 530)
            GameObject panel = new GameObject("NeonContainer", typeof(RectTransform));
            panel.transform.SetParent(menuRoot.transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(860f, 530f);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = colDarkBg;

            Outline neonGlow = panel.AddComponent<Outline>();
            neonGlow.effectColor = new Color(0f, 0.95f, 1f, 0.9f);
            neonGlow.effectDistance = new Vector2(2f, 2f);

            // 3. Header Title: "SETTINGS & PAUSE MENU"
            GameObject titleObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(panel.transform, false);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -14f);
            titleRect.sizeDelta = new Vector2(0f, 36f);

            Text titleTxt = titleObj.GetComponent<Text>();
            titleTxt.font = font;
            titleTxt.fontSize = 19;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.text = "SETTINGS & PAUSE MENU";
            titleTxt.color = Color.white;

            // 4. Left Tab Column: Perfectly positioned inside panel using VerticalLayoutGroup
            GameObject tabsCol = new GameObject("TabsColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
            tabsCol.transform.SetParent(panel.transform, false);
            RectTransform tabsColRect = tabsCol.GetComponent<RectTransform>();
            tabsColRect.anchorMin = new Vector2(0f, 0f);
            tabsColRect.anchorMax = new Vector2(0f, 1f);
            tabsColRect.pivot = new Vector2(0f, 1f);
            tabsColRect.offsetMin = new Vector2(28f, 90f);
            tabsColRect.offsetMax = new Vector2(200f, -65f);

            VerticalLayoutGroup vlg = tabsCol.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            CreateTabButton(tabsCol.transform, "Audio", 0, out imgTabAudio);
            CreateTabButton(tabsCol.transform, "Video", 1, out imgTabVideo);
            CreateTabButton(tabsCol.transform, "Controls", 2, out imgTabControls);
            CreateTabButton(tabsCol.transform, "Gameplay", 3, out imgTabGameplay);

            // 5. Right Content Area: Perfectly placed inside panel
            GameObject contentArea = new GameObject("ContentArea", typeof(RectTransform));
            contentArea.transform.SetParent(panel.transform, false);
            RectTransform contentRect = contentArea.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.offsetMin = new Vector2(220f, 90f);
            contentRect.offsetMax = new Vector2(-28f, -65f);

            // --- TAB 1: AUDIO ---
            audioContentPanel = new GameObject("AudioTabContent", typeof(RectTransform));
            audioContentPanel.transform.SetParent(contentArea.transform, false);
            RectTransform aRect = audioContentPanel.GetComponent<RectTransform>();
            aRect.anchorMin = Vector2.zero;
            aRect.anchorMax = Vector2.one;
            aRect.sizeDelta = Vector2.zero;

            sliderMaster = CreateSliderCard(audioContentPanel.transform, "Master", 0f, 0f, OnMasterVolumeChanged, out txtMasterVal);
            sliderMusic = CreateSliderCard(audioContentPanel.transform, "Music", 305f, 0f, OnMusicVolumeChanged, out txtMusicVal);
            sliderSFX = CreateSliderCard(audioContentPanel.transform, "SFX", 0f, -100f, OnSFXVolumeChanged, out txtSFXVal);
            sliderVoice = CreateSliderCard(audioContentPanel.transform, "Voice", 305f, -100f, OnVoiceVolumeChanged, out txtVoiceVal);

            CreateToggleSwitch(audioContentPanel.transform, "Mute in Background", 0f, -205f);
            CreateToggleSwitch(audioContentPanel.transform, "UI Sounds", 305f, -205f);

            // --- TAB 2: VIDEO ---
            videoContentPanel = new GameObject("VideoTabContent", typeof(RectTransform));
            videoContentPanel.transform.SetParent(contentArea.transform, false);
            RectTransform vRect = videoContentPanel.GetComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;
            vRect.sizeDelta = Vector2.zero;
            CreateVideoSettings(videoContentPanel.transform);

            // --- TAB 3: CONTROLS ---
            controlsContentPanel = new GameObject("ControlsTabContent", typeof(RectTransform));
            controlsContentPanel.transform.SetParent(contentArea.transform, false);
            RectTransform cRect = controlsContentPanel.GetComponent<RectTransform>();
            cRect.anchorMin = Vector2.zero;
            cRect.anchorMax = Vector2.one;
            cRect.sizeDelta = Vector2.zero;
            CreateControlsInfo(controlsContentPanel.transform);

            // --- TAB 4: GAMEPLAY ---
            gameplayContentPanel = new GameObject("GameplayTabContent", typeof(RectTransform));
            gameplayContentPanel.transform.SetParent(contentArea.transform, false);
            RectTransform gRect = gameplayContentPanel.GetComponent<RectTransform>();
            gRect.anchorMin = Vector2.zero;
            gRect.anchorMax = Vector2.one;
            gRect.sizeDelta = Vector2.zero;
            CreateGameplayInfo(gameplayContentPanel.transform);

            // 6. Bottom Action Bar
            GameObject bottomBar = new GameObject("BottomBar", typeof(RectTransform));
            bottomBar.transform.SetParent(panel.transform, false);
            RectTransform botRect = bottomBar.GetComponent<RectTransform>();
            botRect.anchorMin = new Vector2(0f, 0f);
            botRect.anchorMax = new Vector2(1f, 0f);
            botRect.pivot = new Vector2(0.5f, 0f);
            botRect.anchoredPosition = new Vector2(0f, 22f);
            botRect.sizeDelta = new Vector2(0f, 48f);

            btnResume = CreateBottomButton(bottomBar.transform, "RESUME GAME", new Vector2(130f, 0f), colNeonCyan, new Color(0f, 0.18f, 0.32f, 1f), ResumeGame, out txtResumeBtn);
            btnMainMenu = CreateBottomButton(bottomBar.transform, "MAIN MENU", new Vector2(-130f, 0f), new Color(1f, 0.35f, 0.45f, 0.9f), new Color(0.24f, 0.06f, 0.10f, 1f), ReturnToMainMenu, out _);

            // Init values
            float savedMaster = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
            if (sliderMaster != null) sliderMaster.value = savedMaster;
            if (sliderMusic != null) sliderMusic.value = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
            if (sliderSFX != null) sliderSFX.value = PlayerPrefs.GetFloat("SFXVolume", 0.9f);
            if (sliderVoice != null) sliderVoice.value = PlayerPrefs.GetFloat("VoiceVolume", 0.85f);

            SelectTab(0);
        }

        private Button CreateTabButton(Transform parent, string label, int tabIndex, out Image tabImg)
        {
            GameObject obj = new GameObject($"Tab_{label}", typeof(RectTransform), typeof(Button), typeof(Image), typeof(LayoutElement));
            obj.transform.SetParent(parent, false);

            LayoutElement le = obj.GetComponent<LayoutElement>();
            le.preferredHeight = 44f;
            le.minHeight = 44f;

            tabImg = obj.GetComponent<Image>();
            tabImg.color = colTabInactive;

            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = colNeonCyan;
            outline.effectDistance = new Vector2(1.5f, 1.5f);
            outline.enabled = false;

            Button btn = obj.GetComponent<Button>();
            btn.onClick.AddListener(() => SelectTab(tabIndex));

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(obj.transform, false);
            RectTransform tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(16f, 0f);
            tRect.offsetMax = Vector2.zero;

            Text txt = textObj.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.text = label;
            txt.color = Color.white;

            return btn;
        }

        private Slider CreateSliderCard(Transform parent, string title, float xPos, float yPos, UnityEngine.Events.UnityAction<float> callback, out Text valTxt)
        {
            GameObject card = new GameObject($"Card_{title}", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);

            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(xPos, yPos);
            rect.sizeDelta = new Vector2(285f, 85f);

            Image cardImg = card.GetComponent<Image>();
            cardImg.color = colCardBg;

            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.12f, 0.22f, 0.38f, 0.7f);
            outline.effectDistance = new Vector2(1f, 1f);

            // Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(card.transform, false);
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 1f);
            tRect.anchorMax = new Vector2(0.6f, 1f);
            tRect.pivot = new Vector2(0f, 1f);
            tRect.anchoredPosition = new Vector2(14f, -10f);
            tRect.sizeDelta = new Vector2(0f, 22f);

            Text txt = titleObj.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 14;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.text = title;
            txt.color = Color.white;

            // Value text (e.g. 100%)
            GameObject valObj = new GameObject("ValText", typeof(RectTransform), typeof(Text));
            valObj.transform.SetParent(card.transform, false);
            RectTransform vRect = valObj.GetComponent<RectTransform>();
            vRect.anchorMin = new Vector2(0.6f, 1f);
            vRect.anchorMax = new Vector2(1f, 1f);
            vRect.pivot = new Vector2(1f, 1f);
            vRect.anchoredPosition = new Vector2(-14f, -10f);
            vRect.sizeDelta = new Vector2(0f, 22f);

            valTxt = valObj.GetComponent<Text>();
            valTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            valTxt.fontSize = 13;
            valTxt.alignment = TextAnchor.MiddleRight;
            valTxt.text = "100%";
            valTxt.color = colNeonCyan;

            // Slider Bar
            GameObject sliderObj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(card.transform, false);
            RectTransform sRect = sliderObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 0f);
            sRect.anchorMax = new Vector2(1f, 0f);
            sRect.pivot = new Vector2(0.5f, 0f);
            sRect.anchoredPosition = new Vector2(0f, 16f);
            sRect.sizeDelta = new Vector2(-28f, 16f);

            // Slider Background
            GameObject sBg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            sBg.transform.SetParent(sliderObj.transform, false);
            RectTransform sBgRect = sBg.GetComponent<RectTransform>();
            sBgRect.anchorMin = new Vector2(0f, 0.35f);
            sBgRect.anchorMax = new Vector2(1f, 0.65f);
            sBgRect.sizeDelta = Vector2.zero;
            sBg.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.25f, 1f);

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRect = fillArea.GetComponent<RectTransform>();
            faRect.anchorMin = new Vector2(0f, 0.35f);
            faRect.anchorMax = new Vector2(1f, 0.65f);
            faRect.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fRect = fill.GetComponent<RectTransform>();
            fRect.sizeDelta = Vector2.zero;
            Image fillImg = fill.GetComponent<Image>();
            fillImg.color = colSliderFill;

            // Handle
            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform haRect = handleArea.GetComponent<RectTransform>();
            haRect.anchorMin = Vector2.zero;
            haRect.anchorMax = Vector2.one;
            haRect.sizeDelta = Vector2.zero;

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform hRect = handle.GetComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(18f, 18f);
            Image hImg = handle.GetComponent<Image>();
            hImg.color = Color.white;
            Outline hOutline = handle.AddComponent<Outline>();
            hOutline.effectColor = colNeonCyan;

            Slider slider = sliderObj.GetComponent<Slider>();
            slider.fillRect = fRect;
            slider.handleRect = hRect;
            slider.targetGraphic = hImg;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.onValueChanged.AddListener(callback);

            return slider;
        }

        private void CreateToggleSwitch(Transform parent, string title, float xPos, float yPos)
        {
            GameObject card = new GameObject($"Toggle_{title}", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);

            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(xPos, yPos);
            rect.sizeDelta = new Vector2(285f, 48f);

            card.GetComponent<Image>().color = colCardBg;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(card.transform, false);
            RectTransform tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 0f);
            tRect.anchorMax = new Vector2(0.7f, 1f);
            tRect.offsetMin = new Vector2(14f, 0f);
            tRect.offsetMax = Vector2.zero;

            Text txt = textObj.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.text = title;
            txt.color = Color.white;

            GameObject toggleObj = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
            toggleObj.transform.SetParent(card.transform, false);
            RectTransform togRect = toggleObj.GetComponent<RectTransform>();
            togRect.anchorMin = new Vector2(1f, 0.5f);
            togRect.anchorMax = new Vector2(1f, 0.5f);
            togRect.pivot = new Vector2(1f, 0.5f);
            togRect.anchoredPosition = new Vector2(-14f, 0f);
            togRect.sizeDelta = new Vector2(46f, 24f);

            GameObject bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(toggleObj.transform, false);
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0f, 0.7f, 0.9f, 0.8f);

            GameObject check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(bg.transform, false);
            RectTransform chkRect = check.GetComponent<RectTransform>();
            chkRect.sizeDelta = new Vector2(20f, 20f);
            chkRect.anchoredPosition = new Vector2(11f, 0f);
            check.GetComponent<Image>().color = Color.white;

            Toggle toggle = toggleObj.GetComponent<Toggle>();
            toggle.targetGraphic = bgImg;
            toggle.graphic = check.GetComponent<Image>();
            toggle.isOn = true;
        }

        private void CreateVideoSettings(Transform parent)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject title = new GameObject("VidTitle", typeof(RectTransform), typeof(Text));
            title.transform.SetParent(parent, false);
            RectTransform tr = title.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0, 1);
            tr.anchorMax = new Vector2(1, 1);
            tr.pivot = new Vector2(0, 1);
            tr.anchoredPosition = new Vector2(0, 0);
            tr.sizeDelta = new Vector2(0, 30);
            Text t = title.GetComponent<Text>();
            t.font = font;
            t.fontSize = 16;
            t.fontStyle = FontStyle.Bold;
            t.text = "GRAPHICS & DISPLAY";
            t.color = colNeonCyan;

            CreateToggleSwitch(parent, "Fullscreen Mode", 0f, -40f);
            CreateToggleSwitch(parent, "V-Sync", 305f, -40f);
            CreateToggleSwitch(parent, "Anti-Aliasing", 0f, -100f);
            CreateToggleSwitch(parent, "Dynamic Shadows", 305f, -100f);
        }

        private void CreateControlsInfo(Transform parent)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            string[] keys = new string[] {
                "W, A, S, D", "Move / Walk (Smooth Speed)",
                "Left Shift", "Fast Sprint (Uses Stamina)",
                "Spacebar", "Jump Obstacles",
                "Left Click", "Attack / Sword Swing Combo",
                "1, 2, 3, 4, 5, 6", "Use Hotbar Inventory Items",
                "Escape (ESC)", "Pause & Settings Menu"
            };

            for (int i = 0; i < keys.Length / 2; i++)
            {
                GameObject card = new GameObject($"Ctrl_{i}", typeof(RectTransform), typeof(Image));
                card.transform.SetParent(parent, false);
                RectTransform r = card.GetComponent<RectTransform>();
                r.anchorMin = new Vector2(0f, 1f);
                r.anchorMax = new Vector2(1f, 1f);
                r.pivot = new Vector2(0f, 1f);
                r.anchoredPosition = new Vector2(0f, -(i * 44f));
                r.sizeDelta = new Vector2(0f, 38f);

                card.GetComponent<Image>().color = colCardBg;

                // Key Badge
                GameObject kObj = new GameObject("Key", typeof(RectTransform), typeof(Text));
                kObj.transform.SetParent(card.transform, false);
                RectTransform kr = kObj.GetComponent<RectTransform>();
                kr.anchorMin = new Vector2(0, 0);
                kr.anchorMax = new Vector2(0.35f, 1);
                kr.offsetMin = new Vector2(12, 0);
                kr.offsetMax = Vector2.zero;
                Text kt = kObj.GetComponent<Text>();
                kt.font = font;
                kt.fontSize = 13;
                kt.fontStyle = FontStyle.Bold;
                kt.alignment = TextAnchor.MiddleLeft;
                kt.text = keys[i * 2];
                kt.color = colNeonCyan;

                // Action
                GameObject aObj = new GameObject("Action", typeof(RectTransform), typeof(Text));
                aObj.transform.SetParent(card.transform, false);
                RectTransform ar = aObj.GetComponent<RectTransform>();
                ar.anchorMin = new Vector2(0.35f, 0);
                ar.anchorMax = new Vector2(1, 1);
                ar.offsetMin = new Vector2(10, 0);
                ar.offsetMax = new Vector2(-10, 0);
                Text at = aObj.GetComponent<Text>();
                at.font = font;
                at.fontSize = 13;
                at.alignment = TextAnchor.MiddleLeft;
                at.text = keys[i * 2 + 1];
                at.color = Color.white;
            }
        }

        private void CreateGameplayInfo(Transform parent)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject descObj = new GameObject("Desc", typeof(RectTransform), typeof(Text));
            descObj.transform.SetParent(parent, false);
            RectTransform r = descObj.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(10, 10);
            r.offsetMax = new Vector2(-10, -10);

            Text t = descObj.GetComponent<Text>();
            t.font = font;
            t.fontSize = 14;
            t.text = "SURVIVAL RULES & TIPS:\n\n" +
                     "• Watch your Thirst (Blue) & Hunger (Orange) vitals on the tracker.\n" +
                     "• Drink water bottles (Slot 2) or eat berries (Slot 3) to survive.\n" +
                     "• Hostile Chomper beasts patrol the forest. Check the floating health bars above their heads to track their remaining HP!\n" +
                     "• Night falls gradually - seek shelter or stay alert under moonlight.";
            t.color = new Color(0.85f, 0.9f, 0.95f, 1f);
        }

        private Button CreateBottomButton(Transform parent, string label, Vector2 pos, Color borderColor, Color bgCol, UnityEngine.Events.UnityAction action, out Text btnTxt)
        {
            GameObject obj = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Button), typeof(Image));
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(210f, 44f);
            rect.anchoredPosition = pos;

            Image img = obj.GetComponent<Image>();
            img.color = bgCol;

            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(1.5f, 1.5f);

            Button btn = obj.GetComponent<Button>();
            btn.onClick.AddListener(action);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(obj.transform, false);
            RectTransform tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            btnTxt = textObj.GetComponent<Text>();
            btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnTxt.fontSize = 14;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.text = label;
            btnTxt.color = Color.white;

            return btn;
        }
        #endregion
    }
}
