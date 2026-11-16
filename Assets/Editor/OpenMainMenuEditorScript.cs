using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class OpenMainMenuEditorScript
{
    static OpenMainMenuEditorScript()
    {
        // EditorApplication.delayCall += OpenSceneOnce;
    }

    [MenuItem("Tools/Open Main Menu Scene", priority = 0)]
    public static void OpenScene()
    {
        string scenePath = "Assets/_Project/Scenes/MainMenu.unity";
        if (System.IO.File.Exists(scenePath))
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log("<color=green>[MainMenu]</color> Successfully opened MainMenu.unity!");
        }
        else
        {
            Debug.LogError($"[MainMenu] Scene file not found at {scenePath}");
        }
    }

    private static void OpenSceneOnce()
    {
        string scenePath = "Assets/_Project/Scenes/MainMenu.unity";
        if (System.IO.File.Exists(scenePath) && EditorSceneManager.GetActiveScene().path != scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log("<color=green>[MainMenu]</color> Auto-opened MainMenu.unity as active scene!");
        }
    }
}
