using UnityEngine;
using System.Collections.Generic;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance { get; private set; }

    [Header("Placement Rules")]
    [SerializeField] private float pathClearanceRadius = 1.0f;
    [SerializeField] private float stationClearanceRadius = 1.0f;

    private Camera mainCamera;
    private List<Transform> pathWaypoints;

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
        mainCamera = Camera.main;
        pathWaypoints = new List<Transform>();
        for (int i = 0; i < PathManager.Instance.WaypointCount; i++)
        {
            pathWaypoints.Add(PathManager.Instance.GetWaypoint(i));
        }
    }

    public bool IsValidPlacement(Vector2 position)
    {
        if (!IsWithinScreenBounds(position))
        {
            Debug.Log("Placement failed: Outside screen bounds.");
            return false;
        }

        if (!IsClearOfPath(position))
        {
            Debug.Log("Placement failed: Too close to the customer path.");
            return false;
        }

        if (!IsClearOfOtherStations(position))
        {
            Debug.Log("Placement failed: Too close to another station.");
            return false;
        }

        return true;
    }

    private bool IsWithinScreenBounds(Vector2 position)
    {
        Vector3 screenPoint = mainCamera.WorldToViewportPoint(position);
        return screenPoint.x > 0.05f && screenPoint.x < 0.95f &&
               screenPoint.y > 0.05f && screenPoint.y < 0.95f;
    }

    private bool IsClearOfPath(Vector2 position)
    {
        for (int i = 0; i < pathWaypoints.Count - 1; i++)
        {
            Vector2 waypointStart = pathWaypoints[i].position;
            Vector2 waypointEnd = pathWaypoints[i + 1].position;

            float distance = DistancePointToLineSegment(position, waypointStart, waypointEnd);

            if (distance < pathClearanceRadius)
            {
                return false;
            }
        }
        return true;
    }

    private bool IsClearOfOtherStations(Vector2 position)
    {
        foreach (var station in StationManager.Instance.GetActiveStations())
        {
            float distance = Vector2.Distance(position, station.transform.position);
            if (distance < stationClearanceRadius)
            {
                return false;
            }
        }
        return true;
    }

    private float DistancePointToLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        float lineLengthSquared = (lineEnd - lineStart).sqrMagnitude;
        if (lineLengthSquared == 0.0f)
        {
            return Vector2.Distance(point, lineStart);
        }

        float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(point - lineStart, lineEnd - lineStart) / lineLengthSquared));

        Vector2 projection = lineStart + t * (lineEnd - lineStart);

        return Vector2.Distance(point, projection);
    }
}