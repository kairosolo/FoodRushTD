using UnityEngine;
using TMPro;
using System.Collections.Generic;
using KairosoloSystems;

[RequireComponent(typeof(TMP_Dropdown))]
public class ResolutionDropdown : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    private const string RES_INDEX_KEY = "ResolutionIndex";

    private void Start()
    {
        dropdown.ClearOptions();

        List<string> options = new List<string>();
        foreach (var res in SettingsManager.Instance.uniqueResolutions)
        {
            options.Add($"{res.width} x {res.height}");
        }
        dropdown.AddOptions(options);

        dropdown.value = KPlayerPrefs.GetInt(RES_INDEX_KEY, SettingsManager.Instance.uniqueResolutions.Count - 1);
        dropdown.RefreshShownValue();

        dropdown.onValueChanged.AddListener(SettingsManager.Instance.SetResolution);
    }
}