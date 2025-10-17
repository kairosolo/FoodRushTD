using UnityEngine;
using System.Collections.Generic;

public class PathManager : MonoBehaviour
{
    public static PathManager Instance { get; private set; }

    [SerializeField] private List<Transform> pathWaypoints;

    private bool isSimulationMode => MenuSimulationManager.Instance != null;

    public int WaypointCount => isSimulationMode ? MenuSimulationManager.Instance.CustomerPathWaypoints.Count : pathWaypoints.Count;

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

    public Transform GetWaypoint(int index)
    {
        List<Transform> currentWaypoints = isSimulationMode ? MenuSimulationManager.Instance.CustomerPathWaypoints : pathWaypoints;

        if (index < 0 || index >= currentWaypoints.Count)
        {
            Debug.LogError($"PathManager: Invalid waypoint index {index}");
            return null;
        }
        return currentWaypoints[index];
    }
}