using UnityEngine;
using UnityEngine.UI;

public class Station : MonoBehaviour
{
    [Header("Station Settings")]
    [SerializeField] private float serviceRange = 5f;
    [SerializeField] private Transform firePoint;

    [Header("UI References")]
    [SerializeField] private Image productIconImage;
    [SerializeField] private Image progressBarImage;

    private StationData stationData;
    private ProductData currentProduct;
    private float preparationTimer;
    private bool isPreparing;
    private bool isHoldingProduct;

    public void Initialize(StationData data)
    {
        stationData = data;
        if (stationData.AvailableProducts != null && stationData.AvailableProducts.Count > 0)
        {
            SwitchProduct(stationData.AvailableProducts[0]);
        }
        else
        {
            Debug.LogError("Station has no available products!");
            isPreparing = false;
        }
    }

    private void Update()
    {
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
            // If we have a product ready, constantly check for a customer to serve
            FindAndServeCustomer();
        }
    }

    public void SwitchProduct(ProductData newProduct)
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
        // Trigger SwitchProductSFX
    }

    private void CompletePreparation()
    {
        isPreparing = false;
        isHoldingProduct = true;
        progressBarImage.fillAmount = 1;
        Debug.Log($"Preparation for {currentProduct.ProductName} complete! Holding product.");
        // Play FoodReadySFX
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
            // Check if the customer actually needs this product
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
            // Trigger ServeFoodSFX
        }
    }

    private void UpdateProgressBar()
    {
        if (currentProduct == null || progressBarImage == null) return;

        float progress = preparationTimer / currentProduct.BasePreparationTime;
        progressBarImage.fillAmount = Mathf.Clamp01(progress);
    }
}