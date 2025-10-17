using UnityEngine;

public class MoneyDropManager : MonoBehaviour
{
    public static MoneyDropManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject droppedMoneyPrefab;
    [SerializeField] private RectTransform cashUiTarget;
    [SerializeField] private Camera mainCamera;

    private Vector3 targetWorldPosition;

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

    private void Update()
    {
        if (cashUiTarget != null)
        {
            targetWorldPosition = mainCamera.ScreenToWorldPoint(cashUiTarget.position);
            targetWorldPosition.z = 0;
        }
    }

    public void SpawnMoney(Vector3 position, int amount)
    {
        if (droppedMoneyPrefab == null)
        {
            Debug.LogError("Dropped Money Prefab is not assigned in MoneyDropManager!");
            EconomyManager.Instance.AddCash(amount);
            return;
        }

        GameObject moneyObject = Instantiate(droppedMoneyPrefab, position, Quaternion.identity);
        if (moneyObject.TryGetComponent<DroppedMoney>(out DroppedMoney money))
        {
            money.Initialize(amount, targetWorldPosition);
        }
    }
}