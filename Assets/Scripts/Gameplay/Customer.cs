using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class Customer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject orderIconContainer;
    [SerializeField] private GameObject orderIconPrefab;
    [SerializeField] private CharacterRandomizer characterRandomizer;
    [SerializeField] private Animator animator;


    public event Action<float, float> OnPatienceChanged;

    private float moveSpeed;
    private float satisfiedMoveSpeedMultiplier = 2.5f;
    private int cashReward;
    private List<ProductData> order;
    private bool isVip;
    private float patienceDuration;
    private float currentPatience;
    private SpriteRenderer spriteRenderer;
    private int nextWaypointIndex;
    private Vector3 startPosition;
    private Vector3 targetWaypointPosition;
    private float travelTime;
    private float lerpTimer;
    private bool isMoving;
    private bool isOrderComplete = false;
    private bool isEnraged = false;

    private Dictionary<ProductData, GameObject> orderIconMap;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        orderIconMap = new Dictionary<ProductData, GameObject>();
    }

    public void Initialize(CustomerData data)
    {
        characterRandomizer.RandomizeAll();

        this.moveSpeed = data.MoveSpeed;
        this.cashReward = data.CashReward;
        this.order = new List<ProductData>(data.PotentialOrder);
        this.isVip = data.IsVip;
        this.patienceDuration = data.PatienceDuration;

        if (isVip) currentPatience = patienceDuration;

        CustomerManager.Instance.AddCustomer(this);
        transform.position = PathManager.Instance.GetWaypoint(0).position;
        nextWaypointIndex = 1;
        SetNextWaypoint();
        DisplayOrderIcons();
    }

    private void Update()
    {
        if (isMoving)
        {
            lerpTimer += Time.deltaTime;
            float t = Mathf.Clamp01(lerpTimer / travelTime);
            transform.position = Vector3.Lerp(startPosition, targetWaypointPosition, t);
            if (t >= 1f)
            {
                SetNextWaypoint();
            }
        }

        if (isVip && !isOrderComplete && !isEnraged && isMoving)
        {
            currentPatience -= Time.deltaTime;
            OnPatienceChanged?.Invoke(currentPatience, patienceDuration);

            if (currentPatience <= 0)
            {
                HandlePatienceDepleted();
            }
        }
    }

    private void HandlePatienceDepleted()
    {
        isEnraged = true;

        GameUIManager.Instance.HideVipPatienceMeter();

        Debug.Log("VIP has run out of patience and is enraged!");
        // Trigger VIPLostSFX

        order.Clear();
        foreach (var icon in orderIconMap.Values) Destroy(icon);
        orderIconMap.Clear();

        if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.4f, 0.4f, 1f);

        moveSpeed *= satisfiedMoveSpeedMultiplier;

        int lastWaypointIndex = PathManager.Instance.WaypointCount - 1;
        if (lastWaypointIndex >= 0)
        {
            targetWaypointPosition = PathManager.Instance.GetWaypoint(lastWaypointIndex).position;
            nextWaypointIndex = PathManager.Instance.WaypointCount;
            startPosition = transform.position;
            float distanceToExit = Vector3.Distance(startPosition, targetWaypointPosition);
            travelTime = distanceToExit / moveSpeed;
            lerpTimer = 0f;
        }
    }

    public bool DoesOrderContain(ProductData product)
    {
        if (isOrderComplete || isEnraged) return false;

        return order.Contains(product);
    }

    private void DisplayOrderIcons()
    {
        foreach (Transform child in orderIconContainer.transform) Destroy(child.gameObject);
        orderIconMap.Clear();
        if (orderIconPrefab == null || orderIconContainer == null) return;
        foreach (ProductData product in order)
        {
            GameObject iconObject = Instantiate(orderIconPrefab, orderIconContainer.transform);
            if (iconObject.TryGetComponent<Image>(out Image iconImage))
            {
                iconImage.sprite = product.ProductIcon;
                if (!orderIconMap.ContainsKey(product)) orderIconMap.Add(product, iconObject);
            }
        }
    }

    public void ReceiveFoodItem(ProductData receivedProduct)
    {
        if (order.Contains(receivedProduct))
        {
            order.Remove(receivedProduct);
            if (orderIconMap.TryGetValue(receivedProduct, out GameObject iconObject))
            {
                Destroy(iconObject);
                orderIconMap.Remove(receivedProduct);
            }
            if (order.Count == 0) HandleOrderComplete();
        }
    }

    private void HandleOrderComplete()
    {
        isOrderComplete = true;
        animator.SetTrigger("isHappy");

        if (isVip)
        {
            GameUIManager.Instance.HideVipPatienceMeter();
        }

        if (spriteRenderer != null) spriteRenderer.color = new Color(0.5f, 1f, 0.5f, 0.5f);
        moveSpeed *= satisfiedMoveSpeedMultiplier;
        int lastWaypointIndex = PathManager.Instance.WaypointCount - 1;
        if (lastWaypointIndex >= 0)
        {
            targetWaypointPosition = PathManager.Instance.GetWaypoint(lastWaypointIndex).position;
            nextWaypointIndex = PathManager.Instance.WaypointCount;
            startPosition = transform.position;
            float distanceToExit = Vector3.Distance(startPosition, targetWaypointPosition);
            travelTime = distanceToExit / moveSpeed;
            lerpTimer = 0f;
        }
    }

    private void SetNextWaypoint()
    {
        if (nextWaypointIndex >= PathManager.Instance.WaypointCount) { HandleReachingExit(); return; }
        startPosition = transform.position;
        targetWaypointPosition = PathManager.Instance.GetWaypoint(nextWaypointIndex).position;
        float distance = Vector3.Distance(startPosition, targetWaypointPosition);
        if (distance > 0.01f) { travelTime = distance / moveSpeed; lerpTimer = 0f; isMoving = true; } else { travelTime = 0f; isMoving = false; }
        nextWaypointIndex++;
    }

    private void HandleReachingExit()
    {
        isMoving = false;
        if (isOrderComplete) EconomyManager.Instance.AddCash(cashReward);
        else GameLoopManager.Instance.CustomerReachedExitUnsatisfied();
        CustomerManager.Instance.RemoveCustomer(this);
        Destroy(gameObject);
    }
}