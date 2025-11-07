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

    [Header("Product Display")]
    [SerializeField] private GameObject unlockedProductsDisplayContainer;
    [SerializeField] private GameObject productIconDisplayPrefab;

    [Header("Sell Button")]
    [SerializeField] private Button sellButton;
    [SerializeField] private TextMeshProUGUI sellButtonText;

    [Range(0f, 1f)]
    [SerializeField] private float sellRefundPercentage = 0.75f;

    [Header("Initial Product Selection Panel")]
    [SerializeField] private GameObject productSelectPanelContainer;
    [SerializeField] private Transform initialProductButtonContainer;
    [SerializeField] private GameObject productButtonPrefab;

    [Header("Tooltip Buttons")]
    [SerializeField] private TooltipTrigger specializeTooltipTrigger;
    [SerializeField] private TooltipTrigger diversifyTooltipTrigger;
    private Station currentStation;
    public bool IsUpgradePanelOpen => upgradePanelContainer.activeSelf;
    public bool IsInitialProductSelectionOpen => productSelectPanelContainer.activeSelf;
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

    private void OnEnable()
    {
        EconomyManager.OnCashChanged += HandleCashChanged;
    }

    private void OnDisable()
    {
        EconomyManager.OnCashChanged -= HandleCashChanged;
    }

    private void HandleCashChanged(int newCash)
    {
        if (IsUpgradePanelOpen && currentStation != null)
        {
            RefreshPanel();
        }
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

        int currentCash = EconomyManager.Instance.CurrentCash;

        stationNameText.text = currentStation.StationData.StationName;
        stationLevelText.text = $"Level {currentStation.SpecializationLevel + 1}";
        float speedMultiplier = 1f + (currentStation.SpecializationLevel * currentStation.StationData.SpecializeSpeedBonus);
        stationInfoText.text = $"Speed: x{speedMultiplier:F2}";

        bool canSpecialize = currentStation.CanUpgradeSpecialization();
        int specializeCost = currentStation.GetSpecializeCost();
        specializeButton.interactable = canSpecialize && currentCash >= specializeCost;
        if (canSpecialize)
        {
            specializeButtonText.text = "Improve\nPrep Time";
            specializeCostText.text = $"<sprite name=\"Multi_Cash\"> {specializeCost}";
        }
        else
        {
            specializeButtonText.text = "Improve\nPrep Time";
            specializeCostText.text = "Max Level";
        }

        if (specializeTooltipTrigger != null)
        {
            float bonus = currentStation.StationData.SpecializeSpeedBonus * 100;
            specializeTooltipTrigger.SetText("Prep Time (Specialize)", $"Increases this station's cooking speed for all its recipes by {bonus}%.");
        }

        int diversifyLevel = currentStation.DiversifyLevel;
        bool canDiversify = diversifyLevel < 5;
        int diversifyCost = currentStation.GetNextDiversifyCost();
        diversifyButton.interactable = canDiversify && currentCash >= diversifyCost;

        if (canDiversify)
        {
            diversifyCostText.text = $"<sprite name=\"Multi_Cash\"> {diversifyCost}";
        }
        else
        {
            diversifyCostText.text = "Max Level";
        }

        string diversifyTitle = "Learn Recipe";
        string diversifyDesc = "Unlocks a new recipe for this station, allowing it to cook more types of food.";

        switch (diversifyLevel)
        {
            case 0:
                diversifyButtonText.text = "Learn\nRecipe";
                break;

            case 1:
                diversifyButtonText.text = "Master\nRecipes";
                diversifyTitle = "Master Recipes";
                diversifyDesc = "Unlocks a second cooking slot, allowing the station to prepare two different items simultaneously.";
                break;

            case 2:
            case 3:
            case 4:
                diversifyButtonText.text = $"Increase\nCapacity {diversifyLevel - 1}";
                diversifyTitle = $"Increase Capacity {diversifyLevel - 1}";
                diversifyDesc = $"Increases the maximum stack size for each recipe, allowing the station to hold up to {diversifyLevel} of each item at once.";
                break;

            default:
                diversifyButtonText.text = "Max\nCapacity";
                break;
        }

        if (diversifyTooltipTrigger != null)
        {
            diversifyTooltipTrigger.SetText(diversifyTitle, diversifyDesc);
        }

        UpdateProductDisplay();
        int refundAmount = Mathf.FloorToInt(currentStation.TotalValue * sellRefundPercentage);
        sellButtonText.text = $"Sell\n${refundAmount}";
        UpdateUpgradePips(specializePipsContainer, currentStation.SpecializationLevel, currentStation.StationData.MaxSpecializeLevel);
        UpdateUpgradePips(diversifyPipsContainer, currentStation.DiversifyLevel, 5);
    }

    private void UpdateProductDisplay()
    {
        if (unlockedProductsDisplayContainer == null) return;

        foreach (Transform child in unlockedProductsDisplayContainer.transform)
        {
            Destroy(child.gameObject);
        }

        unlockedProductsDisplayContainer.SetActive(true);

        foreach (ProductData product in currentStation.UnlockedProducts)
        {
            GameObject iconObj = Instantiate(productIconDisplayPrefab, unlockedProductsDisplayContainer.transform);
            if (iconObj.TryGetComponent<ProductIconDisplay>(out var displayScript))
            {
                float prepTime = currentStation.GetPreparationTime(product, currentStation.SpecializationLevel);
                string prepTimeString = $"{prepTime:F2}s";

                displayScript.Initialize(product.ProductIconUI, prepTimeString);
            }
        }
    }

    private void UpdateUpgradePips(Transform container, int currentLevel, int maxLevel)
    {
        if (container == null) return;

        for (int i = 0; i < container.childCount; i++)
        {
            Transform pip = container.GetChild(i);
            Image pipImage = pip.GetComponent<Image>();

            if (pipImage == null) continue;

            if (i <= maxLevel)
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
        if (currentStation != null)
        {
            currentStation.UpgradeDiversifyPath();
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