using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUIManager : MonoBehaviour
{
    public static UpgradeUIManager Instance { get; private set; }

    [Header("Main Panel")]
    [SerializeField] private GameObject upgradePanelContainer;

    [Header("Info Displays")]
    [SerializeField] private TextMeshProUGUI stationNameText;
    [SerializeField] private TextMeshProUGUI stationLevelText;
    [SerializeField] private TextMeshProUGUI stationInfoText;

    [Header("Upgrade Buttons")]
    [SerializeField] private Button specializeButton;
    [SerializeField] private TextMeshProUGUI specializeButtonText;
    [SerializeField] private TextMeshProUGUI specializeCostText;
    [SerializeField] private Button diversifyButton;
    [SerializeField] private TextMeshProUGUI diversifyButtonText;
    [SerializeField] private TextMeshProUGUI diversifyCostText;

    [Header("Product Switching")]
    [SerializeField] private Transform productButtonContainer;
    [SerializeField] private GameObject productButtonPrefab;

    [Header("Initial Product Selection Panel")]
    [SerializeField] private GameObject productSelectPanelContainer;
    [SerializeField] private Transform initialProductButtonContainer;

    private Station currentStation;
    public bool IsPanelOpen => upgradePanelContainer.activeSelf || productSelectPanelContainer.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }

    private void Start()
    {
        upgradePanelContainer.SetActive(false);
        productSelectPanelContainer.SetActive(false);

    }

    public void OpenInitialProductSelection(Station station)
    {
        currentStation = station;
        if (currentStation == null) return;

        foreach (Transform child in initialProductButtonContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var product in station.StationData.AvailableProducts)
        {
            GameObject buttonObj = Instantiate(productButtonPrefab, initialProductButtonContainer);
            if (buttonObj.TryGetComponent<ProductSwitchButton>(out var buttonScript))
            {
                buttonScript.Initialize(product, currentStation, false);
            }
        }

        productSelectPanelContainer.SetActive(true);
        GameLoopManager.Instance.EnterUIMode();
    }

    public void CloseInitialProductSelection()
    {
        productSelectPanelContainer.SetActive(false);
        currentStation = null;
        GameLoopManager.Instance.ExitUIMode();
    }

    public void RequestRefresh()
    {
        if (upgradePanelContainer.activeSelf && currentStation != null)
        {
            RefreshPanel();
        }
    }

    public void OpenPanel(Station station)
    {
        currentStation = station;
        if (currentStation == null) return;

        upgradePanelContainer.SetActive(true);
        RefreshPanel();
        GameLoopManager.Instance.EnterUIMode();
    }

    public void ClosePanel()
    {
        upgradePanelContainer.SetActive(false);
        currentStation = null;
        GameLoopManager.Instance.ExitUIMode();
    }

    private void RefreshPanel()
    {
        if (currentStation == null)
        {
            ClosePanel();
            return;
        }

        stationNameText.text = currentStation.StationData.StationName;
        float currentSpeedBonus = currentStation.SpecializationLevel * currentStation.StationData.SpecializeSpeedBonus * 100;
        stationInfoText.text = $"Speed Bonus: +{currentSpeedBonus:F0}%";
        stationLevelText.text = $"Level {currentStation.SpecializationLevel + 1}";

        bool canSpecialize = currentStation.CanUpgradeSpecialization();
        specializeButton.interactable = canSpecialize;
        if (canSpecialize)
        {
            specializeButtonText.text = $"Cooking Speed";
            specializeCostText.text = $"<sprite name=\"Multi_Cash\"> {currentStation.GetSpecializeCost()}";
        }
        else
        {
            specializeCostText.text = "Max Level";
        }

        bool canDiversify = currentStation.CanUpgradeDiversify();
        diversifyButton.interactable = canDiversify;
        if (canDiversify)
        {
            diversifyButtonText.text = $"Learn Recipe";
            diversifyCostText.text = $"<sprite name=\"Multi_Cash\"> {currentStation.StationData.DiversifyCost}";
        }
        else
        {
            diversifyCostText.text = "Diversified";
        }

        foreach (Transform child in productButtonContainer) Destroy(child.gameObject);
        foreach (var product in currentStation.UnlockedProducts)
        {
            GameObject buttonObj = Instantiate(productButtonPrefab, productButtonContainer);
            if (buttonObj.TryGetComponent<ProductSwitchButton>(out var buttonScript))
            {
                buttonScript.Initialize(product, currentStation, currentStation.CurrentProduct == product);
            }
        }
    }

    public void OnSpecializeClicked()
    {
        if (currentStation != null && currentStation.CanUpgradeSpecialization())
        {
            currentStation.UpgradeSpecialization();
            RefreshPanel();
        }
    }

    public void OnDiversifyClicked()
    {
        if (currentStation != null && currentStation.CanUpgradeDiversify())
        {
            currentStation.UnlockNextProduct();
            RefreshPanel();
        }
    }
}