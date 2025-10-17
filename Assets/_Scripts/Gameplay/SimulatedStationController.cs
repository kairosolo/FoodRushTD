using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SimulatedStationController : MonoBehaviour
{
    [Header("Simulation Settings")]
    [SerializeField] private float minSwitchTime = 8f;
    [SerializeField] private float maxSwitchTime = 15f;

    private Station station;
    private float switchProductTimer;

    public void Initialize(Station stationComponent)
    {
        this.station = stationComponent;

        station.TriggerPlacementEffects();

        if (station.StationData.AvailableProducts.Count == 0)
        {
            Debug.LogWarning($"Station {station.StationData.StationName} has no products to make for the simulation.");
            return;
        }

        station.UnlockedProducts.AddRange(station.StationData.AvailableProducts);

        ProductData initialProduct = station.StationData.AvailableProducts[Random.Range(0, station.StationData.AvailableProducts.Count)];
        station.SetInitialProductAndActivate(initialProduct);

        ResetSwitchTimer();
    }

    private void Update()
    {
        if (station == null || station.UnlockedProducts.Count <= 1)
        {
            return;
        }

        switchProductTimer -= Time.deltaTime;
        if (switchProductTimer <= 0)
        {
            SwitchToRandomProduct();
            ResetSwitchTimer();
        }
    }

    private void SwitchToRandomProduct()
    {
        List<ProductData> possibleNewProducts = station.UnlockedProducts
            .Where(p => p != station.CurrentProduct)
            .ToList();

        if (possibleNewProducts.Count > 0)
        {
            ProductData newProduct = possibleNewProducts[Random.Range(0, possibleNewProducts.Count)];
            station.SwitchActiveProduct(newProduct);
        }
    }

    private void ResetSwitchTimer()
    {
        switchProductTimer = Random.Range(minSwitchTime, maxSwitchTime);
    }
}