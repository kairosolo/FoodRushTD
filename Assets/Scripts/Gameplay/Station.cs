using UnityEngine;
using UnityEngine.UI;

public class Station : MonoBehaviour
{
    [Header("Station Settings")]
    [SerializeField] private float serviceRange = 5f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float clickRadius = 1f;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionDelay = 0.5f;

    [Header("UI References")]
    [SerializeField] private Image productIconImage;
    [SerializeField] private Image progressBarImage;

    private StationData stationData;
    private ProductData currentProduct;
    private float preparationTimer;
    private bool isPreparing;
    private bool isHoldingProduct;
    private int currentProductIndex = 0;

    private bool canInteract;
    private float interactionTimer;

    public float ClickRadius => clickRadius;

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

    public void Initialize(StationData data)
    {
        stationData = data;
        if (stationData.AvailableProducts != null && stationData.AvailableProducts.Count > 0)
        {
            currentProductIndex = 0;
            SwitchProduct(stationData.AvailableProducts[currentProductIndex]);
        }
        else
        {
            Debug.LogError("Station has no available products!");
        }

        canInteract = false;
        interactionTimer = interactionDelay;
    }

    private void Update()
    {
        if (!canInteract)
        {
            interactionTimer -= Time.deltaTime;
            if (interactionTimer <= 0f)
            {
                canInteract = true;
            }
        }

        if (isPreparing)
        {
            preparationTimer += Time.deltaTime;
            UpdateProgressBar();
            if (preparationTimer >= currentProduct.BasePreparationTime)
            {
                CompletePreparation();
            }
        }
        else if (isHoldingProduct)
        {
            FindAndServeCustomer();
        }
    }

    public void CycleProduct()
    {
        if (!canInteract)
        {
            Debug.Log("Station is not ready for interaction yet.");
            return;
        }

        if (isHoldingProduct)
        {
            Debug.Log("Cannot switch product while holding a finished item!");
            return;
        }

        if (stationData.AvailableProducts.Count <= 1)
        {
            Debug.Log("No other products to switch to.");
            return;
        }

        currentProductIndex++;
        if (currentProductIndex >= stationData.AvailableProducts.Count)
        {
            currentProductIndex = 0;
        }

        ProductData nextProduct = stationData.AvailableProducts[currentProductIndex];

        if (EconomyManager.Instance.SpendCash(10))
        {
            Debug.Log($"Switched station to {nextProduct.ProductName}");
            SwitchProduct(nextProduct);
        }
    }

    private void SwitchProduct(ProductData newProduct)
    {
        currentProduct = newProduct;
        productIconImage.sprite = currentProduct.ProductIcon;
        productIconImage.gameObject.SetActive(true);
        StartPreparation();
    }

    private void StartPreparation()
    {
        preparationTimer = 0f;
        isPreparing = true;
        isHoldingProduct = false;
        progressBarImage.fillAmount = 0;
    }

    private void CompletePreparation()
    {
        isPreparing = false;
        isHoldingProduct = true;
        progressBarImage.fillAmount = 1;
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
            if (customer.DoesOrderContain(currentProduct))
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
        if (currentProduct.FoodProjectilePrefab == null)
        {
            Debug.LogError($"No projectile prefab assigned for {currentProduct.ProductName}!");
            return;
        }
        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        GameObject projectileObj = Instantiate(currentProduct.FoodProjectilePrefab, spawnPosition, Quaternion.identity);
        if (projectileObj.TryGetComponent<FoodProjectile>(out FoodProjectile projectile))
        {
            projectile.Initialize(target, currentProduct);
        }
    }

    private void UpdateProgressBar()
    {
        if (currentProduct == null || progressBarImage == null) return;
        float progress = preparationTimer / currentProduct.BasePreparationTime;
        progressBarImage.fillAmount = Mathf.Clamp01(progress);
    }
}