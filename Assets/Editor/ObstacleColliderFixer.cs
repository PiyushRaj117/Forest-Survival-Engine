#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ForestSurvival.EditorTools
{
    public static class ObstacleColliderFixer
    {
        [MenuItem("Tools/Forest Survival/Fix Obstacle Colliders (Trees & Rocks)")]
        public static void FixAllObstacles()
        {
            int prefabCollidersAdded = 0;

            // 1. Add Colliders directly to all Polytope Studio Prefabs (Trees, Rocks, Stumps)
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Polytope Studio" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string lower = path.ToLower();

                // Target Rocks, Trees, and Stumps (Skip grass and flowers)
                if (lower.Contains("rock") || lower.Contains("tree") || lower.Contains("stump") || lower.Contains("log") || lower.Contains("ore") || lower.Contains("boulder"))
                {
                    if (lower.Contains("grass") || lower.Contains("poppy") || lower.Contains("mushroom") || lower.Contains("flower"))
                        continue;

                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        Collider col = prefab.GetComponentInChildren<Collider>();
                        if (col == null)
                        {
                            // Open prefab contents to modify
                            GameObject instance = PrefabUtility.LoadPrefabContents(path);
                            MeshFilter mf = instance.GetComponentInChildren<MeshFilter>();
                            if (mf != null && mf.sharedMesh != null)
                            {
                                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                                mc.sharedMesh = mf.sharedMesh;
                                mc.convex = false; // Static obstacle collider
                                PrefabUtility.SaveAsPrefabAsset(instance, path);
                                prefabCollidersAdded++;
                            }
                            else
                            {
                                BoxCollider bc = instance.AddComponent<BoxCollider>();
                                PrefabUtility.SaveAsPrefabAsset(instance, path);
                                prefabCollidersAdded++;
                            }
                            PrefabUtility.UnloadPrefabContents(instance);
                        }
                    }
                }
            }

            // 2. Ensure all active Terrains enable Tree Colliders and refresh
            Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            foreach (Terrain t in terrains)
            {
                TerrainCollider tc = t.GetComponent<TerrainCollider>();
                if (tc != null)
                {
                    tc.enabled = false;
                    tc.enabled = true; // Force collision tree bake refresh
                }

                t.drawTreesAndFoliage = true;
                EditorUtility.SetDirty(t);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#4CAF50><b>[Obstacle Fixer]</b></color> Finished! Successfully added colliders to {prefabCollidersAdded} Rock & Tree prefabs. Terrain tree collisions refreshed!");
        }
    }
}
#endif
