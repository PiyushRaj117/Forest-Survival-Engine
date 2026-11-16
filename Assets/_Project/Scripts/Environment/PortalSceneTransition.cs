using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;

namespace ForestSurvival.Environment
{
    [RequireComponent(typeof(Collider))]
    public class PortalSceneTransition : MonoBehaviour
    {
        [Header("Scene Settings")]
        [SerializeField] private string targetSceneName = "VikingVillageScene";
        [SerializeField] private string spawnPointName = "VillageSpawnPoint";

        [Header("Cutscene Video Settings")]
        [SerializeField] private string videoFilePath = @"D:\stream YT\Game\Lord-of-Mysteries-SnapYT.App.mp4";
        [SerializeField] private float cutsceneDuration = 78f; // Exactly 1:18 (78 seconds)

        private bool isTransitioning = false;
        private Canvas cutsceneCanvas;
        private RawImage videoDisplay;
        private VideoPlayer videoPlayer;
        private AudioSource audioSource;
        private Text skipPromptText;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isTransitioning) return;
            if (other.CompareTag("Player") || other.GetComponent<CustomPlayerController>() != null)
            {
                StartCoroutine(TransitionRoutine(other.gameObject));
            }
        }

        private IEnumerator TransitionRoutine(GameObject player)
        {
            isTransitioning = true;

            // 1. Lock player controls
            CustomPlayerController controller = player.GetComponent<CustomPlayerController>();
            if (controller != null) controller.enabled = false;

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // 2. Hide HUD
            Canvas[] gameplayCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in gameplayCanvases)
            {
                if (c.name.Contains("Survival") || c.name.Contains("HUD") || c.name.Contains("QuickInventory"))
                {
                    c.enabled = false;
                }
            }

            // Keep player persistent across scene loading
            DontDestroyOnLoad(player);

            // 3. Create Fullscreen Video Overlay
            CreateVideoUI();

            // Prepare & Play Video
            videoPlayer.url = videoFilePath;
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared)
            {
                yield return null;
            }

            videoDisplay.color = Color.white;
            videoPlayer.Play();
            if (audioSource != null) audioSource.Play();

            // 4. Track cutscene playback (exact 78 seconds) & allow skip via SPACE or ESC
            float timer = 0f;
            while (timer < cutsceneDuration && videoPlayer.isPlaying)
            {
                timer += Time.unscaledDeltaTime;

                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
                {
                    break;
                }

                yield return null;
            }

            // Immediately stop & clean up VideoPlayer to avoid black screen hang
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }

            // 5. Load Target Scene Asynchronously
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            yield return null; // Wait 1 frame for scene objects to initialize

            // 6. Teleport player to VillageSpawnPoint & re-enable
            GameObject spawnPoint = GameObject.Find(spawnPointName);
            Vector3 targetPosition = spawnPoint != null ? spawnPoint.transform.position : new Vector3(0.5f, 1.5f, 12f);

            if (player != null)
            {
                player.transform.position = targetPosition;
                if (cc != null) cc.enabled = true;
                if (controller != null) controller.enabled = true;
            }

            // Re-enable Main Camera if disabled
            Camera mainCam = Camera.main;
            if (mainCam != null && !mainCam.enabled)
            {
                mainCam.enabled = true;
            }

            // Restore Gameplay UI Canvases
            foreach (var c in gameplayCanvases)
            {
                if (c != null) c.enabled = true;
            }

            // Destroy Cutscene Canvas completely
            if (cutsceneCanvas != null)
            {
                Destroy(cutsceneCanvas.gameObject);
            }

            isTransitioning = false;
        }

        private void CreateVideoUI()
        {
            GameObject canvasObj = new GameObject("PortalCutsceneCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cutsceneCanvas = canvasObj.GetComponent<Canvas>();
            cutsceneCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cutsceneCanvas.sortingOrder = 9999;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Black Background
            GameObject bgObj = new GameObject("BlackBg", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgObj.GetComponent<Image>().color = Color.black;

            // RawImage for Video
            GameObject rawImgObj = new GameObject("VideoDisplay", typeof(RectTransform), typeof(RawImage));
            rawImgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rawRect = rawImgObj.GetComponent<RectTransform>();
            rawRect.anchorMin = Vector2.zero;
            rawRect.anchorMax = Vector2.one;
            rawRect.sizeDelta = Vector2.zero;
            videoDisplay = rawImgObj.GetComponent<RawImage>();
            videoDisplay.color = Color.clear;

            // Video Player Component
            videoPlayer = canvasObj.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.APIOnly;
            audioSource = canvasObj.AddComponent<AudioSource>();
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, audioSource);

            videoPlayer.sendFrameReadyEvents = true;
            videoPlayer.frameReady += (source, frameIdx) =>
            {
                videoDisplay.texture = source.texture;
            };

            // Skip Prompt Text
            GameObject textObj = new GameObject("SkipPrompt", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(canvasObj.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(1, 0);
            textRect.anchorMax = new Vector2(1, 0);
            textRect.pivot = new Vector2(1, 0);
            textRect.anchoredPosition = new Vector2(-40, 30);
            textRect.sizeDelta = new Vector2(400, 40);

            skipPromptText = textObj.GetComponent<Text>();
            skipPromptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            skipPromptText.fontSize = 18;
            skipPromptText.alignment = TextAnchor.MiddleRight;
            skipPromptText.color = new Color(1f, 1f, 1f, 0.75f);
            skipPromptText.text = "Press SPACE or ESC to Skip";

            DontDestroyOnLoad(canvasObj);
        }
    }
}
