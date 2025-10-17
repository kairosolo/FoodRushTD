using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Station : MonoBehaviour
{
    [Header("Station Settings")]
    [SerializeField] private float serviceRange = 5f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float clickRadius = 1f;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionDelay = 0.5f;

    [Header("References")]
    [SerializeField] private Canvas stationCanvas;
    [SerializeField] private RangeVisualizer rangeVisualizer;
    [SerializeField] private GameObject placementInfo;
    [SerializeField] private SpriteRenderer productVisualizer;
    [SerializeField] private Animator animator;
    [SerializeField] private Image productIconImage;
    [SerializeField] private Image progressBarImage;

    public int TotalValue { get; set; }

    public StationData StationData { get; private set; }
    public ProductData CurrentProduct { get; private set; }
    public List<ProductData> UnlockedProducts { get; private set; }
    public int SpecializationLevel { get; private set; }
    public float ClickRadius => clickRadius;

    private CharacterRandomizer characterRandomizer;
    private float preparationTimer;
    private bool isPreparing;
    private bool isHoldingProduct;
    private bool canInteractFlag;
    private float interactionTimer;

    private void Awake()
    {
        characterRandomizer = GetComponent<CharacterRandomizer>();
    }

    private void OnDisable()
    {
        if (StationManager.Instance != null)
        {
            StationManager.Instance.RemoveStation(this);
        }
    }

    public void FinalizePlacement()
    {
        if (StationManager.Instance != null)
        {
            StationManager.Instance.AddStation(this);
        }
    }

    private void Update()
    {
        if (!canInteractFlag)
        {
            interactionTimer -= Time.deltaTime;
            if (interactionTimer <= 0f)
            {
                canInteractFlag = true;
            }
        }

        if (isPreparing)
        {
            preparationTimer += Time.deltaTime;
            UpdateProgressBar();
            float currentPrepTime = GetCurrentPreparationTime();
            if (preparationTimer >= currentPrepTime)
            {
                CompletePreparation();
            }
        }
        else if (isHoldingProduct)
        {
            FindAndServeCustomer();
        }
    }

    public bool CanInteract()
    {
        if (rangeVisualizer != null) rangeVisualizer.Hide();
        return canInteractFlag;
    }

    public void ShowRange()
    {
        if (rangeVisualizer != null)
        {
            rangeVisualizer.Show(serviceRange);
        }
    }

    public void HideRange()
    {
        if (rangeVisualizer != null)
        {
            rangeVisualizer.Hide();
        }
    }

    public void PartialInitialize(StationData data)
    {
        StationData = data;
        SpecializationLevel = 0;
        UnlockedProducts = new List<ProductData>();

        this.enabled = false;
        TotalValue = StationData.PlacementCost;

        if (stationCanvas != null)
        {
            stationCanvas.gameObject.SetActive(false);
        }
    }

    public void TriggerPlacementEffects()
    {
        if (characterRandomizer != null)
        {
            characterRandomizer.RandomizeAll();
        }

        if (VFXManager.Instance != null)
            VFXManager.Instance.PlayVFX("stationPlacementVFX", transform.position, transform.rotation);

        placementInfo.SetActive(false);

        if (animator != null)
        {
            animator.SetTrigger("isPrepared");
        }
    }

    public void SetInitialProductAndActivate(ProductData initialProduct)
    {
        this.enabled = true;

        if (!UnlockedProducts.Contains(initialProduct))
            UnlockedProducts.Add(initialProduct);

        if (stationCanvas != null)
        {
            stationCanvas.gameObject.SetActive(true);
        }

        SetInitialProduct(initialProduct);

        canInteractFlag = false;
        interactionTimer = interactionDelay;
    }

    public void SwitchActiveProduct(ProductData newProduct)
    {
        if (!UnlockedProducts.Contains(newProduct) || CurrentProduct == newProduct)
        {
            return;
        }

        if (isHoldingProduct)
        {
            Debug.Log("Sacrificed held product to switch.");
        }

        CurrentProduct = newProduct;
        productIconImage.sprite = CurrentProduct.ProductIconUI;
        if (productVisualizer != null)
        {
            productVisualizer.sprite = CurrentProduct.ProductSprite;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("Station_SwitchProduct");

        StartPreparation();

        if (UpgradeUIManager.Instance != null)
        {
            UpgradeUIManager.Instance.RequestRefresh();
        }
    }

    private void SetInitialProduct(ProductData newProduct)
    {
        CurrentProduct = newProduct;
        productIconImage.sprite = CurrentProduct.ProductIconUI;
        if (productVisualizer != null)
        {
            productVisualizer.sprite = CurrentProduct.ProductSprite;
        }

        if (animator != null)
        {
            animator.SetTrigger("isChoosing");
        }

        StartPreparation();
    }

    public void StartPreparation()
    {
        preparationTimer = 0f;
        isPreparing = true;
        isHoldingProduct = false;
        if (progressBarImage != null)
        {
            progressBarImage.fillAmount = 0;
        }
    }

    public void UpgradeSpecialization()
    {
        if (!CanUpgradeSpecialization() || !canInteractFlag) return;

        int cost = GetSpecializeCost();
        if (EconomyManager.Instance.SpendCash(cost))
        {
            TotalValue += cost;
            SpecializationLevel++;
            Debug.Log($"{StationData.StationName} specialized to Level {SpecializationLevel + 1}");
            AudioManager.Instance.PlaySFX("Station_Upgrade");
        }
    }

    public void UnlockNextProduct()
    {
        if (!CanUpgradeDiversify() || !canInteractFlag) return;
        int cost = StationData.DiversifyCost;
        if (EconomyManager.Instance.SpendCash(cost))
        {
            ProductData productToUnlock = null;
            foreach (var potentialProduct in StationData.AvailableProducts)
            {
                if (!UnlockedProducts.Contains(potentialProduct))
                {
                    productToUnlock = potentialProduct;
                    break;
                }
            }

            if (productToUnlock != null)
            {
                TotalValue += cost;
                UnlockedProducts.Add(productToUnlock);
                Debug.Log($"{StationData.StationName} diversified to unlock {productToUnlock.ProductName}");
                AudioManager.Instance.PlaySFX("Station_Upgrade");
            }
            else
            {
                Debug.LogWarning("Tried to diversify, but no new product was found. Refunding cost.");
                EconomyManager.Instance.AddCash(cost);
            }
        }
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

    private float GetCurrentPreparationTime()
    {
        return GetPreparationTime(this.SpecializationLevel);
    }

    private void UpdateProgressBar()
    {
        if (CurrentProduct == null || progressBarImage == null) return;
        float progress = preparationTimer / GetCurrentPreparationTime();
        progressBarImage.fillAmount = Mathf.Clamp01(progress);
    }

    private void CompletePreparation()
    {
        isPreparing = false;
        isHoldingProduct = true;
        progressBarImage.fillAmount = 1;
        if (animator != null) animator.SetTrigger("isCooked");
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("Station_FoodReady");
    }

    private void FindAndServeCustomer()
    {
        Customer targetCustomer = FindCustomerInRange();
        if (targetCustomer != null)
        {
            ServeFoodToCustomer(targetCustomer);
            isHoldingProduct = false;
            StartPreparation();
        }
    }

    private Customer FindCustomerInRange()
    {
        if (CustomerManager.Instance == null) return null;

        Customer nearestCustomer = null;
        float shortestDistance = float.MaxValue;
        foreach (Customer customer in CustomerManager.Instance.GetActiveCustomers())
        {
            if (customer.DoesOrderContain(CurrentProduct))
            {
                float distance = Vector3.Distance(transform.position, customer.transform.position);
                if (distance <= serviceRange && distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestCustomer = customer;
                }
            }
        }
        return nearestCustomer;
    }

    private void ServeFoodToCustomer(Customer target)
    {
        target.NotifyItemInFlight(CurrentProduct);
        if (animator != null) animator.SetTrigger("isShooting");

        if (CurrentProduct.FoodProjectilePrefab == null)
        {
            Debug.LogError($"No projectile prefab assigned for {CurrentProduct.ProductName}!");
            return;
        }

        if (ProjectilePoolManager.Instance == null)
        {
            Debug.LogError("ProjectilePoolManager not found in the scene! Cannot serve food.");
            return;
        }

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;

        GameObject projectileObj = ProjectilePoolManager.Instance.GetProjectile(CurrentProduct.FoodProjectilePrefab);
        projectileObj.transform.position = spawnPosition;
        projectileObj.transform.rotation = Quaternion.identity;

        if (projectileObj.TryGetComponent<FoodProjectile>(out FoodProjectile projectile))
        {
            projectile.Initialize(target, CurrentProduct);
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX("Projectile_Launch");
        }
        StartPreparation();
    }

    public float GetPreparationTime(int specializationLevelOverride)
    {
        if (CurrentProduct == null) return float.MaxValue;

        float speedMultiplier = 1f + (specializationLevelOverride * StationData.SpecializeSpeedBonus);
        float baseTime = CurrentProduct.BasePreparationTime / speedMultiplier;

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
}