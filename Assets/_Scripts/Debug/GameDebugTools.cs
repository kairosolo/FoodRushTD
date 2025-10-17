using UnityEngine;

public class GameDebugTools : MonoBehaviour
{
    [Header("Event Testing")]
    public DailyEventData eventToTrigger;

    [Header("Progression Testing")]
    public StationData stationToUnlock;

    [Header("Economy Testing")]
    public int cashToAdd = 500;

    [Header("Game State Testing")]
    public int unsatisfiedCustomersToAdd = 1;
}