using UnityEngine;
using TMPro;
using KairosoloSystems;
using System.Collections.Generic;

[RequireComponent(typeof(TMP_Dropdown))]
public class FrameRateDropdown : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    private void Start()
    {
        dropdown.AddOptions(new List<string> { "30 FPS", "60 FPS", "120 FPS", "Unlimited" });
        dropdown.value = KPlayerPrefs.GetInt("FrameRateCapIndex", 1);
        dropdown.RefreshShownValue();
        dropdown.onValueChanged.AddListener(SettingsManager.Instance.SetFrameRateCap);
    }
}