using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("SampleScene");
    }


    public void OpenSettings()
    {
        ForestSurvival.UI.NeonPauseMenuUI pause = Object.FindFirstObjectByType<ForestSurvival.UI.NeonPauseMenuUI>();
        if (pause != null)
        {
            pause.OpenFromMainMenu();
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
