using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProductIconDisplay : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Image productIconImage;
    [SerializeField] private TextMeshProUGUI prepTimeText;

    public void Initialize(Sprite icon, string prepTime)
    {
        if (productIconImage != null)
        {
            productIconImage.sprite = icon;
        }

        if (prepTimeText != null)
        {
            if (!string.IsNullOrEmpty(prepTime))
            {
                prepTimeText.text = prepTime;
                prepTimeText.gameObject.SetActive(true);
            }
            else
            {
                prepTimeText.gameObject.SetActive(false);
            }
        }
    }
}