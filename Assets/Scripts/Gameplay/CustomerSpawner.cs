using UnityEngine;
using System.Collections.Generic;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private float spawnInterval = 3f;

    [Header("Order Settings")]
    [SerializeField] private ProductData friesData;

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
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer -= spawnInterval;
            SpawnCustomer();
        }
    }

    private void StartSpawning()
    {
        isSpawning = true;
        spawnTimer = 0f;
    }

    private void StopSpawning()
    {
        isSpawning = false;
    }

    private void SpawnCustomer()
    {
        if (customerPrefab == null || friesData == null)
        {
            Debug.LogError("Customer Prefab or Fries Data is not assigned in the spawner!");
            return;
        }

        GameObject customerObject = Instantiate(customerPrefab);

        if (customerObject.TryGetComponent<Customer>(out Customer customer))
        {
            // Give the customer an order for 1 Fries (for now)
            List<ProductData> newOrder = new List<ProductData> { friesData };
            customer.Initialize(newOrder);
        }
        // Trigger CustomerSpawnSFX
    }
}