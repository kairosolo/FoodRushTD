using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class StationInteraction : MonoBehaviour
{
    private Camera mainCamera;
    [SerializeField] private StationPlacement stationPlacement;
    private bool isPointerOverUI = false;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        isPointerOverUI = EventSystem.current.IsPointerOverGameObject();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (isPointerOverUI)
        {
            return;
        }

        if (UpgradeUIManager.Instance != null && UpgradeUIManager.Instance.IsInitialProductSelectionOpen)
        {
            return;
        }

        if (!context.performed || stationPlacement.IsPlacing)
        {
            return;
        }

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
            if (closestStation.CanInteract())
            {
                UpgradeUIManager.Instance.OpenPanel(closestStation);
            }
        }
        else
        {
            if (UpgradeUIManager.Instance.IsUpgradePanelOpen)
            {
                UpgradeUIManager.Instance.ClosePanel();
            }
        }
    }
}