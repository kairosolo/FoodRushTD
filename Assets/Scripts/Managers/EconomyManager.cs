using UnityEngine;
using System;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    public static event Action<int> OnCashChanged;

    [SerializeField] private int startingCash = 250;

    public int CurrentCash { get; private set; }
    public int TotalCashEarned { get; private set; }

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

    private void Start()
    {
        CurrentCash = startingCash;
        TotalCashEarned = 0;
        OnCashChanged?.Invoke(CurrentCash);
    }

    public void AddCash(int amount)
    {
        CurrentCash += amount;
        TotalCashEarned += amount;
        OnCashChanged?.Invoke(CurrentCash);
    }

    public bool SpendCash(int amount)
    {
        if (CurrentCash >= amount)
        {
            CurrentCash -= amount;
            OnCashChanged?.Invoke(CurrentCash);
            return true;
        }
        // Trigger NotEnoughCashSFX
        Debug.Log("Not enough cash!");
        return false;
    }
}