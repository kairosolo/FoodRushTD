using UnityEngine;
using System.Collections.Generic;

public class Customer : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float satisfiedMoveSpeedMultiplier = 2.5f;

    [Header("Order Information")]
    [SerializeField] private List<ProductData> order;
    [SerializeField] private int cashReward = 50;

    private int nextWaypointIndex;
    private Vector3 startPosition;
    private Vector3 targetWaypointPosition;
    private float travelTime;
    private float lerpTimer;
    private bool isMoving;
    private bool isOrderComplete = false;

    public void Initialize(List<ProductData> customerOrder)
    {
        order = new List<ProductData>(customerOrder);
        CustomerManager.Instance.AddCustomer(this);
        transform.position = PathManager.Instance.GetWaypoint(0).position;
        nextWaypointIndex = 1;
        SetNextWaypoint();
    }

    private void Update()
    {
        if (!isMoving) return;

        lerpTimer += Time.deltaTime;
        float t = Mathf.Clamp01(lerpTimer / travelTime);
        transform.position = Vector3.Lerp(startPosition, targetWaypointPosition, t);

        if (t >= 1f)
        {
            SetNextWaypoint();
        }
    }

    public bool DoesOrderContain(ProductData product)
    {
        if (isOrderComplete) return false;

        return order.Contains(product);
    }

    public void ReceiveFoodItem(ProductData receivedProduct)
    {
        if (order.Contains(receivedProduct))
        {
            order.Remove(receivedProduct);
            Debug.Log("Customer received correct item!");

            if (order.Count == 0)
            {
                HandleOrderComplete();
            }
        }
    }

    private void HandleOrderComplete()
    {
        isOrderComplete = true;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.5f, 1f, 0.5f, 0.5f);
        }
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
        if (nextWaypointIndex >= PathManager.Instance.WaypointCount)
        {
            HandleReachingExit();
            return;
        }
        startPosition = transform.position;
        targetWaypointPosition = PathManager.Instance.GetWaypoint(nextWaypointIndex).position;
        float distance = Vector3.Distance(startPosition, targetWaypointPosition);
        if (distance > 0.01f)
        {
            travelTime = distance / moveSpeed;
            lerpTimer = 0f;
            isMoving = true;
        }
        else
        {
            travelTime = 0f;
            isMoving = false;
        }
        nextWaypointIndex++;
    }

    private void HandleReachingExit()
    {
        isMoving = false;
        if (isOrderComplete)
        {
            Debug.Log($"Satisfied customer has left. +${cashReward}");
            EconomyManager.Instance.AddCash(cashReward);
        }
        else
        {
            GameLoopManager.Instance.CustomerReachedExitUnsatisfied();
        }
        CustomerManager.Instance.RemoveCustomer(this);
        Destroy(gameObject);
    }
}