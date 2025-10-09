using UnityEngine;
using UnityEngine.UI;
using KairosoloSystems;

[RequireComponent(typeof(Toggle))]
public class FullscreenToggle : MonoBehaviour
{
    [SerializeField] private Toggle toggle;

    private void Start()
    {
        toggle.isOn = KPlayerPrefs.GetInt("IsFullscreen", 1) == 1;
        toggle.onValueChanged.AddListener(SettingsManager.Instance.SetFullscreen);
    }
}