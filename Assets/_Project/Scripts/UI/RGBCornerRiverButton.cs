using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class HyperFastNeonRiverButton : MonoBehaviour
{
    [Header("Speed Settings")]
    [Tooltip("Warp speed factor for the color cycle.")]
    public float speed = 8.0f; // Significantly cranked up for ultra-fast motion

    [Tooltip("Controls how closely packed the color waves are across the button.")]
    public float colorOffsetSpread = 0.5f;

    [Header("Color Palette (All Requested Neons)")]
    public Color[] neonColors = new Color[]
    {
        new Color(1f, 0f, 0f, 1f),         // 1. Red
        new Color(0f, 0f, 1f, 1f),         // 2. Blue
        new Color(0f, 1f, 0f, 1f),         // 3. Green
        new Color(0.05f, 0f, 0.15f, 1f),   // 4. Black / Deep Void Neon (Keeps the glow visible)
        new Color(0.5f, 1f, 0f, 1f),       // 5. Light Green / Lime
        new Color(0f, 1f, 1f, 1f),         // 6. Cyan
        new Color(1f, 0f, 0.5f, 1f),       // 7. Pink (From your image)
        new Color(0.5f, 0f, 1f, 1f),       // 8. Purple (From your image)
        new Color(1f, 0.5f, 0f, 1f)        // 9. Orange
    };

    private TextMeshProUGUI textComponent;

    void Start()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        textComponent.enableVertexGradient = true;
    }

    void Update()
    {
        if (textComponent == null || neonColors.Length == 0) return;

        // Driven by unscaled time so it remains fast even if the game pauses
        float timeFactor = Time.unscaledTime * speed;

        // Offsets the sample time for each vertex corner to create a rapid cascading river flow
        Color topLeft = EvaluateMultiColor(timeFactor);
        Color topRight = EvaluateMultiColor(timeFactor + colorOffsetSpread);
        Color bottomLeft = EvaluateMultiColor(timeFactor + (colorOffsetSpread * 2f));
        Color bottomRight = EvaluateMultiColor(timeFactor + (colorOffsetSpread * 3f));

        // Inject the high-speed colors directly into the TextMeshPro mesh matrix
        textComponent.colorGradient = new VertexGradient(topLeft, topRight, bottomLeft, bottomRight);
    }

    private Color EvaluateMultiColor(float time)
    {
        float totalPosition = Mathf.Repeat(time, neonColors.Length);

        int currentIndex = Mathf.FloorToInt(totalPosition);
        int nextIndex = (currentIndex + 1) % neonColors.Length;

        float interpolationFactor = totalPosition - currentIndex;

        // Linear interpolation ensures a completely seamless, continuous color loop
        return Color.Lerp(neonColors[currentIndex], neonColors[nextIndex], interpolationFactor);
    }
}
