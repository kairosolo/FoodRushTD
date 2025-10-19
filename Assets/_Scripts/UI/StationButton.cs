using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StationButton : MonoBehaviour
{
    [SerializeField] private Image stationIcon;
    [SerializeField] private TextMeshProUGUI stationNameText;
    [SerializeField] private TextMeshProUGUI stationCostText;
    [SerializeField] private Button button;

    private StationData stationData;

    public void Initialize(StationData data)
    {
        stationData = data;
        stationIcon.sprite = stationData.StationIcon;
        stationNameText.text = stationData.StationName;
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnEnable()
    {
        ForceUpdateState();

        EconomyManager.OnCashChanged += UpdateStateFromCash;
        StationManager.OnStationCountChanged += ForceUpdateState;
    }

    private void OnDisable()
    {
        EconomyManager.OnCashChanged -= UpdateStateFromCash;
        StationManager.OnStationCountChanged -= ForceUpdateState;
    }

    private void ForceUpdateState()
    {
        if (EconomyManager.Instance != null)
        {
            UpdateStateFromCash(EconomyManager.Instance.CurrentCash);
        }
    }

    private void UpdateStateFromCash(int currentCash)
    {
        if (stationData != null && StationPlacement.Instance != null)
        {
            int currentCost = StationPlacement.Instance.GetCurrentPlacementCost(stationData);
            stationCostText.text = $"<sprite name=\"Multi_Cash\"> {currentCost}";
            button.interactable = currentCash >= currentCost;
        }
    }

    private void OnButtonClicked()
    {
        if (StationPlacement.Instance != null)
        {
            StationPlacement.Instance.BeginPlacingStation(stationData);
        }
    }
}