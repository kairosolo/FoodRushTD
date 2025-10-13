using UnityEngine;
using System.Collections.Generic;

public class CustomerManager : MonoBehaviour
{
    public static CustomerManager Instance { get; private set; }

    private List<Customer> activeCustomers = new List<Customer>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void AddCustomer(Customer customer)
    {
        if (!activeCustomers.Contains(customer))
        {
            activeCustomers.Add(customer);
        }
    }

    public void RemoveCustomer(Customer customer)
    {
        if (activeCustomers.Contains(customer))
        {
            activeCustomers.Remove(customer);
        }
    }

    public List<Customer> GetActiveCustomers()
    {
        return new List<Customer>(activeCustomers);
    }
}