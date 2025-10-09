using UnityEngine;
using System;

public class GameClock : MonoBehaviour
{
    public static GameClock Instance { get; private set; }

    public static event Action OnDayPhaseStart;

    public static event Action OnNightPhaseStart;

    public static event Action<int, int> OnTimeChanged; // Hour, Minute

    public static event Action<int> OnDayChanged; // Day

    [Header("Time Settings")]
    [SerializeField] private float secondsPerIngameMinute = 1f;
    [SerializeField] private int startingDay = 1;
    [SerializeField] private int startingHour = 3;

    [Header("Phase Times")]
    [SerializeField] private int dayPhaseStartHour = 3;
    [SerializeField] private int nightPhaseStartHour = 21;

    private float timer;
    private int currentDay;
    private int currentHour;
    private int currentMinute;

    public int CurrentDay => currentDay;
    public int CurrentHour => currentHour;
    public int CurrentMinute => currentMinute;

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
        currentDay = startingDay;
        currentHour = startingHour;
        currentMinute = 0;

        OnDayChanged?.Invoke(currentDay);
        OnTimeChanged?.Invoke(currentHour, currentMinute);
        if (currentHour >= dayPhaseStartHour && currentHour < nightPhaseStartHour)
        {
            OnDayPhaseStart?.Invoke();
        }
        else
        {
            OnNightPhaseStart?.Invoke();
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        timer += Time.deltaTime;
        if (timer >= secondsPerIngameMinute)
        {
            timer -= secondsPerIngameMinute;
            AdvanceMinute();
        }
    }

    private void AdvanceMinute()
    {
        currentMinute++;
        if (currentMinute >= 60)
        {
            currentMinute = 0;
            currentHour++;
            if (currentHour >= 24)
            {
                currentHour = 0;
                currentDay++;
                OnDayChanged?.Invoke(currentDay);
            }
        }

        OnTimeChanged?.Invoke(currentHour, currentMinute);

        if (currentHour == dayPhaseStartHour && currentMinute == 0)
        {
            OnDayPhaseStart?.Invoke();
        }
        else if (currentHour == nightPhaseStartHour && currentMinute == 0)
        {
            OnNightPhaseStart?.Invoke();
        }
    }
}