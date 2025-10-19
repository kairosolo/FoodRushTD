using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;

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

    [Header("Animation Settings")]
    [SerializeField] private float dialogueBoxAnimDuration = 0.2f;
    [SerializeField] private float dialogueBoxPopScale = 1.1f;
    [SerializeField] private Color dialogueBoxHitColor = new Color(0.6f, 1f, 0.6f, 1f);

    [Header("Debug")]
    [SerializeField] private float baseMoveSpeed;
    [SerializeField] private float moveSpeed;

    public event Action<float, float> OnPatienceChanged;

    private int difficultyScaledItems = 0;
    private float satisfiedMoveSpeedMultiplier = 3f;
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

    private Image dialogueBoxImage;
    private Vector3 dialogueBoxOriginalScale;
    private Color dialogueBoxOriginalColor;
    private Coroutine dialogueBoxAnimationCoroutine;

    private void Awake()
    {
        orderIconMap = new Dictionary<ProductData, OrderIconUI>();
        currentOrder = new Dictionary<ProductData, int>();
        itemsInFlight = new Dictionary<ProductData, int>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (dialogueBox != null)
        {
            dialogueBoxImage = dialogueBox.GetComponent<Image>();
            if (dialogueBoxImage != null)
            {
                dialogueBoxOriginalScale = dialogueBox.transform.localScale;
                dialogueBoxOriginalColor = dialogueBoxImage.color;
            }
        }
    }

    public void Initialize(CustomerData data)
    {
        characterRandomizer.RandomizeAll();

        float difficultySpeedMultiplier = 1f;
        if (DifficultyManager.Instance != null)
        {
            difficultySpeedMultiplier = DifficultyManager.Instance.SpeedMultiplier;
        }

        float eventSpeedMultiplier = 1f;
        if (DailyEventManager.Instance != null && DailyEventManager.Instance.ActiveEvent != null)
        {
            DailyEventData activeEvent = DailyEventManager.Instance.ActiveEvent;
            if (activeEvent.Type == DailyEventData.EventType.ChallengeModifier)
            {
                eventSpeedMultiplier = activeEvent.CustomerSpeedMultiplier;
            }
        }

        this.moveSpeed = data.MoveSpeed * difficultySpeedMultiplier * eventSpeedMultiplier;
        this.cashReward = data.CashReward;
        this.isVip = data.IsVip;
        this.patienceDuration = data.PatienceDuration / difficultySpeedMultiplier;

        GenerateOrder(data);

        if (isVip) currentPatience = patienceDuration;

        if (CustomerManager.Instance != null)
            CustomerManager.Instance.AddCustomer(this);

        if (PathManager.Instance != null && PathManager.Instance.WaypointCount > 0)
        {
            transform.position = PathManager.Instance.GetWaypoint(0).position;
            nextWaypointIndex = 1;
            SetNextWaypoint();
        }

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
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.HideVipPatienceMeter();

        animator.SetTrigger("isAngry");
        Debug.Log("VIP has run of patience and is enraged!");
        if (AudioManager.Instance != null)
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
            if (iconUI != null)
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

        if (PathManager.Instance != null)
        {
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
    }

    private void GenerateOrder(CustomerData customerData)
    {
        currentOrder.Clear();
        difficultyScaledItems = 0;

        foreach (var item in customerData.PotentialOrder)
        {
            if (currentOrder.ContainsKey(item.product))
            {
                currentOrder[item.product] += item.quantity;
            }
            else
            {
                currentOrder.Add(item.product, item.quantity);
            }
        }

        if (DailyEventManager.Instance != null)
        {
            DailyEventData activeEvent = DailyEventManager.Instance.ActiveEvent;
            if (activeEvent != null && activeEvent.Type == DailyEventData.EventType.ProductCraze)
            {
                ProductData bonusProduct = activeEvent.BonusProduct;

                if (bonusProduct != null && customerData.PotentialOrder.Count > 0)
                {
                    int roll = UnityEngine.Random.Range(0, 101);
                    if (roll <= activeEvent.BonusProductChance)
                    {
                        if (currentOrder.ContainsKey(bonusProduct))
                        {
                            currentOrder[bonusProduct]++;
                        }
                        else
                        {
                            currentOrder.Add(bonusProduct, 1);
                        }
                    }
                }
            }
        }

        if (DifficultyManager.Instance != null)
        {
            int extraItems = DifficultyManager.Instance.MaxAdditionalItems;
            if (extraItems > 0 && currentOrder.Count > 0)
            {
                List<ProductData> productsInOrder = currentOrder.Keys.ToList();

                for (int i = 0; i < extraItems; i++)
                {
                    ProductData productToAdd = productsInOrder[UnityEngine.Random.Range(0, productsInOrder.Count)];
                    currentOrder[productToAdd]++;
                    difficultyScaledItems++;
                }
            }
        }
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
            if (dialogueBoxAnimationCoroutine != null)
            {
                StopCoroutine(dialogueBoxAnimationCoroutine);
                dialogueBox.transform.localScale = dialogueBoxOriginalScale;
                if (dialogueBoxImage != null) dialogueBoxImage.color = dialogueBoxOriginalColor;
            }
            dialogueBoxAnimationCoroutine = StartCoroutine(AnimateDialogueBoxHit());

            AudioManager.Instance.PlaySFX("Customer_ReceiveItem");

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

    private IEnumerator AnimateDialogueBoxHit()
    {
        if (dialogueBox == null || dialogueBoxImage == null) yield break;

        float halfDuration = dialogueBoxAnimDuration / 2f;
        float timer = 0f;
        Vector3 targetScale = dialogueBoxOriginalScale * dialogueBoxPopScale;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            dialogueBox.transform.localScale = Vector3.Lerp(dialogueBoxOriginalScale, targetScale, t);
            dialogueBoxImage.color = Color.Lerp(dialogueBoxOriginalColor, dialogueBoxHitColor, t);
            yield return null;
        }

        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            dialogueBox.transform.localScale = Vector3.Lerp(targetScale, dialogueBoxOriginalScale, t);
            dialogueBoxImage.color = Color.Lerp(dialogueBoxHitColor, dialogueBoxOriginalColor, t);
            yield return null;
        }

        dialogueBox.transform.localScale = dialogueBoxOriginalScale;
        dialogueBoxImage.color = dialogueBoxOriginalColor;
        dialogueBoxAnimationCoroutine = null;
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

        int finalCashReward = this.cashReward;

        if (DifficultyManager.Instance != null && difficultyScaledItems > 0)
        {
            finalCashReward += difficultyScaledItems * DifficultyManager.Instance.CashBonusPerAdditionalItem;
        }

        if (DailyEventManager.Instance != null && DailyEventManager.Instance.ActiveEvent != null)
        {
            DailyEventData activeEvent = DailyEventManager.Instance.ActiveEvent;
            if (activeEvent.Type == DailyEventData.EventType.Boon)
            {
                finalCashReward = Mathf.FloorToInt(finalCashReward * activeEvent.CashRewardMultiplier);
            }
        }

        if (MoneyDropManager.Instance != null)
        {
            MoneyDropManager.Instance.SpawnMoney(transform.position, finalCashReward);
        }

        animator.SetTrigger("isHappy");

        if (VFXManager.Instance != null)
            VFXManager.Instance.PlayVFX("customerHappyVFX", transform.position, transform.rotation);

        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }

        if (dialogueTail != null)
        {
            dialogueTail.SetActive(false);
        }

        if (isVip && GameUIManager.Instance != null)
        {
            GameUIManager.Instance.HideVipPatienceMeter();
        }

        if (spriteRenderer != null) spriteRenderer.color = new Color(0.5f, 1f, 0.5f, 0.5f);
        moveSpeed *= satisfiedMoveSpeedMultiplier;

        if (PathManager.Instance != null)
        {
            int lastWaypointIndex = PathManager.Instance.WaypointCount - 1;
            if (lastWaypointIndex >= 0)
            {
                targetWaypointPosition = PathManager.Instance.GetWaypoint(lastWaypointIndex).position;
                nextWaypointIndex = PathManager.Instance.WaypointCount;
                startPosition = transform.position;
                float distanceToExit = Vector3.Distance(startPosition, targetWaypointPosition);
                if (moveSpeed > 0)
                {
                    travelTime = distanceToExit / moveSpeed;
                }
                else
                {
                    travelTime = float.MaxValue;
                }
                lerpTimer = 0f;
            }
        }
    }

    private void SetNextWaypoint()
    {
        if (PathManager.Instance == null)
        {
            Destroy(gameObject);
            return;
        }

        isMoving = false;

        while (nextWaypointIndex < PathManager.Instance.WaypointCount)
        {
            Vector3 potentialTarget = PathManager.Instance.GetWaypoint(nextWaypointIndex).position;
            float distance = Vector3.Distance(transform.position, potentialTarget);

            if (distance > 0.01f)
            {
                startPosition = transform.position;
                targetWaypointPosition = potentialTarget;
                travelTime = distance / moveSpeed;
                lerpTimer = 0f;
                isMoving = true;
                nextWaypointIndex++;
                return;
            }
            nextWaypointIndex++;
        }
        HandleReachingExit();
    }

    private void HandleReachingExit()
    {
        isMoving = false;
        if (!isOrderComplete && GameLoopManager.Instance != null)
        {
            GameLoopManager.Instance.CustomerReachedExitUnsatisfied();
        }

        if (CustomerManager.Instance != null)
            CustomerManager.Instance.RemoveCustomer(this);

        Destroy(gameObject);
    }
}