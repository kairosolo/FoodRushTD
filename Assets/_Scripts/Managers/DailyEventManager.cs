using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class DailyEventManager : MonoBehaviour
{
    public static DailyEventManager Instance { get; private set; }

    public static event Action<DailyEventData> OnNewDailyEvent;

    [Header("Event Settings")]
    [SerializeField] private List<DailyEventData> allPossibleEvents;

    [Range(0, 100)]
    [SerializeField] private int chanceForEvent = 50;

    public DailyEventData ActiveEvent { get; private set; }
    private DailyEventData lastDayEvent = null;

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

    private void OnEnable()
    {
        GameClock.OnDayPhaseStart += TryTriggerNewEvent;
    }

    private void OnDisable()
    {
        GameClock.OnDayPhaseStart -= TryTriggerNewEvent;
    }

    private void TryTriggerNewEvent()
    {
        if (ActiveEvent != null)
        {
            lastDayEvent = ActiveEvent;
        }

        ActiveEvent = null;

        if (GameClock.Instance != null && GameClock.Instance.CurrentDay <= 1)
        {
            OnNewDailyEvent?.Invoke(null);
            return;
        }

        int currentDay = GameClock.Instance.CurrentDay;

        List<DailyEventData> validEventsForToday = allPossibleEvents
            .Where(evt => currentDay >= evt.MinDayToAppear && evt != lastDayEvent)
            .ToList();

        if (validEventsForToday.Count == 0)
        {
            validEventsForToday = allPossibleEvents
                .Where(evt => currentDay >= evt.MinDayToAppear)
                .ToList();
            if (validEventsForToday.Count > 1 && lastDayEvent != null)
            {
                validEventsForToday.Remove(lastDayEvent);
            }
        }

        if (validEventsForToday.Count == 0)
        {
            OnNewDailyEvent?.Invoke(null);
            return;
        }

        int roll = UnityEngine.Random.Range(1, 101);
        if (roll <= chanceForEvent)
        {
            ActiveEvent = validEventsForToday[UnityEngine.Random.Range(0, validEventsForToday.Count)];
            Debug.Log($"DAILY EVENT: {ActiveEvent.EventName} has started!");
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX("Event_Announce");
        }
        else
        {
            Debug.Log("No daily event today.");
            lastDayEvent = null;
        }

        OnNewDailyEvent?.Invoke(ActiveEvent);
    }

    public void Debug_TriggerEvent(DailyEventData eventData)
    {
        if (eventData != null)
        {
            ActiveEvent = eventData;
            OnNewDailyEvent?.Invoke(eventData);
        }
    }
}