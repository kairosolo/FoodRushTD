using UnityEngine;
using UnityEngine.UI;
using KairosoloSystems;
using System;
using System.IO;
using TMPro;

public class KPlayerPrefsExample : MonoBehaviour
{
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Profile Management UI")]
    [SerializeField] private TMP_Dropdown profileDropdown;
    [SerializeField] private TMP_InputField newProfileInput;
    [SerializeField] private Button createProfileButton;
    [SerializeField] private Button deleteProfileButton;
    [SerializeField] private Button copyProfileButton;
    [SerializeField] private TextMeshProUGUI activeProfileLabel;

    [Header("Global Data UI")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_InputField languageInput;
    [SerializeField] private TextMeshProUGUI globalDataDisplay;

    [Header("Profile Data UI")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Slider levelSlider;
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private Toggle tutorialToggle;
    [SerializeField] private Button levelUpButton;
    [SerializeField] private Button gainExpButton;
    [SerializeField] private Button positionButton;
    [SerializeField] private Button colorButton;
    [SerializeField] private TextMeshProUGUI profileDataDisplay;

    [Header("Import/Export UI")]
    [SerializeField] private Button importGlobalDataButton;
    [SerializeField] private Button exportGlobalDataButton;
    [SerializeField] private Button importProfileDataButton;
    [SerializeField] private Button exportProfileDataButton;
    [SerializeField] private TextMeshProUGUI importExportStatusDisplay;

    private bool isInitialized = false;

    private void Start()
    {
        InitializeUI();
        SetupInitialData();
        RefreshAllDisplays();
        isInitialized = true;
    }

    #region UI Initialization

    private void InitializeUI()
    {
        // Profile Management
        if (createProfileButton) createProfileButton.onClick.AddListener(CreateNewProfile);
        if (deleteProfileButton) deleteProfileButton.onClick.AddListener(DeleteCurrentProfile);
        if (copyProfileButton) copyProfileButton.onClick.AddListener(CopyCurrentProfile);
        if (profileDropdown) profileDropdown.onValueChanged.AddListener(OnProfileDropdownChanged);

        // Global Data
        if (volumeSlider) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        if (qualityDropdown) qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        if (fullscreenToggle) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        if (languageInput) languageInput.onEndEdit.AddListener(OnLanguageChanged);

        // Profile Data
        if (playerNameInput) playerNameInput.onEndEdit.AddListener(OnPlayerNameChanged);
        if (levelSlider) levelSlider.onValueChanged.AddListener(OnLevelChanged);
        if (experienceSlider) experienceSlider.onValueChanged.AddListener(OnExperienceChanged);
        if (tutorialToggle) tutorialToggle.onValueChanged.AddListener(OnTutorialChanged);
        if (levelUpButton) levelUpButton.onClick.AddListener(LevelUpPlayer);
        if (gainExpButton) gainExpButton.onClick.AddListener(GainExperience);
        if (positionButton) positionButton.onClick.AddListener(RandomPosition);
        if (colorButton) colorButton.onClick.AddListener(RandomColor);

        // Update import/export status display
        RefreshImportExportStatus();

        // Import/Export
        if (importGlobalDataButton) importGlobalDataButton.onClick.AddListener(ImportGlobalData);
        if (exportGlobalDataButton) exportGlobalDataButton.onClick.AddListener(ExportGlobalData);
        if (importProfileDataButton) importProfileDataButton.onClick.AddListener(ImportProfileData);
        if (exportProfileDataButton) exportProfileDataButton.onClick.AddListener(ExportProfileData);

        // Setup quality dropdown options
        if (qualityDropdown)
        {
            qualityDropdown.options.Clear();
            qualityDropdown.options.Add(new TMP_Dropdown.OptionData("Low"));
            qualityDropdown.options.Add(new TMP_Dropdown.OptionData("Medium"));
            qualityDropdown.options.Add(new TMP_Dropdown.OptionData("High"));
            qualityDropdown.options.Add(new TMP_Dropdown.OptionData("Ultra"));
        }
    }

    private void SetupInitialData()
    {
        // Global Data Setup
        if (!KPlayerPrefs.HasKey("MasterVolume"))
        {
            KPlayerPrefs.SetFloat("MasterVolume", 0.8f);
            KPlayerPrefs.SetInt("GraphicsQuality", 2);
            KPlayerPrefs.SetBool("FullscreenMode", true);
            KPlayerPrefs.SetString("GameLanguage", "English");
            KPlayerPrefs.SetDateTime("InstallDate", DateTime.Now);
        }

        // Profile Data Setup
        if (!KPlayerPrefs.Profiles.Exists("Player1"))
        {
            KPlayerPrefs.Profiles.Create("Player1");
            KPlayerPrefs.Profiles.SetActive("Player1");

            KPlayerPrefs.Profiles.SetString("PlayerName", "Kairosolo");
            KPlayerPrefs.Profiles.SetInt("Level", 1);
            KPlayerPrefs.Profiles.SetFloat("Experience", 0f);
            KPlayerPrefs.Profiles.SetBool("TutorialComplete", false);
            KPlayerPrefs.Profiles.SetVector3("LastPosition", Vector3.zero);
            KPlayerPrefs.Profiles.SetColor("PlayerColor", Color.white);
            KPlayerPrefs.Profiles.SetDateTime("LastPlayed", DateTime.Now);
        }
    }

    #endregion UI Initialization

    #region Profile Management UI

    private void RefreshProfileDropdown()
    {
        if (!profileDropdown) return;

        var profiles = KPlayerPrefs.Profiles.GetAll();
        var activeProfile = KPlayerPrefs.Profiles.ActiveProfile;

        // Temporarily disable the callback to prevent recursive calls
        profileDropdown.onValueChanged.RemoveListener(OnProfileDropdownChanged);

        profileDropdown.options.Clear();
        for (int i = 0; i < profiles.Length; i++)
        {
            profileDropdown.options.Add(new TMP_Dropdown.OptionData(profiles[i]));
        }

        // Set the dropdown value to match the active profile
        var activeIndex = Array.IndexOf(profiles, activeProfile);
        if (activeIndex >= 0)
        {
            profileDropdown.value = activeIndex;
        }
        profileDropdown.RefreshShownValue();

        // Re-enable the callback
        profileDropdown.onValueChanged.AddListener(OnProfileDropdownChanged);

        // Update the active profile label
        if (activeProfileLabel)
        {
            activeProfileLabel.text = $"Active: {activeProfile}";
        }

        DebugLog($"Dropdown refreshed - Active: {activeProfile}, Index: {activeIndex}");
    }

    private void OnProfileDropdownChanged(int index)
    {
        if (!isInitialized) return;

        var profiles = KPlayerPrefs.Profiles.GetAll();
        if (index >= 0 && index < profiles.Length)
        {
            var selectedProfile = profiles[index];
            var currentActive = KPlayerPrefs.Profiles.ActiveProfile;

            // Only switch if it's actually different
            if (selectedProfile != currentActive)
            {
                KPlayerPrefs.Profiles.SetActive(selectedProfile);

                // Update the active profile label immediately
                if (activeProfileLabel)
                {
                    activeProfileLabel.text = $"Active: {selectedProfile}";
                }

                // Refresh all UI elements that depend on profile data
                RefreshProfileDataUI();

                DebugLog($"Switched to profile: {selectedProfile}");

                // Also log to console for debugging
                DebugLog($"Active profile changed to: {selectedProfile}");
            }
        }
    }

    private void CreateNewProfile()
    {
        if (!newProfileInput || string.IsNullOrEmpty(newProfileInput.text)) return;

        var profileName = newProfileInput.text.Trim();
        if (KPlayerPrefs.Profiles.Create(profileName))
        {
            // Switch to the new profile
            KPlayerPrefs.Profiles.SetActive(profileName);

            // Add some initial data
            KPlayerPrefs.Profiles.SetString("PlayerName", profileName);
            KPlayerPrefs.Profiles.SetInt("Level", 1);
            KPlayerPrefs.Profiles.SetFloat("Experience", 0f);
            KPlayerPrefs.Profiles.SetBool("TutorialComplete", false);
            KPlayerPrefs.Profiles.SetDateTime("LastPlayed", DateTime.Now);

            // Clear the input field
            newProfileInput.text = "";

            // Refresh UI to show new profile as active
            RefreshProfileDropdown();
            RefreshProfileDataUI();

            DebugLog($"Profile '{profileName}' created and activated!");
        }
    }

    private void DeleteCurrentProfile()
    {
        var currentProfile = KPlayerPrefs.Profiles.ActiveProfile;
        if (currentProfile == "Default")
        {
            DebugLog("Cannot delete the Default profile");
            return;
        }

        if (KPlayerPrefs.Profiles.Delete(currentProfile))
        {
            RefreshProfileDropdown();
            RefreshProfileDataUI();
            DebugLog($"Profile '{currentProfile}' deleted");
        }
        else
        {
            DebugLog($"Failed to delete profile '{currentProfile}'");
        }
    }

    private void CopyCurrentProfile()
    {
        var currentProfile = KPlayerPrefs.Profiles.ActiveProfile;
        var copyName = $"{currentProfile}_Copy";

        var counter = 1;
        while (KPlayerPrefs.Profiles.Exists(copyName))
        {
            copyName = $"{currentProfile}_Copy{counter}";
            counter++;
        }

        if (KPlayerPrefs.Profiles.Copy(currentProfile, copyName))
        {
            // Switch to the new copied profile
            KPlayerPrefs.Profiles.SetActive(copyName);

            // Refresh UI to show copied profile as active
            RefreshProfileDropdown();
            RefreshProfileDataUI();

            DebugLog($"Profile copied to '{copyName}' and activated!");
        }
        else
        {
            DebugLog($"Failed to copy profile '{currentProfile}'");
        }
    }

    #endregion Profile Management UI

    #region Global Data UI

    private void RefreshGlobalDataUI()
    {
        // Update UI controls with current global data
        if (volumeSlider) volumeSlider.SetValueWithoutNotify(KPlayerPrefs.GetFloat("MasterVolume", 0.8f));
        if (qualityDropdown) qualityDropdown.SetValueWithoutNotify(KPlayerPrefs.GetInt("GraphicsQuality", 2));
        if (fullscreenToggle) fullscreenToggle.SetIsOnWithoutNotify(KPlayerPrefs.GetBool("FullscreenMode", true));
        if (languageInput) languageInput.SetTextWithoutNotify(KPlayerPrefs.GetString("GameLanguage", "English"));

        // Update display text
        if (globalDataDisplay)
        {
            var volume = KPlayerPrefs.GetFloat("MasterVolume", 0.8f);
            var quality = KPlayerPrefs.GetInt("GraphicsQuality", 2);
            var fullscreen = KPlayerPrefs.GetBool("FullscreenMode", true);
            var language = KPlayerPrefs.GetString("GameLanguage", "English");
            var installDate = KPlayerPrefs.GetDateTime("InstallDate", DateTime.Now);

            globalDataDisplay.text = $"Global Data (Shared Across All Profiles):\n" +
                                   $"• Master Volume: {volume:F2}\n" +
                                   $"• Graphics Quality: {GetQualityName(quality)}\n" +
                                   $"• Fullscreen: {(fullscreen ? "Yes" : "No")}\n" +
                                   $"• Language: {language}\n" +
                                   $"• Install Date: {installDate:yyyy/MM/dd}";
        }
    }

    private void OnVolumeChanged(float value)
    {
        if (!isInitialized) return;
        KPlayerPrefs.SetFloat("MasterVolume", value);
        AudioListener.volume = value; // Apply immediately
        RefreshGlobalDataUI();
    }

    private void OnQualityChanged(int value)
    {
        if (!isInitialized) return;
        KPlayerPrefs.SetInt("GraphicsQuality", value);
        QualitySettings.SetQualityLevel(value); // Apply immediately
        RefreshGlobalDataUI();
    }

    private void OnFullscreenChanged(bool value)
    {
        if (!isInitialized) return;
        KPlayerPrefs.SetBool("FullscreenMode", value);
        RefreshGlobalDataUI();
    }

    private void OnLanguageChanged(string value)
    {
        if (!isInitialized) return;
        KPlayerPrefs.SetString("GameLanguage", value);
        RefreshGlobalDataUI();
    }

    private string GetQualityName(int quality)
    {
        switch (quality)
        {
            case 0: return "Low";
            case 1: return "Medium";
            case 2: return "High";
            case 3: return "Ultra";
            default: return "Unknown";
        }
    }

    #endregion Global Data UI

    #region Profile Data UI

    private void RefreshProfileDataUI()
    {
        // Update UI controls with current profile data
        if (playerNameInput) playerNameInput.SetTextWithoutNotify(KPlayerPrefs.Profiles.GetString("PlayerName", "Unknown"));
        if (levelSlider) levelSlider.SetValueWithoutNotify(KPlayerPrefs.Profiles.GetInt("Level", 1));
        if (experienceSlider) experienceSlider.SetValueWithoutNotify(KPlayerPrefs.Profiles.GetFloat("Experience", 0f));
        if (tutorialToggle) tutorialToggle.SetIsOnWithoutNotify(KPlayerPrefs.Profiles.GetBool("TutorialComplete", false));

        // Update display text
        if (profileDataDisplay)
        {
            var activeProfile = KPlayerPrefs.Profiles.ActiveProfile;
            var name = KPlayerPrefs.Profiles.GetString("PlayerName", "Unknown");
            var level = KPlayerPrefs.Profiles.GetInt("Level", 1);
            var exp = KPlayerPrefs.Profiles.GetFloat("Experience", 0f);
            var tutorial = KPlayerPrefs.Profiles.GetBool("TutorialComplete", false);
            var lastPlayed = KPlayerPrefs.Profiles.GetDateTime("LastPlayed", DateTime.Now);
            var position = KPlayerPrefs.Profiles.GetVector3("LastPosition", Vector3.zero);
            var color = KPlayerPrefs.Profiles.GetColor("PlayerColor", Color.white);

            profileDataDisplay.text = $"Profile Data (Specific to '{activeProfile}'):\n" +
                                    $"• Player Name: {name}\n" +
                                    $"• Tutorial Complete: {tutorial}\n" +
                                    $"• Level: {level}\n" +
                                    $"• Experience: {exp:F0}\n" +
                                    $"• Last Position: ({position.x:F1}, {position.y:F1}, {position.z:F1})\n" +
                                    $"• Player Color: RGB({color.r:F2}, {color.g:F2}, {color.b:F2})\n" +
                                    $"• Last Played: {lastPlayed:HH:mm:ss}";
        }

        // Update advanced data display too
        RefreshImportExportStatus();
    }

    private void OnPlayerNameChanged(string value)
    {
        if (!isInitialized) return;
        KPlayerPrefs.Profiles.SetString("PlayerName", value);
        RefreshProfileDataUI();
    }

    private void OnLevelChanged(float value)
    {
        if (!isInitialized) return;
        KPlayerPrefs.Profiles.SetInt("Level", Mathf.RoundToInt(value));
        RefreshProfileDataUI();
    }

    private void OnExperienceChanged(float value)
    {
        if (!isInitialized) return;
        KPlayerPrefs.Profiles.SetFloat("Experience", value);
        RefreshProfileDataUI();
    }

    private void OnTutorialChanged(bool value)
    {
        if (!isInitialized) return;
        KPlayerPrefs.Profiles.SetBool("TutorialComplete", value);
        RefreshProfileDataUI();
    }

    private void LevelUpPlayer()
    {
        var currentLevel = KPlayerPrefs.Profiles.GetInt("Level", 1);
        var newLevel = currentLevel + 1;
        var expGained = UnityEngine.Random.Range(100f, 300f);
        var currentExp = KPlayerPrefs.Profiles.GetFloat("Experience", 0f);

        KPlayerPrefs.Profiles.SetInt("Level", newLevel);
        KPlayerPrefs.Profiles.SetFloat("Experience", currentExp + expGained);
        KPlayerPrefs.Profiles.SetDateTime("LastPlayed", DateTime.Now);

        RefreshProfileDataUI();
    }

    private void GainExperience()
    {
        var expGained = UnityEngine.Random.Range(50f, 150f);
        var currentExp = KPlayerPrefs.Profiles.GetFloat("Experience", 0f);

        KPlayerPrefs.Profiles.SetFloat("Experience", currentExp + expGained);
        KPlayerPrefs.Profiles.SetDateTime("LastPlayed", DateTime.Now);

        RefreshProfileDataUI();
    }

    private void RandomPosition()
    {
        var randomPosition = new Vector3(
            UnityEngine.Random.Range(-50f, 50f),
            UnityEngine.Random.Range(0f, 20f),
            UnityEngine.Random.Range(-50f, 50f)
        );

        KPlayerPrefs.Profiles.SetVector3("LastPosition", randomPosition);
        KPlayerPrefs.Profiles.SetDateTime("LastPlayed", DateTime.Now);

        RefreshProfileDataUI();
    }

    private void RandomColor()
    {
        var randomColor = new Color(
            UnityEngine.Random.value,
            UnityEngine.Random.value,
            UnityEngine.Random.value,
            1f
        );

        KPlayerPrefs.Profiles.SetColor("PlayerColor", randomColor);
        KPlayerPrefs.Profiles.SetDateTime("LastPlayed", DateTime.Now);

        RefreshProfileDataUI();
    }

    #endregion Profile Data UI

    #region Import/Export Status Display

    private void RefreshImportExportStatus()
    {
        if (!importExportStatusDisplay) return;

        string dataPath = Application.persistentDataPath;
        string activeProfile = KPlayerPrefs.Profiles.ActiveProfile;

        // Check for existing export files
        string globalPattern = "GlobalData_Export_*.json";
        string profilePattern = $"ProfileData_{activeProfile}_Export_*.json";

        string[] globalFiles = Directory.GetFiles(dataPath, globalPattern);
        string[] profileFiles = Directory.GetFiles(dataPath, profilePattern);

        string globalStatus = globalFiles.Length > 0 ?
            $"Latest: {Path.GetFileName(globalFiles[globalFiles.Length - 1])}" :
            "No exports found";

        string profileStatus = profileFiles.Length > 0 ?
            $"Latest: {Path.GetFileName(profileFiles[profileFiles.Length - 1])}" :
            "No exports found";

        importExportStatusDisplay.text = $"Import/Export Status:\n" +
                                       $"• Data Location: {dataPath}\n" +
                                       $"• Global Data: {globalStatus}\n" +
                                       $"• Profile '{activeProfile}': {profileStatus}\n\n" +
                                       $"Export creates timestamped files\n" +
                                       $"Import uses most recent file";
    }

    #endregion Import/Export Status Display

    #region Import/Export UI

    private void ExportGlobalData()
    {
        try
        {
            string fileName = $"GlobalData_Export_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filePath = Path.Combine(Application.persistentDataPath, fileName);

            KPlayerPrefs.ExportGlobalData(filePath);
            RefreshImportExportStatus();  // Update status display

            DebugLog($"Global data exported to: {filePath}");
        }
        catch (System.Exception e)
        {
            DebugLog($"Failed to export global data: {e.Message}");
        }
    }

    private void ImportGlobalData()
    {
        try
        {
            // Look for the most recent export file
            string searchPattern = "GlobalData_Export_*.json";
            string[] files = Directory.GetFiles(Application.persistentDataPath, searchPattern);

            if (files.Length > 0)
            {
                // Get the most recent file
                Array.Sort(files);
                string filePath = files[files.Length - 1];

                KPlayerPrefs.ImportGlobalData(filePath, merge: true);
                RefreshGlobalDataUI();
                RefreshImportExportStatus();  // Update status display

                DebugLog($"Global data imported from: {Path.GetFileName(filePath)}");
            }
            else
            {
                DebugLog("No global data export files found. Please export first.");
            }
        }
        catch (System.Exception e)
        {
            DebugLog($"Failed to import global data: {e.Message}");
        }
    }

    private void ExportProfileData()
    {
        try
        {
            string activeProfile = KPlayerPrefs.Profiles.ActiveProfile;
            string fileName = $"ProfileData_{activeProfile}_Export_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filePath = Path.Combine(Application.persistentDataPath, fileName);

            KPlayerPrefs.ExportActiveProfile(filePath);
            RefreshImportExportStatus();  // Update status display

            DebugLog($"Profile '{activeProfile}' exported to: {filePath}");
        }
        catch (System.Exception e)
        {
            DebugLog($"Failed to export profile data: {e.Message}");
        }
    }

    private void ImportProfileData()
    {
        try
        {
            string activeProfile = KPlayerPrefs.Profiles.ActiveProfile;
            string searchPattern = $"ProfileData_{activeProfile}_Export_*.json";
            string[] files = Directory.GetFiles(Application.persistentDataPath, searchPattern);

            if (files.Length > 0)
            {
                // Get the most recent file for this profile
                Array.Sort(files);
                string filePath = files[files.Length - 1];

                KPlayerPrefs.ImportActiveProfile(filePath, merge: true);
                RefreshProfileDataUI();
                RefreshImportExportStatus();  // Update status display

                DebugLog($"✅ Profile '{activeProfile}' imported from: {Path.GetFileName(filePath)}");
            }
            else
            {
                DebugLog($"❌ No export files found for profile '{activeProfile}'. Please export first.");
            }
        }
        catch (System.Exception e)
        {
            DebugLog($"❌ Failed to import profile data: {e.Message}");
        }
    }

    #endregion Import/Export UI

    #region Utility Methods

    private void RefreshAllDisplays()
    {
        RefreshProfileDropdown();
        RefreshGlobalDataUI();
        RefreshProfileDataUI();
        RefreshImportExportStatus();
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[KPlayerPrefs] {message}");
    }

    private void OnValidate()
    {
        // Ensure sliders have reasonable ranges
        if (levelSlider && levelSlider.maxValue < 50) levelSlider.maxValue = 50;
        if (experienceSlider && experienceSlider.maxValue < 5000) experienceSlider.maxValue = 5000;
    }

    #endregion Utility Methods
}