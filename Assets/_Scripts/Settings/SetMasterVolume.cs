using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using KairosoloSystems;

[RequireComponent(typeof(Slider))]
public class SetMasterVolume : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        if (!KPlayerPrefs.HasKey("MasterVolume"))
        {
            KPlayerPrefs.SetFloat("MasterVolume", 0.8f);
        }
    }

    private void Start()
    {
        slider.value = KPlayerPrefs.GetFloat("MasterVolume", .5f);
        mixer.SetFloat("MasterVolume", Mathf.Log10(slider.value) * 20);
    }

    private void OnEnable()
    {
        slider.value = KPlayerPrefs.GetFloat("MasterVolume");
        SetLevel(slider.value);
    }

    public void SetLevel(float sliderValue)
    {
        mixer.SetFloat("MasterVolume", Mathf.Log10(sliderValue) * 20);
        KPlayerPrefs.SetFloat("MasterVolume", sliderValue);
        if (Time.timeSinceLevelLoad > 0.1f)
        {
            AudioManager.Instance.PlaySFX("UI_Slider_Adjust");
        }
    }
}