using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class StationUnlockUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject announcementContainer;
    [SerializeField] private Image stationIconHolder;
    [SerializeField] private TextMeshProUGUI announcementTitleText;
    [SerializeField] private TextMeshProUGUI announcementBodyText;
    [SerializeField] private Animator announcementAnimator;

    [Header("Display Settings")]
    [SerializeField] private float displayDuration = 4f;

    private Queue<StationData> stationAnnouncementQueue = new Queue<StationData>();
    private Coroutine displayCoroutine;

    private void OnEnable()
    {
        ProgressionManager.OnNewStationUnlocked += QueueStationAnnouncement;
    }

    private void OnDisable()
    {
        ProgressionManager.OnNewStationUnlocked -= QueueStationAnnouncement;
    }

    private void Start()
    {
        announcementContainer.SetActive(false);
    }

    private void Update()
    {
        if (displayCoroutine == null && stationAnnouncementQueue.Count > 0)
        {
            StationData stationToAnnounce = stationAnnouncementQueue.Dequeue();
            displayCoroutine = StartCoroutine(ShowAndHideRoutine(stationToAnnounce));
        }
    }

    private void QueueStationAnnouncement(StationData stationData)
    {
        if (stationData != null)
        {
            stationAnnouncementQueue.Enqueue(stationData);
        }
    }

    private IEnumerator ShowAndHideRoutine(StationData stationData)
    {
        announcementContainer.SetActive(true);
        stationIconHolder.sprite = stationData.StationIcon;
        announcementTitleText.text = $"New Station: {stationData.StationName}!";
        string productList = string.Join(", ", stationData.AvailableProducts.Select(p => p.ProductName));
        announcementBodyText.text = $"Unlocks: {productList}";

        announcementAnimator.SetTrigger("isPoppingIn");

        AudioManager.Instance.PlaySFX("Event_Announce");

        yield return new WaitForSeconds(displayDuration);

        announcementAnimator.SetTrigger("isPoppingOut");

        float popOutAnimationLength = 0.5f;
        AnimationClip[] clips = announcementAnimator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip.name == "Announcement_PopOut")
            {
                popOutAnimationLength = clip.length;
                break;
            }
        }
        yield return new WaitForSeconds(popOutAnimationLength + 0.1f);

        announcementContainer.SetActive(false);

        displayCoroutine = null;
    }
}