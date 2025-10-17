using UnityEngine;

public class VIPManager : MonoBehaviour
{
    public static VIPManager Instance { get; private set; }

    [Header("VIP Settings")]
    [SerializeField] private CustomerData vipCustomerData;
    [SerializeField] private int vipSpawnIntervalInDays = 5;

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
        GameClock.OnDayPhaseStart += CheckAndSpawnVip;
    }

    private void OnDisable()
    {
        GameClock.OnDayPhaseStart -= CheckAndSpawnVip;
    }

    private void CheckAndSpawnVip()
    {
        int currentDay = GameClock.Instance.CurrentDay;
        if (currentDay > 0 && currentDay % vipSpawnIntervalInDays == 0)
        {
            SpawnVip();
        }
    }

    private void SpawnVip()
    {
        if (vipCustomerData == null)
        {
            Debug.LogError("VIP Customer Data is not assigned in VIPManager!");
            return;
        }

        Debug.Log("The Food Critic has arrived!");
        GameObject vipObject = Instantiate(vipCustomerData.CustomerPrefab);

        if (vipObject.TryGetComponent<Customer>(out Customer customer))
        {
            customer.Initialize(vipCustomerData);
            GameUIManager.Instance.ShowVipPatienceMeter(customer);
            AudioManager.Instance.PlaySFX("VIP_Arrive");
        }
    }

    public void Debug_SpawnVip()
    {
        SpawnVip();
    }
}