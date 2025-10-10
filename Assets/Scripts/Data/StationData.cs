using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewStationData", menuName = "Food Rush/Station Data")]
public class StationData : ScriptableObject
{
    [Header("Station Details")]
    [SerializeField] private Sprite stationIcon;
    [SerializeField] private string stationName;
    [SerializeField] private int placementCost;
    [SerializeField] private GameObject stationPrefab;

    [Header("Available Products")]
    [SerializeField] private List<ProductData> availableProducts;

    public Sprite StationIcon => stationIcon;
    public string StationName => stationName;
    public int PlacementCost => placementCost;
    public GameObject StationPrefab => stationPrefab;
    public List<ProductData> AvailableProducts => availableProducts;
}