using UnityEngine;
using UnityEngine.SceneManagement;

// Handles scene navigation
// Keeps scene switching centralized.

public class SceneManagerController : MonoBehaviour
{
    // Loads the poker game scene
    public void LoadGame()
    {
        SceneManager.LoadScene("Game");
    }

    // Loads the main menu scene
    public void LoadMenu()
    {
        SceneManager.LoadScene("Menu");
    }

    // Quits the application
    public void QuitGame()
    {
        Debug.Log("Quit Game");

        Application.Quit();

        // Allows quitting in editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}