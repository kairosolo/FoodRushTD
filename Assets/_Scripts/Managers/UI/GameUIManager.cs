using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Clock UI")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private GameObject dayPhaseIndicator;
    [SerializeField] private GameObject nightPhaseIndicator;

    [Header("Resource UI")]
    [SerializeField] private TextMeshProUGUI cashText;
    [SerializeField] private TextMeshProUGUI unsatisfiedCustomersText;
    [SerializeField] private float cashLerpDuration = 0.4f;

    [Header("VIP UI")]
    [SerializeField] private GameObject vipPatienceMeterContainer;
    [SerializeField] private Image vipPatienceMeterFill;
    [SerializeField] private TextMeshProUGUI vipNameText;

    private Customer currentVip;
    private int displayedCash;
    private Coroutine cashLerpCoroutine;

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

    private void Start()
    {
        if (vipPatienceMeterContainer != null)
        {
            vipPatienceMeterContainer.SetActive(false);
        }

        if (EconomyManager.Instance != null)
        {
            displayedCash = EconomyManager.Instance.CurrentCash;
            SetCashText(displayedCash);
        }
    }

    private void OnEnable()
    {
        GameClock.OnTimeChanged += HandleTimeChanged;
        GameClock.OnDayChanged += HandleDayChanged;
        GameClock.OnDayPhaseStart += HandleDayPhaseStart;
        GameClock.OnNightPhaseStart += HandleNightPhaseStart;
        EconomyManager.OnCashChanged += HandleCashChange;
        GameLoopManager.OnUnsatisfiedCustomersChanged += UpdateUnsatisfiedCustomersText;
    }

    private void OnDisable()
    {
        GameClock.OnTimeChanged -= HandleTimeChanged;
        GameClock.OnDayChanged -= HandleDayChanged;
        GameClock.OnDayPhaseStart -= HandleDayPhaseStart;
        GameClock.OnNightPhaseStart -= HandleNightPhaseStart;
        EconomyManager.OnCashChanged -= HandleCashChange;
        GameLoopManager.OnUnsatisfiedCustomersChanged -= UpdateUnsatisfiedCustomersText;

        if (currentVip != null)
        {
            currentVip.OnPatienceChanged -= HandleVipPatienceChanged;
        }
    }

    public void ShowVipPatienceMeter(Customer vip)
    {
        if (vipPatienceMeterContainer == null || vip == null) return;

        currentVip = vip;
        currentVip.OnPatienceChanged += HandleVipPatienceChanged;

        if (vipNameText != null)
        {
            vipNameText.text = "Food Critic's Patience";
        }

        vipPatienceMeterContainer.SetActive(true);
    }

    public void HideVipPatienceMeter()
    {
        if (vipPatienceMeterContainer == null) return;

        if (currentVip != null)
        {
            currentVip.OnPatienceChanged -= HandleVipPatienceChanged;
            currentVip = null;
        }

        vipPatienceMeterContainer.SetActive(false);
    }

    private void HandleVipPatienceChanged(float current, float max)
    {
        if (vipPatienceMeterFill != null)
        {
            vipPatienceMeterFill.fillAmount = Mathf.Clamp01(current / max);
        }
    }

    private void SetCashText(int amount)
    {
        if (cashText != null)
        {
            cashText.text = $"<sprite name=\"Multi_Cash\"> {amount}";
        }
    }

    private void HandleCashChange(int newTotalAmount)
    {
        if (gameObject.activeInHierarchy)
        {
            if (cashLerpCoroutine != null)
            {
                StopCoroutine(cashLerpCoroutine);
            }
            cashLerpCoroutine = StartCoroutine(LerpCashRoutine(newTotalAmount));
        }
        else
        {
            displayedCash = newTotalAmount;
            SetCashText(displayedCash);
        }
    }

    private IEnumerator LerpCashRoutine(int targetAmount)
    {
        int startAmount = displayedCash;
        float timer = 0f;

        while (timer < cashLerpDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / cashLerpDuration);

            displayedCash = (int)Mathf.Lerp(startAmount, targetAmount, progress);
            SetCashText(displayedCash);

            yield return null;
        }

        displayedCash = targetAmount;
        SetCashText(displayedCash);
        cashLerpCoroutine = null;
    }

    private void UpdateUnsatisfiedCustomersText(int newAmount)
    {
        if (unsatisfiedCustomersText != null)
        {
            unsatisfiedCustomersText.text = $" <sprite name=\"Sad_Customer\"> {newAmount} / 10";
        }
    }

    private void HandleDayChanged(int day)
    {
        if (dayText != null)
        {
            dayText.text = $"Day {day}";
        }
    }

    private void HandleTimeChanged(int hour, int minute)
    {
        if (timeText != null)
        {
            string amPm = hour < 12 ? "AM" : "PM";
            int displayHour = hour % 12;
            if (displayHour == 0) displayHour = 12;
            timeText.text = $"{displayHour:00}:{minute:00} {amPm}";
        }
    }

    private void HandleDayPhaseStart()
    {
        if (dayPhaseIndicator != null) dayPhaseIndicator.SetActive(true);
        if (nightPhaseIndicator != null) nightPhaseIndicator.SetActive(false);
    }

    private void HandleNightPhaseStart()
    {
        if (dayPhaseIndicator != null) dayPhaseIndicator.SetActive(false);
        if (nightPhaseIndicator != null) nightPhaseIndicator.SetActive(true);
    }
}