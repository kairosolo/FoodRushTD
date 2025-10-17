using UnityEngine;

public class DroppedMoney : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float rotationSpeed = 360f;

    private int cashValue;
    private Vector3 targetPosition;
    private bool isInitialized = false;

    public void Initialize(int value, Vector3 target)
    {
        cashValue = value;
        targetPosition = target;
        isInitialized = true;
        Destroy(gameObject, 5f);
    }

    private void Update()
    {
        if (!isInitialized) return;

        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            EconomyManager.Instance.AddCash(cashValue);
            AudioManager.Instance.PlaySFX("Economy_CashGain");
            Destroy(gameObject);
        }
    }
}