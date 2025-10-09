using UnityEngine;
using System.Collections.Generic;

public class PathManager : MonoBehaviour
{
    public static PathManager Instance { get; private set; }

    [SerializeField] private List<Transform> pathWaypoints;

    public int WaypointCount => pathWaypoints.Count;

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
        if (index < 0 || index >= pathWaypoints.Count)
        {
            Debug.LogError($"PathManager: Invalid waypoint index {index}");
            return null;
        }
        return pathWaypoints[index];
    }
}