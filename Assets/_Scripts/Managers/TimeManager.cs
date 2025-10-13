using UnityEngine;
using UnityEngine.Audio;
using System;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    public static event Action<float> OnTimeScaleChanged;

    [Header("Audio")]
    [SerializeField] private AudioMixer masterMixer;
    [SerializeField] private string masterPitchParameter = "MasterPitch";

    [Header("Fast Forward Settings")]
    [SerializeField] private float maxTimeScale = 2f;
    [SerializeField] private float timeScaleRampUpSpeed = 1f;

    public float CurrentTimeScale => Time.timeScale;

    private bool isFastForwarding = false;
    private const float DEFAULT_TIME_SCALE = 1f;
    private const float DEFAULT_PITCH = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            if (isFastForwarding) StopFastForward();
            return;
        }

        if (isFastForwarding)
        {
            float newTimeScale = Mathf.Min(maxTimeScale, Time.timeScale + timeScaleRampUpSpeed * Time.unscaledDeltaTime);
            SetTimeScale(newTimeScale);
        }
    }

    public void ToggleFastForward()
    {
        if (Time.timeScale == 0f) return;

        isFastForwarding = !isFastForwarding;

        if (isFastForwarding)
        {
            StartFastForwardRamp();
        }
        else
        {
            StopFastForward();
        }
    }

    public void ResetTimeScale()
    {
        if (isFastForwarding)
        {
            isFastForwarding = false;
        }
        SetTimeScale(DEFAULT_TIME_SCALE);
    }

    private void StartFastForwardRamp()
    {
        SetTimeScale(DEFAULT_TIME_SCALE + 0.01f);
    }

    private void StopFastForward()
    {
        isFastForwarding = false;
        if (Time.timeScale > 0f)
        {
            SetTimeScale(DEFAULT_TIME_SCALE);
        }
    }

    private void SetTimeScale(float newTimeScale)
    {
        Time.timeScale = newTimeScale;

        if (masterMixer != null)
        {
            masterMixer.SetFloat(masterPitchParameter, newTimeScale);
        }

        OnTimeScaleChanged?.Invoke(newTimeScale);
    }

    private void OnDestroy()
    {
        if (Time.timeScale != 0f)
        {
            Time.timeScale = DEFAULT_TIME_SCALE;
        }
        if (masterMixer != null)
        {
            masterMixer.SetFloat(masterPitchParameter, DEFAULT_PITCH);
        }
    }
}