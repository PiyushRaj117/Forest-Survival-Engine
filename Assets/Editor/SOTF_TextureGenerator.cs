#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ForestSurvival.EditorTools
{
    public static class SOTF_TextureGenerator
    {
        public const string TexturesFolder = "Assets/_Project/Textures/UI";

        public static void EnsureFolderExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                AssetDatabase.CreateFolder("Assets", "_Project");
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Textures"))
                AssetDatabase.CreateFolder("Assets/_Project", "Textures");
            if (!AssetDatabase.IsValidFolder(TexturesFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Textures", "UI");
        }

        public static void GenerateAllTextures()
        {
            EnsureFolderExists();

            GenerateSolidCircle(256, 120, TexturesFolder + "/SOTF_Circle_Filled.png");
            GenerateRing(256, 96, 124, TexturesFolder + "/SOTF_Arc_Ring.png");
            GenerateNotchedBezel(256, 110, 126, 6, TexturesFolder + "/SOTF_Notched_Bezel.png");
            GenerateBadgeRing(128, 48, 60, TexturesFolder + "/SOTF_Badge_Ring.png");
            GenerateSolidCircle(128, 56, TexturesFolder + "/SOTF_Badge_Bg.png");
            
            // Icons
            GenerateWaterIcon(128, TexturesFolder + "/SOTF_Icon_Water.png");
            GenerateMeatIcon(128, TexturesFolder + "/SOTF_Icon_Meat.png");
            GenerateMuscleIcon(128, TexturesFolder + "/SOTF_Icon_Muscle.png");
            GenerateMoonIcon(128, TexturesFolder + "/SOTF_Icon_Moon.png");
            GenerateTrackerRadarBg(256, TexturesFolder + "/SOTF_Tracker_Bg.png");

            AssetDatabase.Refresh();

            // Configure all generated textures as Sprites
            ConfigureAsSprite(TexturesFolder + "/SOTF_Circle_Filled.png");
            ConfigureAsSprite(TexturesFolder + "/SOTF_Arc_Ring.png");
            ConfigureAsSprite(TexturesFolder + "/SOTF_Notched_Bezel.png");
            ConfigureAsSprite(TexturesFolder + "/SOTF_Badge_Ring.png");
            ConfigureAsSprite(TexturesFolder + "/SOTF_Badge_Bg.png");
            ConfigureAsSprite(TexturesFolder + "/SOTF_Icon_Water.png");
            ConfigureAsSprite(TexturesFolder + "/SOTF_Icon_Meat.png");
            ConfigureAsSprite(TexturesFolder + "/SOTF_Icon_Muscle.png");
            ConfigureAsSprite(TexturesFolder + "/SOTF_Icon_Moon.png");
            ConfigureAsSprite(TexturesFolder + "/SOTF_Tracker_Bg.png");

            AssetDatabase.SaveAssets();
            Debug.Log("<color=#4CAF50><b>[SOTF UI]</b></color> Procedural UI Sprites successfully created in " + TexturesFolder);
        }

        private static void ConfigureAsSprite(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        private static void SavePNG(Texture2D tex, string path)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            Object.DestroyImmediate(tex);
        }

        private static void GenerateSolidCircle(int size, float radius, string path)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            SavePNG(tex, path);
        }

        private static void GenerateRing(int size, float innerR, float outerR, string path)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float aIn = Mathf.Clamp01(dist - innerR + 0.5f);
                    float aOut = Mathf.Clamp01(outerR - dist + 0.5f);
                    float alpha = aIn * aOut;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            SavePNG(tex, path);
        }

        private static void GenerateNotchedBezel(int size, float innerR, float outerR, int segments, string path)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 dir = new Vector2(x - center.x, y - center.y);
                    float dist = dir.magnitude;

                    // Only on the left half (180 deg to 360 deg in standard angle, or x < center.x)
                    float aIn = Mathf.Clamp01(dist - innerR + 0.5f);
                    float aOut = Mathf.Clamp01(outerR - dist + 0.5f);
                    float ringAlpha = aIn * aOut;

                    // Angle in degrees [0, 360)
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    // Left side is roughly 90 to 270 degrees
                    bool isLeftSide = (angle >= 88f && angle <= 272f);

                    if (isLeftSide && ringAlpha > 0.01f)
                    {
                        // Check notches
                        float step = 180f / segments;
                        float offsetAngle = (angle - 90f);
                        float mod = offsetAngle % step;
                        float notchGap = (mod < 2f || mod > step - 2f) ? 0f : 1f;

                        pixels[y * size + x] = new Color(0.12f, 0.12f, 0.14f, ringAlpha * notchGap);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            SavePNG(tex, path);
        }

        private static void GenerateBadgeRing(int size, float innerR, float outerR, string path)
        {
            GenerateRing(size, innerR, outerR, path);
        }

        private static void GenerateWaterIcon(int size, string path)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.42f);
            float r = size * 0.22f;
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = 0f;

                    if (dist <= r + 0.5f)
                    {
                        alpha = Mathf.Clamp01(r - dist + 0.5f);
                    }
                    else if (y > center.y)
                    {
                        // Triangle cone to tip at (center.x, center.y + r * 1.7)
                        float tipY = center.y + r * 1.6f;
                        if (y <= tipY)
                        {
                            float t = (y - center.y) / (tipY - center.y);
                            float halfWidth = Mathf.Lerp(r, 0f, t);
                            float dx = Mathf.Abs(x - center.x);
                            alpha = Mathf.Clamp01(halfWidth - dx + 0.5f);
                        }
                    }

                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            SavePNG(tex, path);
        }

        private static void GenerateMeatIcon(int size, string path)
        {
            // Drumstick silhouette
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 meatCenter = new Vector2(size * 0.55f, size * 0.58f);
            float meatRadius = size * 0.22f;
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dMeat = Vector2.Distance(new Vector2(x, y), meatCenter);
                    float alpha = Mathf.Clamp01(meatRadius - dMeat + 0.5f);

                    // Bone handle pointing to bottom-left
                    Vector2 p = new Vector2(x, y);
                    Vector2 boneStart = new Vector2(size * 0.45f, size * 0.45f);
                    Vector2 boneEnd = new Vector2(size * 0.28f, size * 0.28f);
                    float distToBone = DistancePointToSegment(p, boneStart, boneEnd);
                    float boneAlpha = Mathf.Clamp01(size * 0.055f - distToBone + 0.5f);

                    // Bone knobs at bottom-left
                    float dKnob1 = Vector2.Distance(p, boneEnd + new Vector2(-4f, 4f));
                    float dKnob2 = Vector2.Distance(p, boneEnd + new Vector2(4f, -4f));
                    float knobAlpha = Mathf.Max(
                        Mathf.Clamp01(size * 0.06f - dKnob1 + 0.5f),
                        Mathf.Clamp01(size * 0.06f - dKnob2 + 0.5f)
                    );

                    alpha = Mathf.Max(alpha, Mathf.Max(boneAlpha, knobAlpha));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            SavePNG(tex, path);
        }

        private static void GenerateMuscleIcon(int size, string path)
        {
            // Stylized flexed bicep / arm icon
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Arm: curved arc with muscle peak
                    Vector2 p = new Vector2(x, y);
                    float dForearm = DistancePointToSegment(p, new Vector2(c.x - 22f, c.y - 10f), new Vector2(c.x - 5f, c.y + 12f));
                    float dBicep = DistancePointToSegment(p, new Vector2(c.x - 5f, c.y + 12f), new Vector2(c.x + 20f, c.y - 4f));
                    float dFist = Vector2.Distance(p, new Vector2(c.x - 20f, c.y - 12f));

                    float aArm = Mathf.Clamp01(size * 0.1f - Mathf.Min(dForearm, dBicep) + 0.5f);
                    float aFist = Mathf.Clamp01(size * 0.09f - dFist + 0.5f);

                    // Bicep peak circle
                    float dPeak = Vector2.Distance(p, new Vector2(c.x + 4f, c.y + 14f));
                    float aPeak = Mathf.Clamp01(size * 0.12f - dPeak + 0.5f);

                    float alpha = Mathf.Max(aArm, Mathf.Max(aFist, aPeak));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            SavePNG(tex, path);
        }

        private static void GenerateMoonIcon(int size, string path)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float r = size * 0.28f;
            Vector2 cutOffset = new Vector2(size * 0.12f, size * 0.08f);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float d1 = Vector2.Distance(p, c);
                    float d2 = Vector2.Distance(p, c + cutOffset);

                    float a1 = Mathf.Clamp01(r - d1 + 0.5f);
                    float a2 = Mathf.Clamp01(d2 - (r * 0.85f) + 0.5f);

                    pixels[y * size + x] = new Color(1f, 1f, 1f, a1 * a2);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            SavePNG(tex, path);
        }

        private static void GenerateTrackerRadarBg(int size, string path)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float outerR = size * 0.44f;
            Color32[] pixels = new Color32[size * size];

            Color mapBase = new Color(0.38f, 0.47f, 0.36f, 0.95f); // Soft topographical green
            Color gridColor = new Color(0.28f, 0.36f, 0.27f, 0.6f);
            Color borderDark = new Color(0.08f, 0.09f, 0.08f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), c);

                    if (dist > outerR + 1f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    float borderA = Mathf.Clamp01(dist - (outerR - 5f));
                    Color col = Color.Lerp(mapBase, borderDark, borderA);

                    // Grid crosshair
                    if (Mathf.Abs(x - c.x) < 1f || Mathf.Abs(y - c.y) < 1f)
                    {
                        col = Color.Lerp(col, gridColor, 0.4f);
                    }

                    // Inner range circle
                    float dCircle = Mathf.Abs(dist - (outerR * 0.55f));
                    if (dCircle < 1.2f)
                    {
                        col = Color.Lerp(col, gridColor, 0.6f);
                    }

                    // Soft edge anti-aliasing
                    float edgeA = Mathf.Clamp01(outerR - dist + 0.5f);
                    col.a *= edgeA;

                    pixels[y * size + x] = col;
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            SavePNG(tex, path);
        }

        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a;
            Vector2 ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }
    }
}
#endif
