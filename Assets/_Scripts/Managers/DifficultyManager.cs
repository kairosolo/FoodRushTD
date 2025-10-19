using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("Daily Scaling")]
    [Tooltip("How much the base difficulty level increases each day.")]
    [SerializeField] private float difficultyIncreasePerDay = 1.0f;
    [SerializeField] private int dayToStartScaling = 2;

    [Header("Spawn Rate Scaling")]
    [Tooltip("The spawn interval will be divided by this value. Higher = faster spawns.")]
    [SerializeField] private float spawnRateDivisor = 1.0f;
    [SerializeField] private float maxSpawnRateDivisor = 4.0f;

    [Header("Customer Speed Scaling")]
    [Tooltip("Customer move speed will be multiplied by this value.")]
    [SerializeField] private float speedMultiplier = 1.0f;
    [SerializeField] private float maxSpeedMultiplier = 2.0f;

    [Header("Customer Order Scaling")]
    [Tooltip("The day on which customers can start ordering additional items.")]
    [SerializeField] private int dayToStartAddingItems = 4;

    [Tooltip("How many days must pass before another potential extra item is added to orders.")]
    [SerializeField] private int daysPerAdditionalItem = 4;

    [Tooltip("The maximum number of additional items a customer can order.")]
    [SerializeField] private int maxAdditionalItems = 0;

    [Tooltip("The amount of extra cash awarded for each additional item requested by a customer due to difficulty scaling.")]
    [SerializeField] private int cashBonusPerAdditionalItem = 10;

    public float SpawnRateDivisor => spawnRateDivisor;
    public float SpeedMultiplier => speedMultiplier;
    public int MaxAdditionalItems => maxAdditionalItems;
    public int CashBonusPerAdditionalItem => cashBonusPerAdditionalItem;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }

    private void OnEnable() => GameClock.OnDayChanged += UpdateDifficulty;

    private void OnDisable() => GameClock.OnDayChanged -= UpdateDifficulty;

    private void Start() => UpdateDifficulty(GameClock.Instance.CurrentDay);

    private void UpdateDifficulty(int currentDay)
    {
        if (currentDay < dayToStartScaling) return;

        int daysSinceScaleStart = currentDay - dayToStartScaling;
        float difficultyLevel = daysSinceScaleStart * difficultyIncreasePerDay;

        spawnRateDivisor = 1.0f + (difficultyLevel * 0.1f);
        spawnRateDivisor = Mathf.Min(spawnRateDivisor, maxSpawnRateDivisor);

        speedMultiplier = 1.0f + (difficultyLevel * 0.05f);
        speedMultiplier = Mathf.Min(speedMultiplier, maxSpeedMultiplier);

        if (currentDay >= dayToStartAddingItems)
        {
            if (daysPerAdditionalItem <= 0) daysPerAdditionalItem = 1;

            int daysSinceItemScaling = currentDay - dayToStartAddingItems;
            maxAdditionalItems = daysSinceItemScaling / daysPerAdditionalItem;
        }
        else
        {
            maxAdditionalItems = 0;
        }

        Debug.Log($"Day {currentDay}: Difficulty Updated. Spawn Divisor: {spawnRateDivisor:F2}, Speed Multiplier: {speedMultiplier:F2}, Max Extra Items: {maxAdditionalItems}");
    }
}