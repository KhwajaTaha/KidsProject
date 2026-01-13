using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Main";

    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;

    private void Start()
    {
        RefreshButtons();
    }

    private void OnEnable()
    {
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        bool hasSave = SaveUtil.HasSave();

        if (continueButton != null)
            continueButton.interactable = hasSave;

        if (newGameButton != null)
            newGameButton.interactable = true;
    }

    public void OnContinuePressed()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnNewGamePressed()
    {
        SaveUtil.DeleteSave();
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnQuitPressed()
    {
        Application.Quit();
    }
}
