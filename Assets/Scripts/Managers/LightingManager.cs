using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light2D globalLight;

    [Header("Color Settings")]
    [SerializeField] private Color sunriseColor = new Color(1f, 0.75f, 0.5f);
    [SerializeField] private Color middayColor = Color.white;
    [SerializeField] private Color sunsetColor = new Color(1f, 0.6f, 0.4f);
    [SerializeField] private Color midnightColor = new Color(0.1f, 0.15f, 0.3f);

    [Header("Intensity Settings")]
    [SerializeField] private float dayIntensity = 1.0f;
    [SerializeField] private float nightIntensity = 0.5f;

    [Header("Time Anchors (24-Hour Clock)")]
    [Range(0, 24)][SerializeField] private float sunriseHour = 6f;  //  6:00 AM
    [Range(0, 24)][SerializeField] private float middayHour = 12f; // 12:00 PM
    [Range(0, 24)][SerializeField] private float sunsetHour = 18f; //  6:00 PM
    [Range(0, 24)][SerializeField] private float midnightHour = 0f;  // 12:00 AM

    private void OnEnable() => GameClock.OnTimeChanged += UpdateLighting;

    private void OnDisable() => GameClock.OnTimeChanged -= UpdateLighting;

    private void Start()
    {
        if (GameClock.Instance != null)
        {
            UpdateLighting(GameClock.Instance.CurrentHour, GameClock.Instance.CurrentMinute);
        }
    }

    private void UpdateLighting(int hour, int minute)
    {
        if (globalLight == null) return;

        float currentTime = hour + (minute / 60f);

        Color targetColor;
        float targetIntensity;

        // Window 1: Midnight -> Sunrise
        if (currentTime >= midnightHour && currentTime < sunriseHour)
        {
            float t = RemapTime(currentTime, midnightHour, sunriseHour);
            targetColor = Color.Lerp(midnightColor, sunriseColor, t);
            targetIntensity = Mathf.Lerp(nightIntensity, dayIntensity, t);
        }
        // Window 2: Sunrise -> Midday
        else if (currentTime >= sunriseHour && currentTime < middayHour)
        {
            float t = RemapTime(currentTime, sunriseHour, middayHour);
            targetColor = Color.Lerp(sunriseColor, middayColor, t);
            targetIntensity = dayIntensity;
        }
        // Window 3: Midday -> Sunset
        else if (currentTime >= middayHour && currentTime < sunsetHour)
        {
            float t = RemapTime(currentTime, middayHour, sunsetHour);
            targetColor = Color.Lerp(middayColor, sunsetColor, t);
            targetIntensity = dayIntensity;
        }
        // Window 4: Sunset -> Midnight (crosses the 24h mark)
        else
        {
            float t = RemapTime(currentTime, sunsetHour, 24f + midnightHour);
            targetColor = Color.Lerp(sunsetColor, midnightColor, t);
            targetIntensity = Mathf.Lerp(dayIntensity, nightIntensity, t);
        }

        globalLight.color = targetColor;
        globalLight.intensity = targetIntensity;
    }

    private float RemapTime(float currentTime, float startTime, float endTime)
    {
        if (endTime < startTime)
        {
            if (currentTime >= startTime)
                endTime += 24f; // ex. 18:00 -> 24:00
            else
                currentTime += 24f; // ex. 01:00 -> 25:00
        }

        float duration = endTime - startTime;
        float elapsed = currentTime - startTime;

        return Mathf.Clamp01(elapsed / duration);
    }
}