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

    [Header("Upgrade Path A: Specialize")]
    [SerializeField] private int specializeBaseCost = 50;
    [SerializeField] private float specializeSpeedBonus = 0.15f; // How much faster the station gets per level (ex. 0.15 = 15% faster)
    [SerializeField] private int maxSpecializeLevel = 5;

    [Header("Upgrade Path B: Diversify")]
    [SerializeField] private int diversifyCost = 200;

    [Header("Available Products")]
    [SerializeField] private List<ProductData> availableProducts;

    public Sprite StationIcon => stationIcon;
    public string StationName => stationName;
    public int PlacementCost => placementCost;
    public GameObject StationPrefab => stationPrefab;
    public List<ProductData> AvailableProducts => availableProducts;
    public int SpecializeBaseCost => specializeBaseCost;
    public float SpecializeSpeedBonus => specializeSpeedBonus;
    public int MaxSpecializeLevel => maxSpecializeLevel;
    public int DiversifyCost => diversifyCost;
}