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

    [Header("Upgrade Pip Visuals")]
    [SerializeField] private Transform specializePipsContainer;
    [SerializeField] private Transform diversifyPipsContainer;
    [SerializeField] private Color filledPipColor = Color.yellow;
    [SerializeField] private Color emptyPipColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("Product Switching")]
    [SerializeField] private Transform productButtonContainer;
    [SerializeField] private GameObject productButtonPrefab;

    [Header("Sell Button")]
    [SerializeField] private Button sellButton;
    [SerializeField] private TextMeshProUGUI sellButtonText;

    [Range(0f, 1f)]
    [SerializeField] private float sellRefundPercentage = 0.75f;

    [Header("Initial Product Selection Panel")]
    [SerializeField] private GameObject productSelectPanelContainer;
    [SerializeField] private Transform initialProductButtonContainer;

    private Station currentStation;
    public bool IsUpgradePanelOpen => upgradePanelContainer.activeSelf;
    public bool IsPanelOpen => IsUpgradePanelOpen || productSelectPanelContainer.activeSelf;

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
        AudioManager.Instance.PlaySFX("UI_Panel_Open");
        GameLoopManager.Instance.EnterUIMode();
    }

    public void CloseInitialProductSelection()
    {
        productSelectPanelContainer.SetActive(false);
        AudioManager.Instance.PlaySFX("UI_Panel_Close");
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
        if (currentStation != null && currentStation != station)
        {
            currentStation.HideRange();
        }

        currentStation = station;
        if (currentStation == null) return;

        currentStation.ShowRange();
        upgradePanelContainer.SetActive(true);
        AudioManager.Instance.PlaySFX("UI_Panel_Open");
        RefreshPanel();
        GameLoopManager.Instance.EnterUIMode();
    }

    public void ClosePanel()
    {
        if (currentStation != null)
        {
            currentStation.HideRange();
        }

        upgradePanelContainer.SetActive(false);
        AudioManager.Instance.PlaySFX("UI_Panel_Close");
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
            diversifyCostText.text = "Max Level";
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

        int refundAmount = Mathf.FloorToInt(currentStation.TotalValue * sellRefundPercentage);
        sellButtonText.text = $"Sell\n${refundAmount}";

        UpdateUpgradePips(specializePipsContainer,
                         currentStation.SpecializationLevel,
                         currentStation.StationData.MaxSpecializeLevel);

        int diversifyLevel = currentStation.UnlockedProducts.Count - 1;
        int maxDiversifyLevel = currentStation.StationData.AvailableProducts.Count - 1;
        UpdateUpgradePips(diversifyPipsContainer, diversifyLevel, maxDiversifyLevel);
    }

    private void UpdateUpgradePips(Transform container, int currentLevel, int maxLevel)
    {
        if (container == null) return;

        for (int i = 0; i < container.childCount; i++)
        {
            Transform pip = container.GetChild(i);
            Image pipImage = pip.GetComponent<Image>();

            if (pipImage == null) continue;

            if (i < maxLevel)
            {
                pip.gameObject.SetActive(true);
                pipImage.color = (i < currentLevel) ? filledPipColor : emptyPipColor;
            }
            else
            {
                pip.gameObject.SetActive(false);
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

    public void OnSellClicked()
    {
        if (currentStation == null) return;

        Station stationToSell = currentStation;

        int refundAmount = Mathf.FloorToInt(stationToSell.TotalValue * sellRefundPercentage);
        EconomyManager.Instance.AddCash(refundAmount);

        ClosePanel();

        AudioManager.Instance.PlaySFX("Station_Sell");

        if (stationToSell != null)
        {
            Destroy(stationToSell.gameObject);
        }
    }
}