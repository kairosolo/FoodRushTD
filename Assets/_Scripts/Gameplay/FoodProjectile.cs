using UnityEngine;

public class FoodProjectile : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private float rotZSpeed = 360f;

    private Customer targetCustomer;
    private ProductData productData;

    private ProductData.ProjectileMovementType movementType;
    private float travelDuration;
    private float amplitude;
    private float frequency;
    private float elapsedTime;

    private Vector3 startPosition;
    private Vector3 controlPoint;

    public void Initialize(Customer target, ProductData product)
    {
        targetCustomer = target;
        productData = product;

        elapsedTime = 0f;

        movementType = productData.MovementType;
        amplitude = productData.Amplitude;
        frequency = productData.Frequency;

        startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, target.transform.position);
        travelDuration = distance / productData.ProjectileSpeed;

        if (travelDuration <= 0) travelDuration = 0.1f;

        if (movementType == ProductData.ProjectileMovementType.Lob)
        {
            Vector3 directionToTarget = (target.transform.position - startPosition).normalized;
            Vector3 perpendicular = new Vector3(-directionToTarget.y, directionToTarget.x, 0);
            controlPoint = startPosition + (target.transform.position - startPosition) / 2 + perpendicular * amplitude;
        }
    }

    private void Update()
    {
        if (targetCustomer == null)
        {
            ProjectilePoolManager.Instance.ReturnProjectile(gameObject);
            return;
        }

        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / travelDuration);

        MoveProjectile(t);

        transform.Rotate(0f, 0f, rotZSpeed * Time.deltaTime);

        float distanceToTarget = Vector3.Distance(transform.position, targetCustomer.transform.position);
        if (t >= 1f || distanceToTarget < 0.2f)
        {
            targetCustomer.ReceiveFoodItem(productData);
            VFXManager.Instance.PlayVFX("projectileExplodeVFX", transform.position, transform.rotation);
            AudioManager.Instance.PlaySFX("Projectile_Impact");

            ProjectilePoolManager.Instance.ReturnProjectile(gameObject);
        }
    }

    private void MoveProjectile(float t)
    {
        Vector3 targetPos = targetCustomer.transform.position;

        switch (movementType)
        {
            case ProductData.ProjectileMovementType.Linear:
                transform.position = Vector3.Lerp(startPosition, targetPos, t);
                break;

            case ProductData.ProjectileMovementType.Lob:
                float oneMinusT = 1 - t;
                transform.position = (oneMinusT * oneMinusT * startPosition) +
                                     (2 * oneMinusT * t * controlPoint) +
                                     (t * t * targetPos);
                break;

            case ProductData.ProjectileMovementType.Wobble:
                Vector3 linearPositionWobble = Vector3.Lerp(startPosition, targetPos, t);
                Vector3 directionWobble = (targetPos - startPosition).normalized;
                Vector3 perpendicularWobble = Vector3.Cross(directionWobble, Vector3.forward);
                float wobbleOffset = Mathf.Sin(t * frequency * Mathf.PI) * amplitude;
                transform.position = linearPositionWobble + (perpendicularWobble * wobbleOffset);
                break;

            case ProductData.ProjectileMovementType.Spiral:
                Vector3 linearPositionSpiral = Vector3.Lerp(startPosition, targetPos, t);
                Vector3 directionSpiral = (targetPos - startPosition).normalized;
                Vector3 perpendicularSpiral = Vector3.Cross(directionSpiral, Vector3.forward);
                Vector3 perpendicularSpiral2 = Vector3.Cross(directionSpiral, perpendicularSpiral);

                float angle = t * frequency * Mathf.PI * 2;
                float radius = amplitude * (1 - t);

                Vector3 offset = (perpendicularSpiral * Mathf.Cos(angle) + perpendicularSpiral2 * Mathf.Sin(angle)) * radius;
                transform.position = linearPositionSpiral + offset;
                break;
        }
    }
}