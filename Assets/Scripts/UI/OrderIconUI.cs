using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrderIconUI : MonoBehaviour
{
    [SerializeField] private Image productIconImage;
    [SerializeField] private TextMeshProUGUI quantityText;

    public void Initialize(Sprite icon, int quantity)
    {
        productIconImage.sprite = icon;
        UpdateQuantity(quantity);
    }

    public void UpdateQuantity(int quantity)
    {
        if (quantity > 1)
        {
            quantityText.text = $"x{quantity}";
            quantityText.gameObject.SetActive(true);
        }
        else
        {
            quantityText.gameObject.SetActive(false);
        }
    }
}