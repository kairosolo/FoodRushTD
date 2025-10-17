using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class WorldLightManager : MonoBehaviour
{
    public static WorldLightManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Light2D globalLight;

    [Header("Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float intensityThreshold = 0.8f;

    private List<WorldLight> worldLights = new List<WorldLight>();
    private bool areLightsOn = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }

    private void OnEnable() => GameClock.OnTimeChanged += CheckLightingCondition;

    private void OnDisable() => GameClock.OnTimeChanged -= CheckLightingCondition;

    private void Start()
    {
        if (globalLight != null)
        {
            if (globalLight.intensity < intensityThreshold)
            {
                TurnAllLightsOn();
            }
            else
            {
                TurnAllLightsOff();
            }
        }
    }

    public void AddLight(WorldLight light)
    {
        if (!worldLights.Contains(light))
        {
            worldLights.Add(light);
            if (areLightsOn) light.TurnOn();
            else light.TurnOff();
        }
    }

    public void RemoveLight(WorldLight light)
    {
        if (worldLights.Contains(light))
        {
            worldLights.Remove(light);
        }
    }

    private void CheckLightingCondition(int hour, int minute)
    {
        if (globalLight == null) return;

        bool shouldBeOn = globalLight.intensity < intensityThreshold;

        if (shouldBeOn != areLightsOn)
        {
            if (shouldBeOn)
            {
                TurnAllLightsOn();
            }
            else
            {
                TurnAllLightsOff();
            }
        }
    }

    private void TurnAllLightsOn()
    {
        foreach (var light in worldLights)
        {
            light.TurnOn();
        }
        areLightsOn = true;
        AudioManager.Instance.PlaySFX("Lights_On_Click");
    }

    private void TurnAllLightsOff()
    {
        foreach (var light in worldLights)
        {
            light.TurnOff();
        }
        areLightsOn = false;
        AudioManager.Instance.PlaySFX("Lights_Off_Click");
    }
}