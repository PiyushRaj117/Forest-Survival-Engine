using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ForestSurvival.UI
{
    public class GlobalUIManager : MonoBehaviour
    {
        public static GlobalUIManager Instance { get; private set; }

        [Header("Main Menu Unique Elements")]
        [SerializeField] private GameObject mainButtonsPanel;
        [SerializeField] private GameObject mainBackground;

        [Header("Main Overlay & Panels")]
        [SerializeField] private GameObject pauseMenuOverlay;
        [SerializeField] private GameObject settingsPanel;

        [Header("Settings Tabs")]
        [SerializeField] private GameObject audioTab;
        [SerializeField] private GameObject videoTab;
        [SerializeField] private GameObject controlsTab;

        [Header("Audio Settings UI")]
        [SerializeField] private Slider masterSlider;

        public bool IsPaused { get; private set; } = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CleanupEventSystems();

            // Disable legacy pause overlay if NeonPauseMenuUI exists
            if (pauseMenuOverlay != null) pauseMenuOverlay.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            if (scene.name == "MainMenu")
            {
                Time.timeScale = 1f;
                IsPaused = false;

                if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
                if (mainBackground != null) mainBackground.SetActive(true);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
                if (mainBackground != null) mainBackground.SetActive(false);

                Time.timeScale = 1f;
                IsPaused = false;

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void CleanupEventSystems()
        {
            EventSystem[] systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (systems.Length > 1)
            {
                for (int i = 1; i < systems.Length; i++)
                {
                    Destroy(systems[i].gameObject);
                }
            }
        }

        private void Update()
        {
            // If NeonPauseMenuUI handles ESC, suppress legacy overlay
            if (NeonPauseMenuUI.Instance != null)
            {
                if (pauseMenuOverlay != null && pauseMenuOverlay.activeSelf)
                {
                    pauseMenuOverlay.SetActive(false);
                }
                return;
            }

            string currentScene = SceneManager.GetActiveScene().name;

            if (currentScene != "MainMenu" && Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsPaused)
                {
                    ResumeGame();
                }
                else
                {
                    PauseGame();
                }
            }
        }

        public void PauseGame()
        {
            if (NeonPauseMenuUI.Instance != null)
            {
                NeonPauseMenuUI.Instance.PauseGame();
                if (pauseMenuOverlay != null) pauseMenuOverlay.SetActive(false);
                return;
            }

            IsPaused = true;
            Time.timeScale = 0f;
            if (pauseMenuOverlay != null) pauseMenuOverlay.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ResumeGame()
        {
            if (NeonPauseMenuUI.Instance != null && NeonPauseMenuUI.Instance.IsPaused)
            {
                NeonPauseMenuUI.Instance.ResumeGame();
            }

            IsPaused = false;
            Time.timeScale = 1f;
            if (pauseMenuOverlay != null) pauseMenuOverlay.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void OpenSettings()
        {
            if (NeonPauseMenuUI.Instance != null)
            {
                NeonPauseMenuUI.Instance.OpenFromMainMenu();
                return;
            }

            if (settingsPanel != null) settingsPanel.SetActive(true);
            ShowAudioTab();
        }

        public void CloseSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void ShowAudioTab()
        {
            if (audioTab != null) audioTab.SetActive(true);
            if (videoTab != null) videoTab.SetActive(false);
            if (controlsTab != null) controlsTab.SetActive(false);
        }

        public void ShowVideoTab()
        {
            if (audioTab != null) audioTab.SetActive(false);
            if (videoTab != null) videoTab.SetActive(true);
            if (controlsTab != null) controlsTab.SetActive(false);
        }

        public void ShowControlsTab()
        {
            if (audioTab != null) audioTab.SetActive(false);
            if (videoTab != null) videoTab.SetActive(false);
            if (controlsTab != null) controlsTab.SetActive(true);
        }

        public void SetMasterVolume(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat("MasterVolume", value);
        }

        public void ReturnToMainMenuFromGame()
        {
            Time.timeScale = 1f;
            IsPaused = false;

            if (pauseMenuOverlay != null) pauseMenuOverlay.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SceneManager.LoadScene("MainMenu");
        }
    }
}
