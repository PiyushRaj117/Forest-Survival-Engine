// FixMenuButtonsOnly.cs — [InitializeOnLoad] — runs ONCE, then self-deletes.
// Wires MainMenuManager fields, clears broken OnClick listeners,
// fixes EventSystem input module, ensures SampleScene in build settings.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.Linq;

[InitializeOnLoad]
public static class FixMenuButtonsOnly
{
    static FixMenuButtonsOnly()
    {
        EditorApplication.delayCall += Run;
    }

    static void Run()
    {
        // ── 1. Ensure SampleScene in Build Settings ───────────────────────────
        const string samplePath = "Assets/_Project/Scenes/SampleScene.unity";
        const string mainPath   = "Assets/_Project/Scenes/MainMenu.unity";
        const string vikingPath = "Assets/_Project/Scenes/VikingVillageScene.unity";

        var scenes = EditorBuildSettings.scenes.ToList();
        void EnsureScene(string p)
        {
            if (!scenes.Any(s => s.path == p))
                scenes.Add(new EditorBuildSettingsScene(p, true));
        }
        EnsureScene(mainPath);
        EnsureScene(samplePath);
        EnsureScene(vikingPath);
        EditorBuildSettings.scenes = scenes.ToArray();

        // ── 2. Open MainMenu.unity ────────────────────────────────────────────
        var scene = EditorSceneManager.OpenScene(mainPath, OpenSceneMode.Single);

        // ── 3. Find key GameObjects ───────────────────────────────────────────
        GameObject Find(string n) {
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go.scene.isLoaded && go.name == n) return go;
            return null;
        }

        var mmGO          = Find("MainMenuManager");
        var buttonsGroup  = Find("ButtonsGroup");
        var settingsPanel = Find("SettingsPanel");
        var playBtnGO     = Find("PlayButton");
        var settBtnGO     = Find("SettingsButton");
        var quitBtnGO     = Find("QuitButton");
        var backBtnGO     = Find("BackButton");
        var eventSysGO    = Find("EventSystem");

        // ── 4. Wire MainMenuManager fields ───────────────────────────────────
        if (mmGO != null)
        {
            var mm = mmGO.GetComponent<MainMenuManager>();
            if (mm != null)
            {
                if (buttonsGroup  != null) mm.mainButtonsPanel = buttonsGroup;
                if (settingsPanel != null) mm.settingsPanel     = settingsPanel;
                if (playBtnGO     != null) mm.playButton        = playBtnGO.GetComponent<Button>();
                if (settBtnGO     != null) mm.settingsButton    = settBtnGO.GetComponent<Button>();
                if (quitBtnGO     != null) mm.quitButton        = quitBtnGO.GetComponent<Button>();
                if (backBtnGO     != null) mm.backButton        = backBtnGO.GetComponent<Button>();
                EditorUtility.SetDirty(mm);
            }
        }

        // ── 5. Clear broken persistent OnClick listeners ──────────────────────
        void ClearOnClick(GameObject go)
        {
            if (go == null) return;
            var btn = go.GetComponent<Button>();
            if (btn == null) return;
            var calls = btn.onClick.GetPersistentEventCount();
            // Remove all persistent listeners by rebuilding with SerializedObject
            var so = new SerializedObject(btn);
            var onClick = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            if (onClick != null) { onClick.ClearArray(); so.ApplyModifiedProperties(); }
            EditorUtility.SetDirty(btn);
        }
        ClearOnClick(playBtnGO);
        ClearOnClick(settBtnGO);
        ClearOnClick(quitBtnGO);
        ClearOnClick(backBtnGO);

        // ── 6. Fix EventSystem input module ──────────────────────────────────
        if (eventSysGO != null)
        {
            var standalone = eventSysGO.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                Object.DestroyImmediate(standalone, true);
                // Add InputSystemUIInputModule if available
                var t = System.Type.GetType(
                    "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (t != null && eventSysGO.GetComponent(t) == null)
                    eventSysGO.AddComponent(t);
                EditorUtility.SetDirty(eventSysGO);
            }
        }

        // ── 7. Save & self-delete ─────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FixMenuButtonsOnly] Done — buttons wired, EventSystem fixed.");

        AssetDatabase.DeleteAsset("Assets/_Project/Editor/FixMenuButtonsOnly.cs");
        AssetDatabase.Refresh();
    }
}
