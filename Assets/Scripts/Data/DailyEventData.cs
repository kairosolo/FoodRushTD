using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDailyEvent", menuName = "Food Rush/Daily Event Data")]
public class DailyEventData : ScriptableObject
{
    [Header("Event Details")]
    [SerializeField] private string eventName;

    [TextArea(3, 5)]
    [SerializeField] private string eventDescription;

    [Header("Gameplay Rules")]
    [SerializeField] private List<CustomerData> featuredCustomers;

    [Range(0, 100)]
    [SerializeField] private int featuredCustomerChance = 75;

    public string EventName => eventName;
    public string EventDescription => eventDescription;
    public List<CustomerData> FeaturedCustomers => featuredCustomers;
    public int FeaturedCustomerChance => featuredCustomerChance;
}