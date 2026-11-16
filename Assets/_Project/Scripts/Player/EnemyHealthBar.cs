using UnityEngine;
using UnityEngine.UI;

namespace ForestSurvival.Combat
{
    public class EnemyHealthBar : MonoBehaviour
    {
        [Header("Offset")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.85f, 0f);
        [SerializeField] private float maxVisibleDistance = 25f;

        private EnemyAI enemyAI;
        private GameObject healthBarCanvas;
        private Image fillImage;
        private Text hpText;
        private CanvasGroup canvasGroup;
        private float targetFill = 1f;
        private Camera mainCam;

        private void Start()
        {
            enemyAI = GetComponent<EnemyAI>();
            mainCam = Camera.main;

            CreateBillboardHealthBar();
        }

        private void CreateBillboardHealthBar()
        {
            healthBarCanvas = new GameObject("FloatingHealthBarCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            healthBarCanvas.transform.SetParent(transform, false);
            healthBarCanvas.transform.localPosition = worldOffset;

            Canvas c = healthBarCanvas.GetComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.sortingOrder = 50;

            canvasGroup = healthBarCanvas.GetComponent<CanvasGroup>();

            RectTransform canvasRect = healthBarCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1.6f, 0.35f);
            canvasRect.localScale = Vector3.one * 0.85f;

            // Background Frame
            GameObject bgObj = new GameObject("Bg", typeof(RectTransform), typeof(Image), typeof(Outline));
            bgObj.transform.SetParent(healthBarCanvas.transform, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.color = new Color(0.06f, 0.08f, 0.12f, 0.90f);

            Outline outline = bgObj.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 0.25f, 0.25f, 0.5f);
            outline.effectDistance = new Vector2(0.02f, 0.02f);

            // Fill Area
            GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(bgObj.transform, false);
            RectTransform faRect = fillArea.GetComponent<RectTransform>();
            faRect.anchorMin = Vector2.zero;
            faRect.anchorMax = Vector2.one;
            faRect.offsetMin = new Vector2(0.04f, 0.04f);
            faRect.offsetMax = new Vector2(-0.04f, -0.04f);

            // Health Fill Bar (Red/Crimson)
            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(fillArea.transform, false);
            RectTransform fRect = fillObj.GetComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero;
            fRect.anchorMax = Vector2.one;
            fRect.sizeDelta = Vector2.zero;

            fillImage = fillObj.GetComponent<Image>();
            fillImage.color = new Color(0.95f, 0.18f, 0.22f, 1f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;

            // HP Text
            GameObject textObj = new GameObject("HpText", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(bgObj.transform, false);
            RectTransform tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            hpText = textObj.GetComponent<Text>();
            hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hpText.fontSize = 14;
            hpText.fontStyle = FontStyle.Bold;
            hpText.alignment = TextAnchor.MiddleCenter;
            hpText.color = Color.white;
            hpText.text = "CHOMPER";
            hpText.rectTransform.localScale = Vector3.one * 0.015f; // Scale down for world space
        }

        private void LateUpdate()
        {
            if (healthBarCanvas == null) return;

            if (enemyAI != null && enemyAI.IsDead)
            {
                Destroy(healthBarCanvas);
                return;
            }

            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;

            // Distance check
            float dist = Vector3.Distance(transform.position, mainCam.transform.position);
            if (dist > maxVisibleDistance)
            {
                if (canvasGroup != null) canvasGroup.alpha = 0f;
                return;
            }

            if (canvasGroup != null) canvasGroup.alpha = 1f;

            // Always face the camera (Billboard effect)
            healthBarCanvas.transform.rotation = mainCam.transform.rotation;

            // Smooth fill animation
            if (fillImage != null)
            {
                fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.deltaTime * 10f);
            }
        }

        public void UpdateHealth(float currentHealth, float maxHealth)
        {
            targetFill = Mathf.Clamp01(currentHealth / maxHealth);

            if (fillImage != null)
            {
                // Shift from green/yellow to red as health drops
                if (targetFill > 0.5f)
                    fillImage.color = Color.Lerp(new Color(1f, 0.75f, 0f), new Color(0.2f, 0.9f, 0.3f), (targetFill - 0.5f) * 2f);
                else
                    fillImage.color = Color.Lerp(new Color(0.95f, 0.15f, 0.2f), new Color(1f, 0.75f, 0f), targetFill * 2f);
            }

            if (hpText != null)
            {
                hpText.text = $"{Mathf.Max(0, Mathf.RoundToInt(currentHealth))}/{Mathf.RoundToInt(maxHealth)}";
            }
        }
    }
}
