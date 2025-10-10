using UnityEngine;
using TMPro;
using System.Collections;

public class DailyEventUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject eventContainer;
    [SerializeField] private TextMeshProUGUI eventNameText;
    [SerializeField] private TextMeshProUGUI eventDescriptionText;

    [Header("Display Settings")]
    [SerializeField] private float displayDuration = 4f;

    private Coroutine displayCoroutine;

    private void OnEnable()
    {
        DailyEventManager.OnNewDailyEvent += ShowEventAnnouncement;
    }

    private void OnDisable()
    {
        DailyEventManager.OnNewDailyEvent -= ShowEventAnnouncement;
    }

    private void ShowEventAnnouncement(DailyEventData eventData)
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }

        if (eventData == null)
        {
            eventContainer.SetActive(false);
            return;
        }

        eventNameText.text = eventData.EventName;
        eventDescriptionText.text = eventData.EventDescription;

        displayCoroutine = StartCoroutine(ShowAndHideRoutine());
    }

    private IEnumerator ShowAndHideRoutine()
    {
        eventContainer.SetActive(true);
        yield return new WaitForSeconds(displayDuration);
        eventContainer.SetActive(false);
    }
}