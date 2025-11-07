using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StationButton : MonoBehaviour
{
    [SerializeField] private Image stationIcon;
    [SerializeField] private TextMeshProUGUI stationNameText;
    [SerializeField] private TextMeshProUGUI stationCostText;
    [SerializeField] private Button button;
    [SerializeField] private Color disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);

    private StationData stationData;
    private Color originalIconColor;
    private Color originalNameColor;
    private Color originalCostColor;

    private void Awake()
    {
        originalIconColor = stationIcon.color;
        originalNameColor = stationNameText.color;
        originalCostColor = stationCostText.color;
    }

    public void Initialize(StationData data)
    {
        stationData = data;
        stationIcon.sprite = stationData.StationIcon;
        stationNameText.text = stationData.StationName;
        button.onClick.AddListener(OnButtonClicked);
    }

    private void Start()
    {
        ForceUpdateState();
    }

    private void OnEnable()
    {
        EconomyManager.OnCashChanged += UpdateStateFromCash;
        StationManager.OnStationCountChanged += ForceUpdateState;
        ForceUpdateState();
    }

    private void OnDisable()
    {
        EconomyManager.OnCashChanged -= UpdateStateFromCash;
        StationManager.OnStationCountChanged -= ForceUpdateState;
    }

    private void ForceUpdateState()
    {
        if (EconomyManager.Instance != null && stationData != null)
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
            bool canAfford = currentCash >= currentCost;
            button.interactable = canAfford;

            if (canAfford)
            {
                stationIcon.color = originalIconColor;
                stationNameText.color = originalNameColor;
                stationCostText.color = originalCostColor;
            }
            else
            {
                stationIcon.color = disabledColor;
                stationNameText.color = disabledColor;
                stationCostText.color = disabledColor;
            }
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