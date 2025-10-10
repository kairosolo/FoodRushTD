using UnityEngine;
using UnityEngine.InputSystem;

public class StationInteraction : MonoBehaviour
{
    private Camera mainCamera;
    [SerializeField] private StationPlacement stationPlacement;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (stationPlacement.IsPlacing) return;

        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Station closestStation = null;
        float shortestDistance = float.MaxValue;

        foreach (var station in StationManager.Instance.GetActiveStations())
        {
            float distance = Vector2.Distance(mouseWorldPos, station.transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closestStation = station;
            }
        }

        if (closestStation != null && shortestDistance <= closestStation.ClickRadius)
        {
            closestStation.CycleProduct();
        }
    }
}