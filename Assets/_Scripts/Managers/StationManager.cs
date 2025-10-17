using UnityEngine;
using System.Collections.Generic;
using System;

public class StationManager : MonoBehaviour
{
    public static StationManager Instance { get; private set; }

    public static event Action OnStationCountChanged;

    private List<Station> activeStations = new List<Station>();
    private Dictionary<StationData, int> stationCounts = new Dictionary<StationData, int>();

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

    public void AddStation(Station station)
    {
        if (station == null || station.StationData == null) return;

        if (!activeStations.Contains(station))
        {
            activeStations.Add(station);

            if (stationCounts.ContainsKey(station.StationData))
            {
                stationCounts[station.StationData]++;
            }
            else
            {
                stationCounts.Add(station.StationData, 1);
            }

            OnStationCountChanged?.Invoke();
        }
    }

    public void RemoveStation(Station station)
    {
        if (station == null || station.StationData == null) return;

        if (activeStations.Contains(station))
        {
            activeStations.Remove(station);

            if (stationCounts.ContainsKey(station.StationData))
            {
                stationCounts[station.StationData]--;
                if (stationCounts[station.StationData] <= 0)
                {
                    stationCounts.Remove(station.StationData);
                }
            }

            OnStationCountChanged?.Invoke();
        }
    }

    public List<Station> GetActiveStations()
    {
        return activeStations;
    }

    public int GetStationCount(StationData stationData)
    {
        return stationCounts.ContainsKey(stationData) ? stationCounts[stationData] : 0;
    }
}