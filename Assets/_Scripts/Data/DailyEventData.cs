using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDailyEvent", menuName = "Food Rush/Daily Event Data")]
public class DailyEventData : ScriptableObject
{
    public enum EventType
    { DemandFocus, ProductCraze, ChallengeModifier, Boon }

    [Header("Event Details")]
    [SerializeField] private Sprite eventIcon;
    [SerializeField] private string eventName;

    [TextArea(3, 5)]
    [SerializeField] private string eventDescription;
    [SerializeField] private int minDayToAppear = 3;
    [SerializeField] private EventType eventType = EventType.DemandFocus;

    [Header("Demand Focus Settings")]
    [Tooltip("The customers that will be featured more often during this event.")]
    [SerializeField] private List<CustomerData> featuredCustomers;

    [Range(1, 10)]
    [SerializeField] private int eventWeight = 4;

    [Header("Product Craze Settings")]
    [Tooltip("This product will be added to many customers' orders.")]
    [SerializeField] private ProductData bonusProduct;

    [Range(0, 100)]
    [SerializeField] private int bonusProductChance = 60;

    [Header("Challenge Modifier Settings")]
    [Range(1f, 2f)]
    [SerializeField] private float customerSpeedMultiplier = 1f;

    [Range(1f, 2f)]
    [SerializeField] private float stationPrepTimeMultiplier = 1f;

    [Header("Boon Settings")]
    [Range(1f, 3f)]
    [SerializeField] private float cashRewardMultiplier = 1f;

    public Sprite EventIcon => eventIcon;
    public string EventName => eventName;
    public string EventDescription => eventDescription;
    public int MinDayToAppear => minDayToAppear;
    public EventType Type => eventType;

    public List<CustomerData> FeaturedCustomers => featuredCustomers;
    public int EventWeight => eventWeight;

    public ProductData BonusProduct => bonusProduct;
    public int BonusProductChance => bonusProductChance;

    public float CustomerSpeedMultiplier => customerSpeedMultiplier;
    public float StationPrepTimeMultiplier => stationPrepTimeMultiplier;

    public float CashRewardMultiplier => cashRewardMultiplier;
}