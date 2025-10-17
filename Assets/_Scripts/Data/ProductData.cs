using UnityEngine;

[CreateAssetMenu(fileName = "NewProductData", menuName = "Food Rush/Product Data")]
public class ProductData : ScriptableObject
{
    public enum ProjectileMovementType
    {
        Linear, // Straight line
        Lob,    // Arcs through the air
        Spiral, // Spirals towards the target
        Wobble  // Moves in a sine wave
    }

    [Header("Product Details")]
    [SerializeField] private string productName;
    [SerializeField] private Sprite productSprite;
    [SerializeField] private Sprite productIconUI;
    [SerializeField] private float basePreparationTime = 5f;

    [Header("Gameplay")]
    [SerializeField] private GameObject foodProjectilePrefab;

    [Header("Projectile Behavior")]
    [SerializeField] private ProjectileMovementType movementType = ProjectileMovementType.Linear;
    [SerializeField] private float projectileSpeed = 5f;

    [Header("Curve Settings (Lob, Spiral, Wobble)")]
    [Tooltip("For Lob: The height of the arc. For Spiral/Wobble: The width of the motion.")]
    [SerializeField] private float amplitude = 1f;

    [Tooltip("For Spiral/Wobble: The number of spirals or wobbles over the path.")]
    [SerializeField] private float frequency = 2f;

    public string ProductName => productName;
    public Sprite ProductSprite => productSprite;
    public Sprite ProductIconUI => productIconUI;
    public float BasePreparationTime => basePreparationTime;
    public GameObject FoodProjectilePrefab => foodProjectilePrefab;

    public ProjectileMovementType MovementType => movementType;
    public float ProjectileSpeed => projectileSpeed;
    public float Amplitude => amplitude;
    public float Frequency => frequency;
}