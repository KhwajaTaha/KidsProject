using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMenuButton : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "Mainmenu";

    public void GoToMenu()
    {
        SceneManager.LoadScene(menuSceneName);
    }
}
