using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCustomerData", menuName = "Food Rush/Customer Data")]
public class CustomerData : ScriptableObject
{
    [Header("Customer Details")]
    [SerializeField] private string customerTypeName;
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private int cashReward = 50;

    [Header("Order Details")]
    [SerializeField] private List<ProductData> potentialOrder;

    [Header("VIP Settings")]
    [SerializeField] private bool isVip = false;
    [SerializeField] private float patienceDuration = 60f;

    public string CustomerTypeName => customerTypeName;
    public GameObject CustomerPrefab => customerPrefab;
    public float MoveSpeed => moveSpeed;
    public int CashReward => cashReward;
    public List<ProductData> PotentialOrder => potentialOrder;
    public bool IsVip => isVip;
    public float PatienceDuration => patienceDuration;
}