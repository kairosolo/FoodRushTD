using UnityEngine;
using System;
using KairosoloSystems;

public class GameLoopManager : MonoBehaviour
{
    public static GameLoopManager Instance { get; private set; }

    public enum GameState
    { Preparation, Action, GameOver }

    public static event Action<int> OnUnsatisfiedCustomersChanged;

    [Header("Lose Condition")]
    [SerializeField] private int maxUnsatisfiedCustomers = 10;

    [Header("UI References")]
    [SerializeField] private GameOverUI gameOverUI;

    public GameState CurrentState { get; private set; }
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
        CurrentState = GameState.Action;
    }

    private void StartNightPhase()
    {
        if (CurrentState == GameState.GameOver) return;
        CurrentState = GameState.Preparation;
    }

    public void CustomerReachedExitUnsatisfied()
    {
        if (CurrentState == GameState.GameOver) return;

        currentUnsatisfiedCustomers++;
        OnUnsatisfiedCustomersChanged?.Invoke(currentUnsatisfiedCustomers);

        if (currentUnsatisfiedCustomers >= maxUnsatisfiedCustomers)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        CurrentState = GameState.GameOver;
        Debug.Log("GAME OVER: Too many unsatisfied customers.");
        Time.timeScale = 0f;

        int finalScore = EconomyManager.Instance.TotalCashEarned;
        int highScore = KPlayerPrefs.GetInt(HIGHSCORE_KEY, 0);

        if (finalScore > highScore)
        {
            highScore = finalScore;
            KPlayerPrefs.SetInt(HIGHSCORE_KEY, highScore);
            KPlayerPrefs.Save();
            Debug.Log($"New highscore: {highScore}");
        }

        gameOverUI.Show(finalScore, highScore);
    }
}