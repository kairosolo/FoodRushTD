using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainUI;
    [SerializeField] private GameObject customerpediaUI;
    [SerializeField] private GameObject settingsUI;
    [SerializeField] private GameObject creditsUI;

    private void Start()
    {
        OpenMenu();
        AudioManager.Instance.PlayMusic("MenuMusic");
    }

    public void StartRun()
    {
        SceneManager.LoadScene(1);
    }

    public void OpenCustomerpedia()
    {
        mainUI.SetActive(false);
        customerpediaUI.SetActive(true);
        settingsUI.SetActive(false);
        creditsUI.SetActive(false);
    }

    public void OpenMenu()
    {
        customerpediaUI.SetActive(false);
        mainUI.SetActive(true);
        settingsUI.SetActive(false);
        creditsUI.SetActive(false);
    }

    public void OpenSettings()
    {
        mainUI.SetActive(false);
        customerpediaUI.SetActive(false);
        settingsUI.SetActive(true);
        creditsUI.SetActive(false);
    }

    public void OpenCredits()
    {
        mainUI.SetActive(false);
        customerpediaUI.SetActive(false);
        settingsUI.SetActive(false);
        creditsUI.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}