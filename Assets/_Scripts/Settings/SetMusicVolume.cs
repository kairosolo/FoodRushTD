using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using KairosoloSystems;

public class SetMusicVolume : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        if (!KPlayerPrefs.HasKey("MusicVolume"))
        {
            KPlayerPrefs.SetFloat("MusicVolume", .5f);
        }
    }

    private void Start()
    {
        slider.value = KPlayerPrefs.GetFloat("MusicVolume", .5f);
        mixer.SetFloat("MusicVolume", Mathf.Log10(slider.value) * 20);
    }

    private void OnEnable()
    {
        slider.value = KPlayerPrefs.GetFloat("MusicVolume", .5f);
    }

    public void SetLevel(float sliderValue)
    {
        mixer.SetFloat("MusicVolume", Mathf.Log10(sliderValue) * 20);
        KPlayerPrefs.SetFloat("MusicVolume", sliderValue);
        if (Time.timeSinceLevelLoad > 0.1f)
        {
            AudioManager.Instance.PlaySFX("UI_Slider_Adjust");
        }
    }
}