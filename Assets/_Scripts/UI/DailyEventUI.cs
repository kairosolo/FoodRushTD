using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DailyEventUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject eventContainer;
    [SerializeField] private Image eventIconHolder;
    [SerializeField] private TextMeshProUGUI eventNameText;
    [SerializeField] private TextMeshProUGUI eventDescriptionText;
    [SerializeField] private Animator eventUIAnimator;

    [Header("Display Settings")]
    [SerializeField] private float displayDuration = 4f;

    private Coroutine displayCoroutine;

    private void Start()
    {
        if (eventContainer != null)
        {
            eventContainer.SetActive(false);
        }
    }

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

        eventIconHolder.sprite = eventData.EventIcon;
        eventNameText.text = eventData.EventName;
        eventDescriptionText.text = eventData.EventDescription;

        displayCoroutine = StartCoroutine(ShowAndHideRoutine());
    }

    private IEnumerator ShowAndHideRoutine()
    {
        eventContainer.SetActive(true);
        eventUIAnimator.SetTrigger("isPoppingIn");

        AudioManager.Instance.PlaySFX("Event_Announce");

        yield return new WaitForSeconds(displayDuration);

        eventUIAnimator.SetTrigger("isPoppingOut");

        yield return new WaitForSeconds(eventUIAnimator.GetCurrentAnimatorStateInfo(0).length);
        eventContainer.SetActive(false);
    }
}