using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject initialLoadingPanel;
    public GameObject mainButtonsPanel;
    public GameObject settingsPanel;

    [Header("Loading UI")]
    public VideoPlayer videoPlayer;
    public VideoClip[] videos;
    public Image progressFill;
    public TMP_Text progressText;
    public float loadingDuration = 10.0f;

    [Header("Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button quitButton;
    public Button backButton;

    private void Start()
    {
        // 0. Ensure time and cursor are correct for menu
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 1. Initial panel states
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (initialLoadingPanel != null) initialLoadingPanel.SetActive(true);

        // 2. Setup video & loading sequence
        PlayRandomVideo();
        StartCoroutine(LoadingRoutine());

        // 3. Bind buttons
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(PlayGame);
        }
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OpenSettings);
        }
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(CloseSettings);
        }
    }

    private void PlayRandomVideo()
    {
        if (videoPlayer != null && videos != null && videos.Length > 0)
        {
            int index = Random.Range(0, videos.Length);
            videoPlayer.clip = videos[index];
            videoPlayer.isLooping = true;
            videoPlayer.Play();
        }
    }

    private IEnumerator LoadingRoutine()
    {
        float timer = 0f;
        while (timer < loadingDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / loadingDuration);

            if (progressFill != null)
                progressFill.fillAmount = progress;

            if (progressText != null)
                progressText.text = $"LOADING FOREST... {Mathf.RoundToInt(progress * 100)}%";

            yield return null;
        }

        if (initialLoadingPanel != null) initialLoadingPanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
    }

    public void PlayGame()
    {
        Debug.Log("[MainMenu] Button triggered: PlayGame");
        SceneManager.LoadScene("SampleScene");
    }

    public void OpenSettings()
    {
        Debug.Log("[MainMenu] Button triggered: OpenSettings");
        if (settingsPanel != null)
        {
            if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        Debug.Log("[MainMenu] Button triggered: CloseSettings");
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("[MainMenu] Button triggered: QuitGame");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
