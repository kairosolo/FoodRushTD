using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject mainContent;
    [SerializeField] private GameObject settingsContent;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.2f;

    private bool isGamePaused;
    private bool canPause = true;
    private Coroutine panelAnimationCoroutine;

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
        mainContent.transform.localScale = Vector3.zero;
        mainContent.SetActive(false);
        settingsContent.SetActive(false);
    }

    public void SetPausable(bool isPausable) => canPause = isPausable;

    public void TogglePause()
    {
        if (!canPause) return;
        isGamePaused = !isGamePaused;

        if (panelAnimationCoroutine != null)
        {
            StopCoroutine(panelAnimationCoroutine);
        }

        if (isGamePaused)
        {
            AudioManager.Instance.PlaySFX("UI_Pause");
            Time.timeScale = 0f;
            panelAnimationCoroutine = StartCoroutine(AnimatePausePanel(true));
        }
        else
        {
            AudioManager.Instance.PlaySFX("UI_Unpause");
            Time.timeScale = 1f;
            panelAnimationCoroutine = StartCoroutine(AnimatePausePanel(false));
        }
    }

    private IEnumerator AnimatePausePanel(bool show)
    {
        Vector3 startScale = show ? Vector3.zero : Vector3.one;
        Vector3 endScale = show ? Vector3.one : Vector3.zero;
        float timer = 0f;

        if (show)
        {
            mainContent.transform.localScale = startScale;
            mainContent.SetActive(true);
            settingsContent.SetActive(false);
        }

        while (timer < animationDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / animationDuration;

            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            mainContent.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        mainContent.transform.localScale = endScale;

        if (!show)
        {
            mainContent.SetActive(false);
            settingsContent.SetActive(false);
        }

        panelAnimationCoroutine = null;
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
        if (LevelManager.Instance != null)
            LevelManager.Instance.GoToLevelName(currentScene.name);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        if (LevelManager.Instance != null)
            LevelManager.Instance.GoToLevelID(2);
    }

    public void EndRun()
    {
        GoToMenu();
    }
}