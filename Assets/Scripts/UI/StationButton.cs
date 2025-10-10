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
        stationCostText.text = $"${stationData.PlacementCost}";

        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        StationPlacement.Instance.BeginPlacingStation(stationData);
    }
}