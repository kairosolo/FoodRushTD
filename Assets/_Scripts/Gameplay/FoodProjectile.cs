using UnityEngine;

public class FoodProjectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float travelSpeed = 10f;
    [SerializeField] private float rotZSpeed = 10f;

    private Customer targetCustomer;
    private ProductData productData;

    public void Initialize(Customer target, ProductData product)
    {
        targetCustomer = target;
        productData = product;
        Destroy(gameObject, 5f);
    }

    private void Update()
    {
        if (targetCustomer == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 direction = (targetCustomer.transform.position - transform.position).normalized;
        transform.position += direction * travelSpeed * Time.deltaTime;

        transform.Rotate(0f, 0f, rotZSpeed * Time.deltaTime);

        float distanceToTarget = Vector3.Distance(transform.position, targetCustomer.transform.position);
        if (distanceToTarget < 0.2f)
        {
            targetCustomer.ReceiveFoodItem(productData);
            VFXManager.Instance.PlayVFX("projectileExplodeVFX", transform.position, transform.rotation);
            // Trigger ItemDeliveredVFX
            // Trigger ItemDeliveredSFX

            Destroy(gameObject);
        }
    }
}