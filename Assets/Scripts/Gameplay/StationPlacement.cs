using UnityEngine;
using UnityEngine.InputSystem;

public class StationPlacement : MonoBehaviour
{
    [Header("Placement Settings")]
    [SerializeField] private StationData stationToPlace;
    [SerializeField] private LayerMask placementLayer;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    public void OnPlaceStation(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        PlaceStationAt(mousePosition);
    }

    private void PlaceStationAt(Vector2 screenPosition)
    {
        if (!EconomyManager.Instance.SpendCash(stationToPlace.PlacementCost))
        {
            return;
        }

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane));
        Vector2 placementPosition = new Vector2(worldPosition.x, worldPosition.y);

        GameObject stationObject = Instantiate(stationToPlace.StationPrefab, placementPosition, Quaternion.identity);

        if (stationObject.TryGetComponent<Station>(out Station station))
        {
            station.Initialize(stationToPlace);
        }

        // Trigger PlaceStationSFX
        Debug.Log($"Placed {stationToPlace.StationName} at {placementPosition}");
    }
}