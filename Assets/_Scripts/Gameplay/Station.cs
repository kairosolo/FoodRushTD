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
    [SerializeField] private GameObject placementInfo;
    [SerializeField] private SpriteRenderer productVisualizer;
    [SerializeField] private Animator animator;
    [SerializeField] private Image productIconImage;
    [SerializeField] private Image progressBarImage;

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

    public bool CanInteract() => canInteractFlag;

    private void OnEnable()
    {
        StationManager.Instance.AddStation(this);
    }

    private void OnDisable()
    {
        if (StationManager.Instance != null)
        {
            StationManager.Instance.RemoveStation(this);
        }
    }

    /*public void Initialize(StationData data) Keeping just in case.
    {
        if (animator != null)
        {
            animator.SetTrigger("isPlacing");
        }
        if (characterRandomizer != null)
        {
            characterRandomizer.RandomizeAll();
        }

        StationData = data;
        SpecializationLevel = 0;
        UnlockedProducts = new List<ProductData>();

        if (StationData.AvailableProducts != null && StationData.AvailableProducts.Count > 0)
        {
            UnlockedProducts.Add(StationData.AvailableProducts[0]);
            SetInitialProduct(UnlockedProducts[0]);
        }
        else
        {
            Debug.LogError("Station has no available products assigned in its StationData!");
            isPreparing = false;
        }

        canInteractFlag = false;
        interactionTimer = interactionDelay;
    }*/

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

    public void PartialInitialize(StationData data)
    {
        StationData = data;
        SpecializationLevel = 0;
        UnlockedProducts = new List<ProductData>();

        this.enabled = false;
    }

    public void TriggerPlacementEffects()
    {
        if (characterRandomizer != null)
        {
            characterRandomizer.RandomizeAll();
        }

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

        UnlockedProducts.Add(initialProduct);
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
        productIconImage.sprite = CurrentProduct.ProductSprite;
        if (productVisualizer != null)
        {
            productVisualizer.sprite = CurrentProduct.ProductSprite;
        }

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
        productIconImage.sprite = CurrentProduct.ProductSprite;
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

    public void UpgradeSpecialization()
    {
        if (!CanUpgradeSpecialization() || !canInteractFlag) return;

        int cost = GetSpecializeCost();
        if (EconomyManager.Instance.SpendCash(cost))
        {
            SpecializationLevel++;
            Debug.Log($"{StationData.StationName} specialized to Level {SpecializationLevel + 1}");
            AudioManager.Instance.PlaySFX("Station_Upgrade");
        }
    }

    public void UnlockNextProduct()
    {
        if (!CanUpgradeDiversify() || !canInteractFlag) return;

        if (EconomyManager.Instance.SpendCash(StationData.DiversifyCost))
        {
            ProductData productToUnlock = StationData.AvailableProducts[UnlockedProducts.Count];
            UnlockedProducts.Add(productToUnlock);
            Debug.Log($"{StationData.StationName} diversified to unlock {productToUnlock.ProductName}");
            AudioManager.Instance.PlaySFX("Station_Upgrade");
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
        if (CurrentProduct == null) return float.MaxValue;
        float speedMultiplier = 1f + (SpecializationLevel * StationData.SpecializeSpeedBonus);
        return CurrentProduct.BasePreparationTime / speedMultiplier;
    }

    private void StartPreparation()
    {
        preparationTimer = 0f;
        isPreparing = true;
        isHoldingProduct = false;
        progressBarImage.fillAmount = 0;
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
        if (animator != null)
        {
            animator.SetTrigger("isCooked");
        }
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

        if (animator != null)
        {
            animator.SetTrigger("isShooting");
        }

        if (CurrentProduct.FoodProjectilePrefab == null)
        {
            Debug.LogError($"No projectile prefab assigned for {CurrentProduct.ProductName}!");
            return;
        }

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        GameObject projectileObj = Instantiate(CurrentProduct.FoodProjectilePrefab, spawnPosition, Quaternion.identity);
        if (projectileObj.TryGetComponent<FoodProjectile>(out FoodProjectile projectile))
        {
            projectile.Initialize(target, CurrentProduct);
            AudioManager.Instance.PlaySFX("Projectile_Launch");
        }
    }
}