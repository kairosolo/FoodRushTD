using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class Station : MonoBehaviour
{
    private enum SlotState
    { Idle, Preparing, Holding }

    [Header("Station Settings")]
    [SerializeField] private float serviceRange = 5f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float clickRadius = 1f;
    [SerializeField] private float switchCommitmentTime = 1.0f;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionDelay = 0.5f;

    [Header("Primary UI References")]
    [SerializeField] private Image productIconImage;
    [SerializeField] private Image progressBarImage;
    [SerializeField] private TextMeshProUGUI primaryStackText;

    [Header("Secondary UI References (For Mastery)")]
    [SerializeField] private GameObject secondaryProductUIGroup;
    [SerializeField] private Image secondaryProductIconImage;
    [SerializeField] private Image secondaryProgressBarImage;
    [SerializeField] private TextMeshProUGUI secondaryStackText;

    [Header("Core References")]
    [SerializeField] private Canvas stationCanvas;
    [SerializeField] private RangeVisualizer rangeVisualizer;
    [SerializeField] private GameObject placementInfo;
    [SerializeField] private GameObject primaryProductVisual;
    [SerializeField] private GameObject secondaryProductVisual;
    [SerializeField] private Animator animator;

    public int TotalValue { get; set; }
    public StationData StationData { get; private set; }
    public ProductData CurrentProduct => currentProduct;
    public List<ProductData> UnlockedProducts { get; private set; }
    public int DiversifyLevel { get; private set; }
    public int SpecializationLevel { get; private set; }
    public bool IsMastered => DiversifyLevel >= 2;
    public float ClickRadius => clickRadius;

    public int MaxStackSize => (DiversifyLevel > 2) ? (DiversifyLevel - 1) : 1;

    private ProductData currentProduct;
    private ProductData secondaryProduct;

    private Dictionary<ProductData, float> prepTimers;
    private Dictionary<ProductData, int> heldStacks;

    private SlotState primarySlotState = SlotState.Idle;
    private SlotState secondarySlotState = SlotState.Idle;

    private float primaryCommitTimer;
    private float secondaryCommitTimer;

    private CharacterRandomizer characterRandomizer;
    private bool canInteractFlag;
    private float interactionTimer;
    private float serveCooldownTimer;

    private void Awake()
    {
        characterRandomizer = GetComponent<CharacterRandomizer>();
        UnlockedProducts = new List<ProductData>();
        prepTimers = new Dictionary<ProductData, float>();
        heldStacks = new Dictionary<ProductData, int>();
        DiversifyLevel = 0;
    }

    private void Update()
    {
        if (!canInteractFlag)
        {
            interactionTimer -= Time.deltaTime;
            if (interactionTimer <= 0f) canInteractFlag = true;
        }

        if (serveCooldownTimer > 0f)
        {
            serveCooldownTimer -= Time.deltaTime;
        }

        if (IsMastered && serveCooldownTimer <= 0f)
        {
            Customer target = FindBestTargetStationCanServe();
            if (target != null)
            {
                if (primarySlotState == SlotState.Holding && target.DoesOrderContain(currentProduct))
                {
                    ServeFood(target, currentProduct);
                }
                else if (secondarySlotState == SlotState.Holding && target.DoesOrderContain(secondaryProduct))
                {
                    ServeFood(target, secondaryProduct);
                }
            }
        }

        if (IsMastered)
        {
            UpdateSingleSlot(ref primarySlotState, ref currentProduct, secondaryProduct, ref primaryCommitTimer);
            UpdateSingleSlot(ref secondarySlotState, ref secondaryProduct, currentProduct, ref secondaryCommitTimer);
        }
        else
        {
            UpdateStandardStation();
        }

        UpdateAnimationState();
    }

    private void UpdateAnimationState()
    {
        if (animator == null) return;

        bool isPrimaryPreparing = primarySlotState == SlotState.Preparing;
        bool isSecondaryPreparing = IsMastered && secondarySlotState == SlotState.Preparing;

        animator.SetBool("isCooking", isPrimaryPreparing || isSecondaryPreparing);
    }

    private void LateUpdate()
    {
        UpdateAllVisuals();
    }

    private void UpdateStandardStation()
    {
        UpdateSingleSlot(ref primarySlotState, ref currentProduct, null, ref primaryCommitTimer);
    }

    private void UpdateSingleSlot(ref SlotState state, ref ProductData productInSlot, ProductData otherProduct, ref float commitTimer)
    {
        if (serveCooldownTimer <= 0f && productInSlot != null && heldStacks.ContainsKey(productInSlot) && heldStacks[productInSlot] > 0)
        {
            Customer target = FindBestTargetForProduct(productInSlot);
            if (target != null)
            {
                ServeFood(target, productInSlot);
                if (heldStacks[productInSlot] < MaxStackSize)
                {
                    state = SlotState.Preparing;
                }
                return;
            }
        }

        switch (state)
        {
            case SlotState.Idle:
                productInSlot = FindBestProductToPrepare(otherProduct) ?? GetNextProductInCycle(otherProduct);
                if (productInSlot != null)
                {
                    state = SlotState.Preparing;
                    commitTimer = switchCommitmentTime;
                }
                break;

            case SlotState.Preparing:
                if (productInSlot == null) { state = SlotState.Idle; break; }

                if (heldStacks[productInSlot] >= MaxStackSize)
                {
                    state = SlotState.Holding;
                    break;
                }

                commitTimer -= Time.deltaTime;
                if (commitTimer <= 0f)
                {
                    ProductData bestProduct = FindBestProductToPrepare(otherProduct);
                    if (bestProduct != null && bestProduct != productInSlot && heldStacks[productInSlot] == 0)
                    {
                        TransferProgress(ref productInSlot, bestProduct, false);
                        commitTimer = switchCommitmentTime;
                    }
                }

                prepTimers[productInSlot] += Time.deltaTime;
                float prepTime = GetPreparationTime(productInSlot, SpecializationLevel);

                if (prepTimers[productInSlot] >= prepTime)
                {
                    while (prepTimers[productInSlot] >= prepTime && heldStacks[productInSlot] < MaxStackSize)
                    {
                        heldStacks[productInSlot]++;
                        prepTimers[productInSlot] -= prepTime;
                        AudioManager.Instance.PlaySFX("Station_FoodReady");
                    }
                }
                break;

            case SlotState.Holding:
                ProductData bestProductToSwitchTo = FindBestProductToPrepare(otherProduct);
                if (bestProductToSwitchTo != null && bestProductToSwitchTo != productInSlot)
                {
                    heldStacks[productInSlot]--;
                    TransferProgress(ref productInSlot, bestProductToSwitchTo, true);
                    state = SlotState.Preparing;
                    commitTimer = switchCommitmentTime;
                    break;
                }

                if (productInSlot != null && heldStacks[productInSlot] < MaxStackSize)
                {
                    state = SlotState.Preparing;
                }
                break;
        }
    }

    private Customer FindBestTargetStationCanServe()
    {
        if (CustomerManager.Instance == null) return null;

        ProductData heldProduct1 = (primarySlotState == SlotState.Holding || heldStacks[currentProduct] > 0) ? currentProduct : null;
        ProductData heldProduct2 = (secondarySlotState == SlotState.Holding || (secondaryProduct != null && heldStacks[secondaryProduct] > 0)) ? secondaryProduct : null;

        if (heldProduct1 == null && heldProduct2 == null) return null;

        return CustomerManager.Instance.GetActiveCustomers()
            .Select(c => new { Customer = c, Distance = Vector3.Distance(transform.position, c.transform.position) })
            .Where(x => x.Distance <= serviceRange)
            .Where(x => (heldProduct1 != null && x.Customer.DoesOrderContain(heldProduct1)) ||
                        (heldProduct2 != null && x.Customer.DoesOrderContain(heldProduct2)))
            .OrderBy(x => x.Distance)
            .FirstOrDefault()
            ?.Customer;
    }

    private void TransferProgress(ref ProductData productInSlot, ProductData newProduct, bool isFullTransfer = false)
    {
        ProductData oldProduct = productInSlot;
        float progressToTransfer = isFullTransfer ? GetPreparationTime(oldProduct, SpecializationLevel) : prepTimers[oldProduct];

        productInSlot = newProduct;

        if (!prepTimers.ContainsKey(productInSlot)) prepTimers[productInSlot] = 0f;

        float newTotalProgress = prepTimers[productInSlot] + progressToTransfer;

        float newProductMaxTime = GetPreparationTime(productInSlot, SpecializationLevel);
        prepTimers[productInSlot] = Mathf.Min(newTotalProgress, newProductMaxTime * 0.99f);

        if (prepTimers.ContainsKey(oldProduct))
        {
            prepTimers[oldProduct] = 0f;
        }
    }

    private void UpdateAllVisuals()
    {
        if (productIconImage != null)
        {
            bool hasProduct = primarySlotState != SlotState.Idle && currentProduct != null;
            productIconImage.enabled = hasProduct;
            if (hasProduct) productIconImage.sprite = currentProduct.ProductIconUI;
        }
        if (progressBarImage != null)
        {
            float progress = 0f;
            if (primarySlotState == SlotState.Preparing && currentProduct != null)
            {
                progress = prepTimers[currentProduct] / GetPreparationTime(currentProduct, SpecializationLevel);
            }
            else if (primarySlotState == SlotState.Holding)
            {
                progress = 1f;
            }
            progressBarImage.fillAmount = Mathf.Clamp01(progress);
        }
        if (primaryStackText != null)
        {
            bool showStack = currentProduct != null && heldStacks.ContainsKey(currentProduct) && heldStacks[currentProduct] > 1;
            primaryStackText.gameObject.SetActive(showStack);
            if (showStack) primaryStackText.text = $"x{heldStacks[currentProduct]}";
        }

        if (IsMastered && secondaryProductUIGroup != null)
        {
            if (secondaryProductIconImage != null)
            {
                bool hasProduct = secondarySlotState != SlotState.Idle && secondaryProduct != null;
                secondaryProductIconImage.enabled = hasProduct;
                if (hasProduct) secondaryProductIconImage.sprite = secondaryProduct.ProductIconUI;
            }
            if (secondaryProgressBarImage != null)
            {
                float progress = 0f;
                if (secondarySlotState == SlotState.Preparing && secondaryProduct != null)
                    progress = prepTimers[secondaryProduct] / GetPreparationTime(secondaryProduct, SpecializationLevel);
                else if (secondarySlotState == SlotState.Holding)
                    progress = 1f;
                secondaryProgressBarImage.fillAmount = progress;
            }
            if (secondaryStackText != null)
            {
                bool showStack = secondaryProduct != null && heldStacks.ContainsKey(secondaryProduct) && heldStacks[secondaryProduct] > 1;
                secondaryStackText.gameObject.SetActive(showStack);
                if (showStack) secondaryStackText.text = $"x{heldStacks[secondaryProduct]}";
            }
        }

        UpdateWorldSpaceVisuals();
    }

    private void UpdateWorldSpaceVisuals()
    {
        bool primaryIsCooking = primarySlotState == SlotState.Preparing && currentProduct != null;
        bool primaryIsHolding = primarySlotState == SlotState.Holding && currentProduct != null;

        bool secondaryIsCooking = IsMastered && secondarySlotState == SlotState.Preparing && secondaryProduct != null;
        bool secondaryIsHolding = IsMastered && secondarySlotState == SlotState.Holding && secondaryProduct != null;

        bool isFullyIdle = IsMastered ? (primaryIsHolding && secondaryIsHolding) : primaryIsHolding;

        if (primaryProductVisual != null)
        {
            bool showPrimaryVisual = primaryIsCooking || (primaryIsHolding && isFullyIdle);
            primaryProductVisual.SetActive(showPrimaryVisual);

            if (showPrimaryVisual)
            {
                SpriteRenderer primaryRenderer = primaryProductVisual.GetComponent<SpriteRenderer>();
                if (primaryRenderer != null && currentProduct != null)
                {
                    primaryRenderer.sprite = currentProduct.ProductSprite;
                }
            }
        }

        if (secondaryProductVisual != null)
        {
            bool showSecondaryVisual = IsMastered && (secondaryIsCooking || (secondaryIsHolding && isFullyIdle));
            secondaryProductVisual.SetActive(showSecondaryVisual);

            if (showSecondaryVisual)
            {
                SpriteRenderer secondaryRenderer = secondaryProductVisual.GetComponent<SpriteRenderer>();
                if (secondaryRenderer != null && secondaryProduct != null)
                {
                    secondaryRenderer.sprite = secondaryProduct.ProductSprite;
                }
            }
        }
    }

    #region Core Logic Methods

    private ProductData FindBestProductToPrepare(ProductData otherSlotProduct)
    {
        if (CustomerManager.Instance == null) return null;

        return CustomerManager.Instance.GetActiveCustomers()
            .Select(c => new { Customer = c, Distance = Vector3.Distance(transform.position, c.transform.position) })
            .Where(x => x.Distance <= serviceRange)
            .OrderBy(x => x.Distance)
            .SelectMany(x => UnlockedProducts.Select(p => new { Product = p, Customer = x.Customer }))
            .FirstOrDefault(x => x.Product != otherSlotProduct && x.Customer.DoesOrderContain(x.Product))
            ?.Product;
    }

    private ProductData GetNextProductInCycle(ProductData productToExclude)
    {
        if (UnlockedProducts.Count == 0) return null;
        if (UnlockedProducts.Count == 1) return UnlockedProducts[0] == productToExclude ? null : UnlockedProducts[0];

        int currentIndex = (currentProduct != null) ? UnlockedProducts.IndexOf(currentProduct) : -1;

        for (int i = 1; i <= UnlockedProducts.Count; i++)
        {
            int nextIndex = (currentIndex + i) % UnlockedProducts.Count;
            ProductData nextProduct = UnlockedProducts[nextIndex];
            if (nextProduct != productToExclude)
            {
                return nextProduct;
            }
        }
        return null;
    }

    private Customer FindBestTargetForProduct(ProductData product)
    {
        if (product == null || CustomerManager.Instance == null) return null;

        return CustomerManager.Instance.GetActiveCustomers()
            .Where(c => c.DoesOrderContain(product))
            .Select(c => new { Customer = c, Distance = Vector3.Distance(transform.position, c.transform.position) })
            .Where(x => x.Distance <= serviceRange)
            .OrderBy(x => x.Distance)
            .FirstOrDefault()
            ?.Customer;
    }

    private void ServeFood(Customer target, ProductData product)
    {
        serveCooldownTimer = 0.25f;

        target.NotifyItemInFlight(product);
        if (animator != null) animator.SetTrigger("isShooting");
        AudioManager.Instance.PlaySFX("Projectile_Launch");

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        GameObject projectileObj = ProjectilePoolManager.Instance.GetProjectile(product.FoodProjectilePrefab);
        projectileObj.transform.position = spawnPosition;
        if (projectileObj.TryGetComponent<FoodProjectile>(out var p)) p.Initialize(target, product);

        heldStacks[product]--;
    }

    #endregion Core Logic Methods

    #region Upgrade and Initialization Methods

    public void AddUnlockedProduct(ProductData product)
    {
        if (product == null || UnlockedProducts.Contains(product)) return;

        UnlockedProducts.Add(product);
        if (!prepTimers.ContainsKey(product))
        {
            prepTimers.Add(product, 0f);
        }
        if (!heldStacks.ContainsKey(product))
        {
            heldStacks.Add(product, 0);
        }
    }

    public void UpgradeDiversifyPath()
    {
        if (DiversifyLevel >= 5 || !canInteractFlag) return;

        int cost = GetNextDiversifyCost();
        if (EconomyManager.Instance.SpendCash(cost))
        {
            TotalValue += cost;
            DiversifyLevel++;

            if (DiversifyLevel == 1)
            {
                ProductData productToUnlock = StationData.AvailableProducts.FirstOrDefault(p => !UnlockedProducts.Contains(p));
                if (productToUnlock != null)
                {
                    AddUnlockedProduct(productToUnlock);
                }
            }
            else if (DiversifyLevel == 2)
            {
                if (secondaryProductUIGroup != null) secondaryProductUIGroup.SetActive(true);
            }

            if (primarySlotState == SlotState.Holding)
            {
                primarySlotState = SlotState.Preparing;
            }
            if (IsMastered && secondarySlotState == SlotState.Holding)
            {
                secondarySlotState = SlotState.Preparing;
            }

            AudioManager.Instance.PlaySFX("Station_Upgrade");
        }
    }

    public int GetNextDiversifyCost()
    {
        switch (DiversifyLevel)
        {
            case 0: return StationData.DiversifyCost;
            case 1: return StationData.MasterRecipeCost;
            case 2:
            case 3:
            case 4:
                return StationData.CapacityUpgradeCost * (DiversifyLevel - 1);

            default:
                return 0;
        }
    }

    public void SetInitialProductAndActivate(ProductData initialProduct)
    {
        this.enabled = true;
        AddUnlockedProduct(initialProduct);
        stationCanvas.gameObject.SetActive(true);
        currentProduct = initialProduct;
        primarySlotState = SlotState.Preparing;
        primaryCommitTimer = switchCommitmentTime;

        canInteractFlag = false;
        interactionTimer = interactionDelay;
        if (secondaryProductUIGroup != null) secondaryProductUIGroup.SetActive(false);
    }

    public float GetPreparationTime(int specializationLevelOverride)
    {
        ProductData productForCalc = currentProduct ?? UnlockedProducts.FirstOrDefault();
        return GetPreparationTime(productForCalc, specializationLevelOverride);
    }

    public float GetPreparationTime(ProductData product, int specializationLevelOverride)
    {
        if (product == null) return float.MaxValue;
        float speedMultiplier = 1f + (specializationLevelOverride * StationData.SpecializeSpeedBonus);
        float baseTime = product.BasePreparationTime / speedMultiplier;

        if (DailyEventManager.Instance != null)
        {
            DailyEventData activeEvent = DailyEventManager.Instance.ActiveEvent;
            if (activeEvent != null && activeEvent.Type == DailyEventData.EventType.ChallengeModifier)
            {
                return baseTime * activeEvent.StationPrepTimeMultiplier;
            }
        }
        return baseTime;
    }

    public void UpgradeSpecialization()
    {
        if (!CanUpgradeSpecialization() || !canInteractFlag) return;
        int cost = GetSpecializeCost();
        if (EconomyManager.Instance.SpendCash(cost))
        {
            TotalValue += cost;
            SpecializationLevel++;
            AudioManager.Instance.PlaySFX("Station_Upgrade");
        }
    }

    #endregion Upgrade and Initialization Methods

    #region Boilerplate Methods

    public void FinalizePlacement()
    {
        if (StationManager.Instance != null) StationManager.Instance.AddStation(this);
    }

    private void OnDisable()
    {
        if (StationManager.Instance != null) StationManager.Instance.RemoveStation(this);
    }

    public bool CanInteract()
    {
        return canInteractFlag;
    }

    public void ShowRange()
    {
        if (rangeVisualizer != null) rangeVisualizer.Show(serviceRange);
    }

    public void HideRange()
    {
        if (rangeVisualizer != null) rangeVisualizer.Hide();
    }

    public void PartialInitialize(StationData data)
    {
        StationData = data;
        TotalValue = StationData.PlacementCost;
        this.enabled = false;
        stationCanvas.gameObject.SetActive(false);
    }

    public void TriggerPlacementEffects()
    {
        if (characterRandomizer != null) characterRandomizer.RandomizeAll();
        if (VFXManager.Instance != null) VFXManager.Instance.PlayVFX("stationPlacementVFX", transform.position, transform.rotation);
        placementInfo.SetActive(false);
        if (animator != null) animator.SetTrigger("isPrepared");
    }

    public int GetSpecializeCost()
    {
        return StationData.SpecializeBaseCost * (SpecializationLevel + 1);
    }

    public bool CanUpgradeSpecialization()
    {
        return SpecializationLevel < StationData.MaxSpecializeLevel;
    }

    public bool CanUpgradeDiversify()
    {
        return UnlockedProducts.Count < StationData.AvailableProducts.Count;
    }

    #endregion Boilerplate Methods
}