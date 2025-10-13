using UnityEngine;
using System.Collections.Generic;
using System;

public class DailyEventManager : MonoBehaviour
{
    public static DailyEventManager Instance { get; private set; }

    public static event Action<DailyEventData> OnNewDailyEvent;

    [Header("Event Settings")]
    [SerializeField] private List<DailyEventData> possibleEvents;

    [Range(0, 100)]
    [SerializeField] private int chanceForEvent = 50;
    [SerializeField] private int firstDayForEvents = 3;

    public DailyEventData ActiveEvent { get; private set; }

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
        ActiveEvent = null;

        if (GameClock.Instance.CurrentDay < firstDayForEvents)
        {
            OnNewDailyEvent?.Invoke(null);
            return;
        }

        int roll = UnityEngine.Random.Range(1, 101);
        if (roll <= chanceForEvent)
        {
            if (possibleEvents.Count > 0)
            {
                ActiveEvent = possibleEvents[UnityEngine.Random.Range(0, possibleEvents.Count)];
                Debug.Log($"DAILY EVENT: {ActiveEvent.EventName} has started!");
                AudioManager.Instance.PlaySFX("Event_Announce");
            }
        }
        else
        {
            Debug.Log("No daily event today.");
        }

        OnNewDailyEvent?.Invoke(ActiveEvent);
    }
}