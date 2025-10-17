using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField]
    private float baseSpawnInterval = 3f;

    [Header("Debug")]
    [SerializeField] private float currentSpawnInterval;

    private float spawnTimer;
    private bool isSpawning = false;

    private void OnEnable()
    {
        GameClock.OnDayPhaseStart += StartSpawning;
        GameClock.OnNightPhaseStart += StopSpawning;
    }

    private void OnDisable()
    {
        GameClock.OnDayPhaseStart -= StartSpawning;
        GameClock.OnNightPhaseStart -= StopSpawning;
    }

    private void Start()
    {
        if (GameLoopManager.Instance.CurrentState == GameLoopManager.GameState.Action)
        {
            StartSpawning();
        }
    }

    private void Update()
    {
        if (!isSpawning) return;

        spawnTimer += Time.deltaTime;

        float currentSpawnInterval = baseSpawnInterval / DifficultyManager.Instance.SpawnRateDivisor;
        this.currentSpawnInterval = currentSpawnInterval;
        if (spawnTimer >= currentSpawnInterval)
        {
            spawnTimer -= currentSpawnInterval;
            SpawnCustomer();
        }
    }

    private void StartSpawning() => isSpawning = true;

    private void StopSpawning() => isSpawning = false;

    private void SpawnCustomer()
    {
        CustomerData customerToSpawn = GetCustomerToSpawn();

        if (customerToSpawn == null)
        {
            Debug.LogWarning("Could not determine a customer to spawn!");
            return;
        }

        GameObject customerObject = Instantiate(customerToSpawn.CustomerPrefab);
        if (customerObject.TryGetComponent<Customer>(out Customer customer))
        {
            customer.Initialize(customerToSpawn);
        }
        AudioManager.Instance.PlaySFX("Customer_Spawn");
    }

    private CustomerData GetCustomerToSpawn()
    {
        DailyEventData activeEvent = DailyEventManager.Instance.ActiveEvent;
        List<CustomerData> validCustomerPool = GetValidCustomerPool();

        if (validCustomerPool.Count == 0) return null;

        if (activeEvent != null && activeEvent.FeaturedCustomers.Count > 0)
        {
            List<CustomerData> weightedPool = new List<CustomerData>();

            weightedPool.AddRange(validCustomerPool);

            foreach (var featuredCustomer in activeEvent.FeaturedCustomers)
            {
                if (validCustomerPool.Contains(featuredCustomer))
                {
                    for (int i = 0; i < activeEvent.EventWeight; i++)
                    {
                        weightedPool.Add(featuredCustomer);
                    }
                }
            }

            return weightedPool[Random.Range(0, weightedPool.Count)];
        }

        return validCustomerPool[Random.Range(0, validCustomerPool.Count)];
    }

    private List<CustomerData> GetValidCustomerPool()
    {
        List<CustomerData> validCustomers = new List<CustomerData>();
        List<ProductData> unlockedProducts = new List<ProductData>();

        foreach (var station in ProgressionManager.Instance.AvailableStations)
        {
            unlockedProducts.AddRange(station.AvailableProducts);
        }
        unlockedProducts = unlockedProducts.Distinct().ToList();

        foreach (var customerData in ProgressionManager.Instance.AvailableCustomers)
        {
            if (customerData.PotentialOrder.All(orderItem => unlockedProducts.Contains(orderItem.product)))
            {
                validCustomers.Add(customerData);
            }
        }

        return validCustomers;
    }
}