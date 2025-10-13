using UnityEngine;
using UnityEngine.UI;

public class ProductSwitchButton : MonoBehaviour
{
    [SerializeField] private Image productIcon;
    [SerializeField] private Button button;
    [SerializeField] private GameObject selectedIndicator;

    private ProductData productData;
    private Station currentStation;

    public void Initialize(ProductData product, Station station, bool isSelected)
    {
        productData = product;
        currentStation = station;

        productIcon.sprite = product.ProductIconUI;
        selectedIndicator.SetActive(isSelected);

        button.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        if (currentStation.enabled == false)
        {
            currentStation.SetInitialProductAndActivate(productData);
            UpgradeUIManager.Instance.CloseInitialProductSelection();
        }
        else
            currentStation.SwitchActiveProduct(productData);
    }
}