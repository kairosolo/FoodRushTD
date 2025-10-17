using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using KairosoloSystems;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioMixer masterMixer;
    [SerializeField] private AudioMixer musicMixer;
    [SerializeField] private AudioMixer sfxMixer;

    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioSource loopingSfxAudioSource;

    [SerializeField] public Sound[] musicTracks;
    [SerializeField] public Sound[] sfxClips;

    private Dictionary<string, AudioClip> musicDictionary;
    private Dictionary<string, AudioClip> sfxDictionary;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        musicDictionary = new Dictionary<string, AudioClip>();
        foreach (Sound sound in musicTracks)
        {
            musicDictionary.Add(sound.name, sound.clip);
        }

        sfxDictionary = new Dictionary<string, AudioClip>();
        foreach (Sound sound in sfxClips)
        {
            sfxDictionary.Add(sound.name, sound.clip);
        }
    }

    private void Start()
    {
        masterMixer.SetFloat("MasterVolume", Mathf.Log10(KPlayerPrefs.GetFloat("MasterVolume", 0.8f)) * 20);
        musicMixer.SetFloat("MusicVolume", Mathf.Log10(KPlayerPrefs.GetFloat("MusicVolume", 0.5f)) * 20);
        sfxMixer.SetFloat("SfxVolume", Mathf.Log10(KPlayerPrefs.GetFloat("SfxVolume", 0.5f)) * 20);

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MenuScene")
        {
            PlayMusic("MenuMusic");
        }
    }

    public void PlayMusic(string name)
    {
        if (musicDictionary.TryGetValue(name, out AudioClip clip))
        {
            musicAudioSource.clip = clip;
            musicAudioSource.loop = true;
            musicAudioSource.Play();
        }
        else
        {
            Debug.LogWarning("AudioManager: Music track not found: " + name);
        }
    }

    public void PlaySFX(string name)
    {
        if (sfxDictionary.TryGetValue(name, out AudioClip clip))
        {
            sfxAudioSource.PlayOneShot(clip);
            Debug.Log(name);
        }
        else
        {
            Debug.LogWarning("AudioManager: SFX clip not found: " + name);
        }
    }

    public void PlayLoopingSFX(string name)
    {
        if (sfxDictionary.TryGetValue(name, out AudioClip clip))
        {
            if (loopingSfxAudioSource.clip != clip || !loopingSfxAudioSource.isPlaying)
            {
                loopingSfxAudioSource.clip = clip;
                loopingSfxAudioSource.loop = true;
                loopingSfxAudioSource.Play();
            }
        }
        else
        {
            Debug.LogWarning("AudioManager: SFX clip not found for looping: " + name);
        }
    }

    public void StopLoopingSFX()
    {
        loopingSfxAudioSource.Stop();
        loopingSfxAudioSource.clip = null;
    }
}