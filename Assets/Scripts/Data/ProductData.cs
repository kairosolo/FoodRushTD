using UnityEngine;

[CreateAssetMenu(fileName = "NewProductData", menuName = "Food Rush/Product Data")]
public class ProductData : ScriptableObject
{
    [Header("Product Details")]
    [SerializeField] private string productName;
    [SerializeField] private Sprite productIcon;
    [SerializeField] private float basePreparationTime = 5f;

    [Header("Gameplay")]
    [SerializeField] private GameObject foodProjectilePrefab;

    public string ProductName => productName;
    public Sprite ProductIcon => productIcon;
    public float BasePreparationTime => basePreparationTime;
    public GameObject FoodProjectilePrefab => foodProjectilePrefab;
}