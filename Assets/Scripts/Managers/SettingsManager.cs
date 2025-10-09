using UnityEngine;
using System.Collections.Generic;
using KairosoloSystems;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public List<Resolution> uniqueResolutions { get; private set; }

    private const string RES_INDEX_KEY = "ResolutionIndex";
    private const string FULLSCREEN_KEY = "IsFullscreen";
    private const string FPS_CAP_KEY = "FrameRateCapIndex";
    private const string GRAPHICS_QUALITY_KEY = "GraphicsQualityIndex";

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

        uniqueResolutions = new List<Resolution>();
        Resolution[] allResolutions = Screen.resolutions;
        HashSet<string> addedResolutions = new HashSet<string>();
        for (int i = allResolutions.Length - 1; i >= 0; i--)
        {
            Resolution res = allResolutions[i];
            string resolutionString = $"{res.width} x {res.height}";
            if (!addedResolutions.Contains(resolutionString))
            {
                uniqueResolutions.Insert(0, res);
                addedResolutions.Add(resolutionString);
            }
        }
    }

    private void Start()
    {
        LoadAndApplySettings();
    }

    private void LoadAndApplySettings()
    {
        int resolutionIndex = KPlayerPrefs.GetInt(RES_INDEX_KEY, uniqueResolutions.Count - 1);
        bool isFullscreen = KPlayerPrefs.GetInt(FULLSCREEN_KEY, 1) == 1;

        if (resolutionIndex >= uniqueResolutions.Count)
        {
            resolutionIndex = uniqueResolutions.Count - 1;
        }
        ApplyResolution(resolutionIndex, isFullscreen);

        int fpsIndex = KPlayerPrefs.GetInt(FPS_CAP_KEY, 1);
        SetFrameRateCap(fpsIndex);

        int qualityIndex = KPlayerPrefs.GetInt(GRAPHICS_QUALITY_KEY, QualitySettings.GetQualityLevel());
        SetGraphicsQuality(qualityIndex);
    }

    private void ApplyResolution(int index, bool fullscreen)
    {
        if (index < 0 || index >= uniqueResolutions.Count) return;
        Resolution res = uniqueResolutions[index];
        Screen.SetResolution(res.width, res.height, fullscreen);
    }

    public void SetResolution(int resolutionIndex)
    {
        ApplyResolution(resolutionIndex, Screen.fullScreen);
        KPlayerPrefs.SetInt(RES_INDEX_KEY, resolutionIndex);
        KPlayerPrefs.Save();
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        KPlayerPrefs.SetInt(FULLSCREEN_KEY, isFullscreen ? 1 : 0);
        KPlayerPrefs.Save();
    }

    public void SetGraphicsQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        KPlayerPrefs.SetInt(GRAPHICS_QUALITY_KEY, qualityIndex);
        KPlayerPrefs.Save();
    }

    public void SetFrameRateCap(int index)
    {
        switch (index)
        {
            case 0: Application.targetFrameRate = 30; break;
            case 1: Application.targetFrameRate = 60; break;
            case 2: Application.targetFrameRate = 120; break;
            case 3: Application.targetFrameRate = 0; break;
        }
        KPlayerPrefs.SetInt(FPS_CAP_KEY, index);
        KPlayerPrefs.Save();
    }
}