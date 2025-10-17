using UnityEngine;
using System.Collections.Generic;
using System;

public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    public static event Action OnAvailableStationsChanged;

    public static event Action<StationData> OnNewStationUnlocked;

    [System.Serializable]
    public class DailyProgression
    {
        public int day;
        public List<CustomerData> customersToSpawn;
        public List<StationData> stationsToUnlock;
    }

    [SerializeField] private List<DailyProgression> progressionTimeline;

    public List<CustomerData> AvailableCustomers { get; private set; }
    public List<StationData> AvailableStations { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            AvailableCustomers = new List<CustomerData>();
            AvailableStations = new List<StationData>();
        }
    }

    private void OnEnable()
    {
        GameClock.OnDayChanged += UpdateProgression;
    }

    private void OnDisable()
    {
        GameClock.OnDayChanged -= UpdateProgression;
    }

    private void Start()
    {
        UpdateProgression(GameClock.Instance.CurrentDay);
    }

    private void UpdateProgression(int currentDay)
    {
        Debug.Log($"Progression Manager: Updating for Day {currentDay}");

        bool stationsChanged = false;

        foreach (var dayProgression in progressionTimeline)
        {
            if (dayProgression.day <= currentDay)
            {
                foreach (var customer in dayProgression.customersToSpawn)
                {
                    if (!AvailableCustomers.Contains(customer))
                    {
                        AvailableCustomers.Add(customer);
                    }
                }

                foreach (var station in dayProgression.stationsToUnlock)
                {
                    if (!AvailableStations.Contains(station))
                    {
                        AvailableStations.Add(station);
                        stationsChanged = true;

                        OnNewStationUnlocked?.Invoke(station);
                    }
                }
            }
        }

        if (stationsChanged)
        {
            OnAvailableStationsChanged?.Invoke();
        }
    }

    public void Debug_UnlockStation(StationData stationData)
    {
        if (stationData != null)
        {
            if (!AvailableStations.Contains(stationData))
            {
                AvailableStations.Add(stationData);
                OnAvailableStationsChanged?.Invoke();
            }

            OnNewStationUnlocked?.Invoke(stationData);
        }
    }
}