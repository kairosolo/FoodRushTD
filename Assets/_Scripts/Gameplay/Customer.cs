using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class Customer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject orderIconContainer;
    [SerializeField] private GameObject orderIconPrefab;
    [SerializeField] private CharacterRandomizer characterRandomizer;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private GameObject dialogueTail;


    [Header("Emotion References")]
    [SerializeField] private GameObject EmotionBox;
    [SerializeField] private Image EmotionHolder;
    [SerializeField] private Sprite HappyIcon, AngryIcon;
    public event Action<float, float> OnPatienceChanged;

    private float moveSpeed;
    private float satisfiedMoveSpeedMultiplier = 2.5f;
    private int cashReward;
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

    private Dictionary<ProductData, int> currentOrder;
    private Dictionary<ProductData, int> itemsInFlight;
    private Dictionary<ProductData, OrderIconUI> orderIconMap;

    private void Awake()
    {
        orderIconMap = new Dictionary<ProductData, OrderIconUI>();
        currentOrder = new Dictionary<ProductData, int>();
        itemsInFlight = new Dictionary<ProductData, int>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Initialize(CustomerData data)
    {
        characterRandomizer.RandomizeAll();

        this.moveSpeed = data.MoveSpeed * DifficultyManager.Instance.SpeedMultiplier;
        this.cashReward = data.CashReward;
        this.isVip = data.IsVip;
        this.patienceDuration = data.PatienceDuration / DifficultyManager.Instance.SpeedMultiplier;

        GenerateOrder(data.PotentialOrder);

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

        animator.SetTrigger("isAngry");
        Debug.Log("VIP has run out of patience and is enraged!");
        AudioManager.Instance.PlaySFX("VIP_PatienceFail");

        if (EmotionBox != null)
        {
            EmotionBox.SetActive(true);
        }

        if (EmotionHolder != null)
        {
            EmotionHolder.sprite = AngryIcon;
        }

        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }

        if (dialogueTail != null)
        {
            dialogueTail.SetActive(false);
        }

        foreach (var iconUI in orderIconMap.Values)
        {
            if (iconUI != null) // Safety check
            {
                Destroy(iconUI.gameObject);
            }
        }

        if (orderIconContainer != null)
        {
            orderIconContainer.transform.parent.gameObject.SetActive(false);
        }

        orderIconMap.Clear();
        currentOrder.Clear();
        itemsInFlight.Clear();

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

    private void GenerateOrder(List<OrderItem> baseOrder)
    {
        currentOrder.Clear();
        foreach (var item in baseOrder)
        {
            currentOrder.Add(item.product, item.quantity);
        }

        int extraItems = DifficultyManager.Instance.MaxAdditionalItems;
        if (extraItems > 0 && baseOrder.Count > 0)
        {
            for (int i = 0; i < extraItems; i++)
            {
                ProductData productToAdd = baseOrder[UnityEngine.Random.Range(0, baseOrder.Count)].product;
                currentOrder[productToAdd]++;
            }
        }

        DisplayOrderIcons();
    }

    private void DisplayOrderIcons()
    {
        foreach (Transform child in orderIconContainer.transform) Destroy(child.gameObject);
        orderIconMap.Clear();
        if (orderIconPrefab == null || orderIconContainer == null) return;
        foreach (var product in currentOrder.Keys)
        {
            GameObject iconObject = Instantiate(orderIconPrefab, orderIconContainer.transform);
            if (iconObject.TryGetComponent<OrderIconUI>(out var iconScript))
            {
                iconScript.Initialize(product.ProductIconUI, currentOrder[product]);
                orderIconMap.Add(product, iconScript);
            }
        }
    }

    public void ReceiveFoodItem(ProductData receivedProduct)
    {
        if (itemsInFlight.ContainsKey(receivedProduct)) itemsInFlight[receivedProduct]--;

        if (currentOrder.ContainsKey(receivedProduct))
        {
            currentOrder[receivedProduct]--;
            orderIconMap[receivedProduct].UpdateQuantity(currentOrder[receivedProduct]);

            if (currentOrder[receivedProduct] <= 0)
            {
                Destroy(orderIconMap[receivedProduct].gameObject);
                orderIconMap.Remove(receivedProduct);
                currentOrder.Remove(receivedProduct);
            }

            if (currentOrder.Count == 0) HandleOrderComplete();
        }
    }

    public bool DoesOrderContain(ProductData product)
    {
        if (isOrderComplete || isEnraged) return false;

        int needed = currentOrder.ContainsKey(product) ? currentOrder[product] : 0;
        int inFlight = itemsInFlight.ContainsKey(product) ? itemsInFlight[product] : 0;

        return needed > inFlight;
    }

    public void NotifyItemInFlight(ProductData product)
    {
        if (itemsInFlight.ContainsKey(product)) itemsInFlight[product]++;
        else itemsInFlight.Add(product, 1);
    }

    private void HandleOrderComplete()
    {
        if (EmotionBox != null)
        {
            EmotionBox.SetActive(true);
        }

        if (EmotionHolder != null)
        {
            EmotionHolder.sprite = HappyIcon;
        }

        isOrderComplete = true;
        animator.SetTrigger("isHappy");
        AudioManager.Instance.PlaySFX("Customer_OrderComplete");
        VFXManager.Instance.PlayVFX("customerHappyVFX", transform.position, transform.rotation);

        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }

        if (dialogueTail != null)
        {
            dialogueTail.SetActive(false);
        }

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