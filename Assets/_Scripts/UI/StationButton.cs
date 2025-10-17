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
        stationCostText.text = $"<sprite name=\"Multi_Cash\"> {stationData.PlacementCost}";

        button.onClick.AddListener(OnButtonClicked);

        UpdateInteractableState(EconomyManager.Instance.CurrentCash);
    }

    private void OnEnable()
    {
        EconomyManager.OnCashChanged += UpdateInteractableState;
    }

    private void OnDisable()
    {
        EconomyManager.OnCashChanged -= UpdateInteractableState;
    }

    private void UpdateInteractableState(int currentCash)
    {
        if (stationData != null)
        {
            button.interactable = currentCash >= stationData.PlacementCost;
        }
    }

    private void OnButtonClicked()
    {
        StationPlacement.Instance.BeginPlacingStation(stationData);
    }
}