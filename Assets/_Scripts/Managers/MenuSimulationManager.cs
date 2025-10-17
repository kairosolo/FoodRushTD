using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class MenuSimulationManager : MonoBehaviour
{
    public static MenuSimulationManager Instance { get; private set; }

    [Header("Simulation Content")]
    [SerializeField] private List<StationData> stationsToPlace;
    [SerializeField] private List<CustomerData> customersToSpawn;

    [Header("Simulation Configuration")]
    [SerializeField] private List<Transform> stationPlacementPoints;
    [SerializeField] private List<Transform> customerPathWaypoints;
    [SerializeField] private float customerSpawnInterval = 4f;

    [Header("Spawn Timing")]
    [SerializeField] private float initialSpawnDelay = 1.5f;
    [SerializeField] private float delayBetweenStations = 0.5f;

    private float spawnTimer;
    private bool canSpawnCustomers = false;

    public List<Transform> CustomerPathWaypoints => customerPathWaypoints;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (stationsToPlace.Count == 0 || customersToSpawn.Count == 0 || stationPlacementPoints.Count == 0)
        {
            Debug.LogError("MenuSimulationManager is not configured. Disabling simulation.");
            this.enabled = false;
            return;
        }

        canSpawnCustomers = true;

        StartCoroutine(SpawnStationsSequentially());
    }

    private void Update()
    {
        if (!canSpawnCustomers) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= customerSpawnInterval)
        {
            spawnTimer -= customerSpawnInterval;
            SpawnSimulatedCustomer();
        }
    }

    private IEnumerator SpawnStationsSequentially()
    {
        yield return new WaitForSeconds(initialSpawnDelay);

        List<StationData> stationPool = new List<StationData>(stationsToPlace);

        List<Transform> availablePlacementPoints = new List<Transform>(stationPlacementPoints);
        for (int i = 0; i < availablePlacementPoints.Count - 1; i++)
        {
            int randomIndex = Random.Range(i, availablePlacementPoints.Count);
            Transform temp = availablePlacementPoints[i];
            availablePlacementPoints[i] = availablePlacementPoints[randomIndex];
            availablePlacementPoints[randomIndex] = temp;
        }

        List<StationData> uniqueStations = stationPool.Distinct().ToList();

        for (int i = 0; i < uniqueStations.Count; i++)
        {
            if (availablePlacementPoints.Count == 0) break;

            SpawnSingleStation(uniqueStations[i], availablePlacementPoints[0]);
            availablePlacementPoints.RemoveAt(0);
            yield return new WaitForSeconds(delayBetweenStations);
        }

        while (availablePlacementPoints.Count > 0)
        {
            StationData randomStationData = stationPool[Random.Range(0, stationPool.Count)];
            SpawnSingleStation(randomStationData, availablePlacementPoints[0]);
            availablePlacementPoints.RemoveAt(0);
            yield return new WaitForSeconds(delayBetweenStations);
        }
    }

    private void SpawnSingleStation(StationData data, Transform spawnPoint)
    {
        if (data == null || spawnPoint == null) return;

        GameObject stationObj = Instantiate(data.StationPrefab, spawnPoint.position, spawnPoint.rotation);

        if (stationObj.TryGetComponent<Station>(out var station))
        {
            station.PartialInitialize(data);
            var controller = stationObj.AddComponent<SimulatedStationController>();
            controller.Initialize(station);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("Station_Place");
            }
        }
    }

    private void SpawnSimulatedCustomer()
    {
        if (customersToSpawn.Count == 0) return;

        CustomerData data = customersToSpawn[Random.Range(0, customersToSpawn.Count)];
        GameObject customerObj = Instantiate(data.CustomerPrefab);
        if (customerObj.TryGetComponent<Customer>(out var customer))
        {
            customer.Initialize(data);
        }
    }
}