using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    // --- NEW ---
    public static GameUIManager Instance { get; private set; }

    [Header("Clock UI")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private GameObject dayPhaseIndicator;
    [SerializeField] private GameObject nightPhaseIndicator;

    [Header("Resource UI")]
    [SerializeField] private TextMeshProUGUI cashText;
    [SerializeField] private TextMeshProUGUI unsatisfiedCustomersText;

    [Header("VIP UI")]
    [SerializeField] private GameObject vipPatienceMeterContainer;
    [SerializeField] private Image vipPatienceMeterFill;
    [SerializeField] private TextMeshProUGUI vipNameText;

    private Customer currentVip;

    // --- NEW ---
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

    private void OnEnable()
    {
        GameClock.OnTimeChanged += HandleTimeChanged;
        GameClock.OnDayChanged += HandleDayChanged;
        GameClock.OnDayPhaseStart += HandleDayPhaseStart;
        GameClock.OnNightPhaseStart += HandleNightPhaseStart;
        EconomyManager.OnCashChanged += UpdateCashText;
        GameLoopManager.OnUnsatisfiedCustomersChanged += UpdateUnsatisfiedCustomersText;
    }

    private void OnDisable()
    {
        GameClock.OnTimeChanged -= HandleTimeChanged;
        GameClock.OnDayChanged -= HandleDayChanged;
        GameClock.OnDayPhaseStart -= HandleDayPhaseStart;
        GameClock.OnNightPhaseStart -= HandleNightPhaseStart;
        EconomyManager.OnCashChanged -= UpdateCashText;
        GameLoopManager.OnUnsatisfiedCustomersChanged -= UpdateUnsatisfiedCustomersText;
        if (currentVip != null)
        {
            currentVip.OnPatienceChanged -= HandleVipPatienceChanged;
        }
    }

    private void UpdateCashText(int newAmount)
    {
        if (cashText != null)
        {
            cashText.text = $"${newAmount}";
        }
    }

    private void UpdateUnsatisfiedCustomersText(int newAmount)
    {
        if (unsatisfiedCustomersText != null)
        {
            unsatisfiedCustomersText.text = $"Unsatisfied: {newAmount} / 10";
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