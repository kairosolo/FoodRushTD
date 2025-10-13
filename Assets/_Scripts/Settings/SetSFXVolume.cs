using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using KairosoloSystems;

public class SetSFXVolume : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        if (!KPlayerPrefs.HasKey("SfxVolume"))
        {
            KPlayerPrefs.SetFloat("SfxVolume", .5f);
        }
    }

    private void Start()
    {
        slider.value = KPlayerPrefs.GetFloat("SfxVolume", .5f);
        mixer.SetFloat("SfxVolume", Mathf.Log10(slider.value) * 20);
    }

    private void OnEnable()
    {
        slider.value = KPlayerPrefs.GetFloat("SfxVolume", .5f);
    }

    public void SetLevel(float sliderValue)
    {
        mixer.SetFloat("SfxVolume", Mathf.Log10(sliderValue) * 20);
        KPlayerPrefs.SetFloat("SfxVolume", sliderValue);
    }
}