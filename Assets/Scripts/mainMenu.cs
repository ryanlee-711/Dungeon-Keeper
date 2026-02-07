using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using static System.Net.Mime.MediaTypeNames;

public class MainMenu : MonoBehaviour
{
    // This function will be called when the button is clicked
    public void PlayGame()
    {
        // Make sure this matches your scene name EXACTLY
        SceneManager.LoadScene("introduction");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game!"); // Just to show it works in the editor
    }
}