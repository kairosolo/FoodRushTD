using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverContainer;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    public void Show(int finalScore, int highScore)
    {
        if (gameOverContainer == null) return;

        if (finalScoreText != null)
        {
            finalScoreText.text = $"Total Earned: ${finalScore}";
        }

        if (highScoreText != null)
        {
            highScoreText.text = $"High Score: ${highScore}";
        }

        gameOverContainer.SetActive(true);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}