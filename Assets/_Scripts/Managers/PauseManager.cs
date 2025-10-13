using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject mainContent;
    [SerializeField] private GameObject settingsContent;

    private bool isGamePaused;
    private bool canPause = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        canPause = true;
        mainContent.SetActive(false);
        settingsContent.SetActive(false);
    }

    public void SetPausable(bool isPausable) => canPause = isPausable;

    public void TogglePause()
    {
        if (!canPause) return;
        isGamePaused = !isGamePaused;

        if (isGamePaused)
        {
            mainContent.SetActive(true);
            settingsContent.SetActive(false);
            Time.timeScale = 0f;
        }
        else
        {
            mainContent.SetActive(false);
            settingsContent.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    public void OpenSettings()
    {
        mainContent.SetActive(false);
        settingsContent.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsContent.SetActive(false);
        mainContent.SetActive(true);
    }

    public void ResumeGame()
    {
        if (isGamePaused) TogglePause();
    }
    public void RestartGame()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void EndRun()
    {
        GoToMenu();
    }
}