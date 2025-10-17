using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class StationPlacement : MonoBehaviour
{
    public static StationPlacement Instance { get; private set; }

    [Header("Visuals")]
    [SerializeField] private Color ghostColorValid = new Color(0.5f, 1f, 0.5f, 0.5f); // Green
    [SerializeField] private Color ghostColorInvalid = new Color(1f, 0.5f, 0.5f, 0.5f); // Red

    private Camera mainCamera;
    private StationData stationToPlace;

    private GameObject stationGhost;
    private SpriteRenderer ghostRenderer;
    private bool currentPlacementIsValid;
    private bool isPointerOverUI = false;

    public bool IsPlacing { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        isPointerOverUI = EventSystem.current.IsPointerOverGameObject();

        if (IsPlacing && stationGhost != null)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, mainCamera.nearClipPlane));
            stationGhost.transform.position = new Vector2(worldPosition.x, worldPosition.y);

            currentPlacementIsValid = PlacementManager.Instance.IsValidPlacement(stationGhost.transform.position);

            ghostRenderer.color = currentPlacementIsValid ? ghostColorValid : ghostColorInvalid;
        }
    }

    public void BeginPlacingStation(StationData stationData)
    {
        if (IsPlacing) CancelPlacement();
        if (EconomyManager.Instance.CurrentCash < stationData.PlacementCost) return;

        IsPlacing = true;
        stationToPlace = stationData;

        stationGhost = Instantiate(stationToPlace.StationPrefab);
        ghostRenderer = stationGhost.GetComponentInChildren<SpriteRenderer>();

        if (stationGhost.TryGetComponent<Station>(out Station station))
        {
            station.enabled = false;
            station.ShowRange();
        }
    }

    public void OnPlaceStation(InputAction.CallbackContext context)
    {
        if (isPointerOverUI)
        {
            return;
        }
        if (context.performed && IsPlacing && currentPlacementIsValid)
        {
            FinishPlacing();
        }
    }

    public void OnCancelPlacement(InputAction.CallbackContext context)
    {
        if (context.performed && IsPlacing) CancelPlacement();
    }

    private void FinishPlacing()
    {
        if (!EconomyManager.Instance.SpendCash(stationToPlace.PlacementCost))
        {
            CancelPlacement();
            return;
        }

        ghostRenderer.color = Color.white;

        if (stationGhost.TryGetComponent<Station>(out Station station))
        {
            station.HideRange();
            station.PartialInitialize(stationToPlace);

            station.TriggerPlacementEffects();

            UpgradeUIManager.Instance.OpenInitialProductSelection(station);
        }

        AudioManager.Instance.PlaySFX("Station_Place");

        IsPlacing = false;
        stationGhost = null;
        stationToPlace = null;
    }

    private void CancelPlacement()
    {
        Destroy(stationGhost);
        IsPlacing = false;
        stationGhost = null;
        stationToPlace = null;
    }
}