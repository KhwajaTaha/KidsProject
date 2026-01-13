using UnityEngine;
using UnityEngine.UI;

public class WinPanelView : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button restartButton;

    public void Init(System.Action onRestart)
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => onRestart?.Invoke());
        }
    }

    public void Show()
    {
        if (root != null) root.SetActive(true);
        else gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
        else gameObject.SetActive(false);
    }
}
