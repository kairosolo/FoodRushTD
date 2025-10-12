using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class OrderItem
{
    public ProductData product;

    [Range(1, 5)]
    public int quantity = 1;
}

[CreateAssetMenu(fileName = "NewCustomerData", menuName = "Food Rush/Customer Data")]
public class CustomerData : ScriptableObject
{
    [Header("Customer Details")]
    [SerializeField] private string customerTypeName;
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private int cashReward = 50;

    [Header("Order Details")]
    [SerializeField] private List<OrderItem> potentialOrder;

    [Header("VIP Settings")]
    [SerializeField] private bool isVip = false;
    [SerializeField] private float patienceDuration = 60f;

    public List<OrderItem> PotentialOrder => potentialOrder;
    public GameObject CustomerPrefab => customerPrefab;
    public string CustomerTypeName => customerTypeName;
    public float MoveSpeed => moveSpeed;
    public int CashReward => cashReward;
    public bool IsVip => isVip;
    public float PatienceDuration => patienceDuration;
}