using UnityEngine;
using TMPro;
using KairosoloSystems;

public class MenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainUI;
    [SerializeField] private GameObject settingsUI;
    [SerializeField] private GameObject creditsUI;
    [SerializeField] private GameObject highScorePanel;

    [Header("High Score Display")]
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI highScoreDateText;

    private const string HIGHSCORE_KEY = "HighestCashEarned";

    private void Start()
    {
        OpenMenu();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic("MenuMusic");
        }
        DisplayHighScore();
    }

    private void DisplayHighScore()
    {
        if (highScoreText == null || highScoreDateText == null)
        {
            if (highScoreText != null) highScoreText.gameObject.SetActive(false);
            if (highScoreDateText != null) highScoreDateText.gameObject.SetActive(false);
            return;
        }

        if (KPlayerPrefs.HasKey(HIGHSCORE_KEY))
        {
            highScorePanel.gameObject.SetActive(true);
            string rawValue = KPlayerPrefs.GetString(HIGHSCORE_KEY);
            string[] parts = rawValue.Split('|');

            if (parts.Length == 2)
            {
                string score = parts[0];
                string date = parts[1];

                highScoreText.text = $"High Score: ${score}";
                highScoreDateText.text = $"Set on: {date}";

                highScoreText.gameObject.SetActive(true);
                highScoreDateText.gameObject.SetActive(true);
            }
            else
            {
                highScoreText.text = $"High Score: ${rawValue}";
                highScoreText.gameObject.SetActive(true);
                highScoreDateText.gameObject.SetActive(false);
            }
        }
        else
        {
            highScorePanel.gameObject.SetActive(false);
            highScoreText.gameObject.SetActive(false);
            highScoreDateText.gameObject.SetActive(false);
        }
    }

    public void StartRun()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.GoToLevelID(1);
        }
    }

    public void OpenMenu()
    {
        mainUI.SetActive(true);
        settingsUI.SetActive(false);
        creditsUI.SetActive(false);
    }

    public void OpenSettings()
    {
        mainUI.SetActive(false);
        settingsUI.SetActive(true);
        creditsUI.SetActive(false);
    }

    public void OpenCredits()
    {
        mainUI.SetActive(false);
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