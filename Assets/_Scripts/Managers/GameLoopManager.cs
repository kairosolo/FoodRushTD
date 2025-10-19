using UnityEngine;
using System;
using KairosoloSystems;

public class GameLoopManager : MonoBehaviour
{
    public static GameLoopManager Instance { get; private set; }

    public enum GameState
    { Preparation, Action, GameOver, UIMode }

    public static event Action<int> OnUnsatisfiedCustomersChanged;

    [Header("Lose Condition")]
    [SerializeField] private int maxUnsatisfiedCustomers = 10;

    [Header("Game Over Shake")]
    [SerializeField] private float gameOverShakeDuration = 0.8f;
    [SerializeField] private float gameOverShakeMagnitude = 0.3f;

    [Header("UI References")]
    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private StationSelectionUI stationSelectionUI;
    public GameState CurrentState { get; private set; }
    private GameState previousState;

    private int currentUnsatisfiedCustomers;

    private const string HIGHSCORE_KEY = "HighestCashEarned";

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
        Time.timeScale = 1f;
    }

    private void Start()
    {
        currentUnsatisfiedCustomers = 0;
        OnUnsatisfiedCustomersChanged?.Invoke(currentUnsatisfiedCustomers);
    }

    private void OnEnable()
    {
        GameClock.OnDayPhaseStart += StartDayPhase;
        GameClock.OnNightPhaseStart += StartNightPhase;
    }

    private void OnDisable()
    {
        GameClock.OnDayPhaseStart -= StartDayPhase;
        GameClock.OnNightPhaseStart -= StartNightPhase;
    }

    private void StartDayPhase()
    {
        if (CurrentState == GameState.GameOver) return;
        if (CurrentState != GameState.UIMode)
        {
            CurrentState = GameState.Action;
        }
        previousState = GameState.Action;
    }

    private void StartNightPhase()
    {
        if (CurrentState == GameState.GameOver) return;
        if (CurrentState != GameState.UIMode)
        {
            CurrentState = GameState.Preparation;
        }
        previousState = GameState.Preparation;
    }

    public void CustomerReachedExitUnsatisfied()
    {
        if (CurrentState == GameState.GameOver) return;

        currentUnsatisfiedCustomers++;
        OnUnsatisfiedCustomersChanged?.Invoke(currentUnsatisfiedCustomers);
        AudioManager.Instance.PlaySFX("Game_CustomerLost");

        if (currentUnsatisfiedCustomers >= maxUnsatisfiedCustomers)
        {
            TriggerGameOver();
        }
        else
        {
            if (CameraManager.Instance != null && CameraManager.Instance.MainCameraShake != null)
            {
                CameraManager.Instance.MainCameraShake.TriggerShake();
            }
        }
    }

    private void TriggerGameOver()
    {
        CurrentState = GameState.GameOver;
        Debug.Log("GAME OVER: Too many unsatisfied customers.");
        Time.timeScale = 0f;

        if (CameraManager.Instance != null && CameraManager.Instance.MainCameraShake != null)
        {
            CameraManager.Instance.MainCameraShake.TriggerShake(gameOverShakeDuration, gameOverShakeMagnitude);
        }

        int finalScore = EconomyManager.Instance.TotalCashEarned;

        int currentHighScore = 0;
        if (KPlayerPrefs.HasKey(HIGHSCORE_KEY))
        {
            string rawValue = KPlayerPrefs.GetString(HIGHSCORE_KEY);
            string[] parts = rawValue.Split('|');
            if (parts.Length > 0 && int.TryParse(parts[0], out int score))
            {
                currentHighScore = score;
            }
        }

        if (finalScore > currentHighScore)
        {
            string dateString = DateTime.Now.ToString("yyyy-MM-dd");
            int daysSurvived = GameClock.Instance.CurrentDay;

            KPlayerPrefs.SetString(HIGHSCORE_KEY, $"{finalScore}|{dateString}|{daysSurvived}");
            KPlayerPrefs.Save();
            Debug.Log($"New highscore: {finalScore} set on {dateString} after surviving {daysSurvived} days.");
        }

        AudioManager.Instance.PlaySFX("Game_LoseJingle");
        gameOverUI.Show(finalScore, currentHighScore);
    }

    public void Debug_TriggerGameOver()
    {
        Debug.Log("DEBUG: Manually triggering game over.");
        TriggerGameOver();
    }

    public void EnterUIMode()
    {
        if (CurrentState == GameState.GameOver || CurrentState == GameState.UIMode) return;

        previousState = CurrentState;
        CurrentState = GameState.UIMode;
        stationSelectionUI.SetInteractable(false);
        Debug.Log("Entered UI Mode. Interactions locked.");
    }

    public void ExitUIMode()
    {
        if (CurrentState != GameState.UIMode) return;

        CurrentState = previousState;
        stationSelectionUI.SetInteractable(true);
        Debug.Log("Exited UI Mode. Interactions unlocked.");
    }
}