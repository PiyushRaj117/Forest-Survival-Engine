// RainbowBarOnly.cs — [InitializeOnLoad] — runs ONCE, then self-deletes.
// Generates CapsuleBg.png + RainbowFill.png, imports as Sprites,
// patches MainMenu.unity (VideoBackground stretch, bar size/sprites, ProgressText),
// saves scene, then deletes this file.
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;

[InitializeOnLoad]
public static class RainbowBarOnly
{
    static RainbowBarOnly()
    {
        EditorApplication.delayCall += Run;
    }

    static void Run()
    {
        // ── 1. Generate PNG sprites ───────────────────────────────────────────
        const int W = 512, H = 64;
        float radius = H / 2f;

        // CapsuleBg: dark navy pill with white border
        Texture2D bgTex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        Color navyFill  = new Color(0.05f, 0.05f, 0.18f, 0.92f);
        Color border    = Color.white;
        Color clear     = Color.clear;
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float dist = CapsuleSDF(x, y, W, H, radius);
                if (dist > 0f)        bgTex.SetPixel(x, y, clear);
                else if (dist > -2f)  bgTex.SetPixel(x, y, border);
                else                  bgTex.SetPixel(x, y, navyFill);
            }
        }
        bgTex.Apply();

        // RainbowFill: smooth horizontal rainbow gradient pill
        Texture2D rainbowTex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        Color[] rainbow = new Color[]
        {
            Color.white,
            new Color(1f, 0.4f, 0.7f),  // pink
            new Color(0.6f, 0.1f, 1f),  // purple
            new Color(0.1f, 0.3f, 1f),  // blue
            Color.cyan,
            Color.green,
            Color.yellow,
            new Color(1f, 0.5f, 0f),    // orange
        };
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float t    = (float)x / (W - 1);
                float seg  = t * (rainbow.Length - 1);
                int   idx  = Mathf.FloorToInt(seg);
                idx = Mathf.Clamp(idx, 0, rainbow.Length - 2);
                Color col  = Color.Lerp(rainbow[idx], rainbow[idx + 1], seg - idx);
                float dist = CapsuleSDF(x, y, W, H, radius);
                col.a = dist > 0f ? 0f : 1f;
                rainbowTex.SetPixel(x, y, col);
            }
        }
        rainbowTex.Apply();

        string uiDir = "Assets/_Project/Textures/UI";
        string bgPath  = uiDir + "/CapsuleBg.png";
        string rbPath  = uiDir + "/RainbowFill.png";
        File.WriteAllBytes(bgPath,  bgTex.EncodeToPNG());
        File.WriteAllBytes(rbPath,  rainbowTex.EncodeToPNG());
        AssetDatabase.Refresh();

        // Import as Sprites
        SetSprite(bgPath);
        SetSprite(rbPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── 2. Patch MainMenu.unity via scene API ─────────────────────────────
        string scenePath = "Assets/_Project/Scenes/MainMenu.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Find GameObjects by name
        GameObject videoBackground  = FindInScene("VideoBackground");
        GameObject progressBarBg    = FindInScene("ProgressBarBg");
        GameObject progressBarFill  = FindInScene("ProgressBarFill");
        GameObject progressText     = FindInScene("ProgressText");

        // 2a. VideoBackground — full stretch
        if (videoBackground != null)
        {
            var rt = videoBackground.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin  = Vector2.zero;
                rt.anchorMax  = Vector2.one;
                rt.offsetMin  = Vector2.zero;
                rt.offsetMax  = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta  = Vector2.zero;
            }
        }

        Sprite bgSprite  = AssetDatabase.LoadAssetAtPath<Sprite>(bgPath);
        Sprite rbSprite  = AssetDatabase.LoadAssetAtPath<Sprite>(rbPath);

        // 2b. ProgressBarBg — 620x30, CapsuleBg sprite, white
        if (progressBarBg != null)
        {
            var rt  = progressBarBg.GetComponent<RectTransform>();
            var img = progressBarBg.GetComponent<UnityEngine.UI.Image>();
            if (rt  != null) { rt.sizeDelta = new Vector2(620, 30); rt.anchoredPosition = new Vector2(0, -300); }
            if (img != null) { img.sprite = bgSprite; img.color = Color.white; img.type = UnityEngine.UI.Image.Type.Sliced; }
        }

        // 2c. ProgressBarFill — RainbowFill sprite, Filled Horizontal, fillAmount=0
        if (progressBarFill != null)
        {
            var rt  = progressBarFill.GetComponent<RectTransform>();
            var img = progressBarFill.GetComponent<UnityEngine.UI.Image>();
            if (rt  != null) { rt.sizeDelta = new Vector2(620, 30); rt.anchoredPosition = new Vector2(0, -300); }
            if (img != null)
            {
                img.sprite      = rbSprite;
                img.color       = Color.white;
                img.type        = UnityEngine.UI.Image.Type.Filled;
                img.fillMethod  = UnityEngine.UI.Image.FillMethod.Horizontal;
                img.fillOrigin  = 0;
                img.fillAmount  = 0f;
            }
        }

        // 2d. ProgressText — centered above bar, fontSize=30, bold white
        if (progressText != null)
        {
            var rt = progressText.GetComponent<RectTransform>();
            if (rt != null) { rt.sizeDelta = new Vector2(620, 40); rt.anchoredPosition = new Vector2(0, -268); }

            // Try TMP first, then legacy Text
            var tmp = progressText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.fontSize   = 30;
                tmp.fontStyle  = TMPro.FontStyles.Bold;
                tmp.color      = Color.white;
                tmp.alignment  = TMPro.TextAlignmentOptions.Center;
            }
            else
            {
                var txt = progressText.GetComponent<UnityEngine.UI.Text>();
                if (txt != null) { txt.fontSize = 30; txt.fontStyle = FontStyle.Bold; txt.color = Color.white; txt.alignment = TextAnchor.MiddleCenter; }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[RainbowBarOnly] Done — rainbow bar + 1080p video applied.");

        // ── 3. Self-delete ────────────────────────────────────────────────────
        string self = "Assets/_Project/Editor/RainbowBarOnly.cs";
        AssetDatabase.DeleteAsset(self);
        AssetDatabase.Refresh();
    }

    // Returns positive outside pill, 0 on edge, negative inside (px from edge)
    static float CapsuleSDF(int x, int y, int W, int H, float r)
    {
        float px = x - W / 2f, py = y - H / 2f;
        float ax = Mathf.Clamp(px, -(W / 2f - r), W / 2f - r);
        float dx = px - ax, dy = py;
        return Mathf.Sqrt(dx * dx + dy * dy) - r;
    }

    static void SetSprite(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        imp.textureType        = TextureImporterType.Sprite;
        imp.spriteImportMode   = SpriteImportMode.Single;
        imp.mipmapEnabled      = false;
        imp.filterMode         = FilterMode.Bilinear;
        imp.alphaIsTransparency = true;
        imp.spriteBorder       = new Vector4(32, 32, 32, 32); // 9-slice border
        imp.SaveAndReimport();
    }

    static GameObject FindInScene(string name)
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.scene.isLoaded && go.name == name) return go;
        return null;
    }
}
