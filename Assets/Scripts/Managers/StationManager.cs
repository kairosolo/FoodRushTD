using UnityEngine;
using System.Collections.Generic;

public class StationManager : MonoBehaviour
{
    public static StationManager Instance { get; private set; }

    private List<Station> activeStations = new List<Station>();

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
        if (!activeStations.Contains(station))
        {
            activeStations.Add(station);
        }
    }

    public void RemoveStation(Station station)
    {
        if (activeStations.Contains(station))
        {
            activeStations.Remove(station);
        }
    }

    public List<Station> GetActiveStations()
    {
        return activeStations;
    }
}