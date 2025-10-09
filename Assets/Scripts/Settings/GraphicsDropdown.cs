using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using KairosoloSystems;

[RequireComponent(typeof(TMP_Dropdown))]
public class GraphicsDropdown : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    private void Start()
    {
        List<string> qualityLevels = QualitySettings.names.ToList();
        dropdown.AddOptions(qualityLevels);

        int currentQualityIndex = KPlayerPrefs.GetInt("GraphicsQualityIndex", QualitySettings.GetQualityLevel());
        dropdown.value = currentQualityIndex;
        dropdown.RefreshShownValue();

        dropdown.onValueChanged.AddListener(SettingsManager.Instance.SetGraphicsQuality);
    }
}