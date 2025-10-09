using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using System.Security.Cryptography;
using System.Linq;

namespace KairosoloSystems
{
    public static class KPlayerPrefs
    {
        // Profile system paths
        private static readonly string DEFAULT_PROFILE_NAME = "Default";

        // Debug Settings
        private static readonly bool ENABLE_DEBUG_LOGS = false;

        private static readonly string PROFILES_ROOT_PATH = Path.Combine(Application.persistentDataPath, "KPlayerPrefs_Profiles");

        private static readonly string GLOBAL_DATA_PATH = Path.Combine(Application.persistentDataPath, "KPlayerPrefs_Global.dat");
        private static readonly string PROFILES_CONFIG_PATH = Path.Combine(Application.persistentDataPath, "KPlayerPrefs_ProfilesConfig.dat");

        // Legacy path for backward compatibility
        private static readonly string LEGACY_SAVE_PATH = Path.Combine(Application.persistentDataPath, "KPlayerPrefs.dat");

        // Encryption settings
        private static readonly string AES_PASSWORD = "KAIROSOLO_SECURE_KEY_2024";

        private static readonly byte[] AES_SALT = { 0x4B, 0x41, 0x49, 0x52, 0x4F, 0x53, 0x4F, 0x4C, 0x4F, 0x53, 0x45, 0x43, 0x55, 0x52, 0x45, 0x21 };

        private static Dictionary<string, object> _globalData = new Dictionary<string, object>();

        private static ProfilesConfig _profilesConfig = new ProfilesConfig();

        private static bool _isLoaded = false;
        private static bool _encryptionEnabled = true;
        private static byte[] _cachedKey = null;

        // Performance tracking
        private static int _saveCount = 0;

        private static int _loadCount = 0;
        private static float _totalSaveTime = 0f;
        private static float _totalLoadTime = 0f;

        // Events
        public static event System.Action OnDataChanged;

        public static event System.Action<string> OnActiveProfileChanged;

        #region Profile Management Classes

        [Serializable]
        public class ProfilesConfig
        {
            public string activeProfile = "Default";
            public List<ProfileInfo> profiles = new List<ProfileInfo>();
            public System.DateTime lastAccessed = System.DateTime.Now;

            public ProfilesConfig()
            {
                // Ensure we always have a default profile
                if (profiles.Count == 0)
                {
                    profiles.Add(new ProfileInfo { name = "Default", creationDate = System.DateTime.Now });
                }
            }
        }

        [Serializable]
        public class ProfileInfo
        {
            public string name;
            public System.DateTime creationDate;
            public System.DateTime lastAccessDate;
            public Dictionary<string, object> metadata = new Dictionary<string, object>();
        }

        #endregion Profile Management Classes

        #region Profile System Public API

        public static class Profiles
        {
            public static ProfileDataAccessor Profile(string profileName)
            {
                if (string.IsNullOrEmpty(profileName))
                    throw new ArgumentException("Profile name cannot be null or empty");

                return GetProfileAccessor(profileName);
            }

            /// <summary>
            /// Gets the currently active profile name
            /// </summary>
            public static string ActiveProfile => _profilesConfig.activeProfile;

            /// <summary>
            /// Creates a new profile
            /// </summary>
            public static bool Create(string profileName)
            {
                EnsureLoaded();

                if (string.IsNullOrEmpty(profileName))
                {
                    Debug.LogError("[KPlayerPrefs] Profile name cannot be null or empty");
                    return false;
                }

                if (Exists(profileName))
                {
                    Debug.LogWarning($"[KPlayerPrefs] Profile '{profileName}' already exists");
                    return false;
                }

                var profileInfo = new ProfileInfo
                {
                    name = profileName,
                    creationDate = System.DateTime.Now,
                    lastAccessDate = System.DateTime.Now
                };

                _profilesConfig.profiles.Add(profileInfo);
                SaveProfilesConfig();

                // Create the profile directory
                var profilePath = GetProfilePath(profileName);
                Directory.CreateDirectory(Path.GetDirectoryName(profilePath));

                DebugLog($"[KPlayerPrefs] Profile '{profileName}' created successfully");
                OnDataChanged?.Invoke();
                return true;
            }

            /// <summary>
            /// Sets the active profile
            /// </summary>
            public static bool SetActive(string profileName)
            {
                EnsureLoaded();

                if (!Exists(profileName))
                {
                    Debug.LogError($"[KPlayerPrefs] Profile '{profileName}' does not exist");
                    return false;
                }

                if (_profilesConfig.activeProfile == profileName)
                    return true; // Already active

                var previousProfile = _profilesConfig.activeProfile;
                _profilesConfig.activeProfile = profileName;

                // Update last access time
                var profileInfo = _profilesConfig.profiles.FirstOrDefault(p => p.name == profileName);
                if (profileInfo != null)
                {
                    profileInfo.lastAccessDate = System.DateTime.Now;
                }

                SaveProfilesConfig();

                DebugLog($"[KPlayerPrefs] Active profile changed from '{previousProfile}' to '{profileName}'");
                OnActiveProfileChanged?.Invoke(profileName);
                OnDataChanged?.Invoke();
                return true;
            }

            /// <summary>
            /// Deletes a profile and all its data
            /// </summary>
            public static bool Delete(string profileName)
            {
                EnsureLoaded();

                if (IsDefaultProfile(profileName))
                {
                    Debug.LogError("[KPlayerPrefs] Cannot delete the Default profile - it is required for system operation");
                    return false;
                }

                if (!Exists(profileName))
                {
                    Debug.LogWarning($"[KPlayerPrefs] Profile '{profileName}' does not exist");
                    return false;
                }

                // If deleting active profile, switch to Default
                if (_profilesConfig.activeProfile == profileName)
                {
                    SetActive("Default");
                }

                // Remove from config
                _profilesConfig.profiles.RemoveAll(p => p.name == profileName);
                SaveProfilesConfig();

                // Delete profile file
                var profilePath = GetProfilePath(profileName);
                try
                {
                    if (File.Exists(profilePath))
                    {
                        File.Delete(profilePath);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[KPlayerPrefs] Failed to delete profile file: {e.Message}");
                }

                DebugLog($"[KPlayerPrefs] Profile '{profileName}' deleted successfully");
                OnDataChanged?.Invoke();
                return true;
            }

            /// <summary>
            /// Checks if a profile exists
            /// </summary>
            public static bool Exists(string profileName)
            {
                EnsureLoaded();
                return _profilesConfig.profiles.Any(p => p.name == profileName);
            }

            /// <summary>
            /// Gets all profile names
            /// </summary>
            public static string[] GetAll()
            {
                EnsureLoaded();
                return _profilesConfig.profiles.Select(p => p.name).ToArray();
            }

            /// <summary>
            /// Gets detailed information about all profiles
            /// </summary>
            public static ProfileInfo[] GetAllInfo()
            {
                EnsureLoaded();
                return _profilesConfig.profiles.ToArray();
            }

            /// <summary>
            /// Gets the total number of existing profiles.
            /// </summary>
            public static int GetCount()
            {
                EnsureLoaded();
                return _profilesConfig.profiles.Count;
            }

            /// <summary>
            /// Gets information about a specific profile
            /// </summary>
            public static ProfileInfo GetInfo(string profileName)
            {
                EnsureLoaded();
                return _profilesConfig.profiles.FirstOrDefault(p => p.name == profileName);
            }

            /// <summary>
            /// Renames a profile
            /// </summary>
            public static bool Rename(string oldName, string newName)
            {
                EnsureLoaded();

                if (oldName == "Default")
                {
                    Debug.LogError("[KPlayerPrefs] Cannot rename the Default profile");
                    return false;
                }

                if (!Exists(oldName))
                {
                    Debug.LogError($"[KPlayerPrefs] Profile '{oldName}' does not exist");
                    return false;
                }

                if (Exists(newName))
                {
                    Debug.LogError($"[KPlayerPrefs] Profile '{newName}' already exists");
                    return false;
                }

                var profileInfo = _profilesConfig.profiles.FirstOrDefault(p => p.name == oldName);
                if (profileInfo != null)
                {
                    // Rename files
                    var oldPath = GetProfilePath(oldName);
                    var newPath = GetProfilePath(newName);

                    try
                    {
                        if (File.Exists(oldPath))
                        {
                            File.Move(oldPath, newPath);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[KPlayerPrefs] Failed to rename profile file: {e.Message}");
                        return false;
                    }

                    // Update config
                    profileInfo.name = newName;

                    // Update active profile reference if needed
                    if (_profilesConfig.activeProfile == oldName)
                    {
                        _profilesConfig.activeProfile = newName;
                    }

                    SaveProfilesConfig();
                    DebugLog($"[KPlayerPrefs] Profile renamed from '{oldName}' to '{newName}'");
                    OnDataChanged?.Invoke();
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Copies a profile to a new profile
            /// </summary>
            public static bool Copy(string sourceProfile, string targetProfile)
            {
                EnsureLoaded();

                if (!Exists(sourceProfile))
                {
                    Debug.LogError($"[KPlayerPrefs] Source profile '{sourceProfile}' does not exist");
                    return false;
                }

                if (Exists(targetProfile))
                {
                    Debug.LogError($"[KPlayerPrefs] Target profile '{targetProfile}' already exists");
                    return false;
                }

                try
                {
                    // Create new profile
                    if (!Create(targetProfile))
                        return false;

                    // Copy data file
                    var sourcePath = GetProfilePath(sourceProfile);
                    var targetPath = GetProfilePath(targetProfile);

                    if (File.Exists(sourcePath))
                    {
                        File.Copy(sourcePath, targetPath, true);
                    }

                    DebugLog($"[KPlayerPrefs] Profile '{sourceProfile}' copied to '{targetProfile}'");
                    OnDataChanged?.Invoke();
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[KPlayerPrefs] Failed to copy profile: {e.Message}");
                    return false;
                }
            }

            /// <summary>
            /// Gets profile metadata
            /// </summary>
            public static Dictionary<string, object> GetMetadata(string profileName)
            {
                EnsureLoaded();
                var profileInfo = _profilesConfig.profiles.FirstOrDefault(p => p.name == profileName);
                return profileInfo?.metadata ?? new Dictionary<string, object>();
            }

            /// <summary>
            /// Sets profile metadata
            /// </summary>
            public static void SetMetadata(string profileName, string key, object value)
            {
                EnsureLoaded();
                var profileInfo = _profilesConfig.profiles.FirstOrDefault(p => p.name == profileName);
                if (profileInfo != null)
                {
                    profileInfo.metadata[key] = value;
                    SaveProfilesConfig();
                }
            }

            #region Profile Data API - Explicit Profile Operations

            /// <summary>
            /// Sets an integer value for the active profile
            /// </summary>
            public static void SetInt(string key, int value)
            {
                SetProfileValue(key, value);
            }

            /// <summary>
            /// Gets an integer value from the active profile
            /// </summary>
            public static int GetInt(string key, int defaultValue = 0)
            {
                var value = GetProfileValue(key);
                return value is int intValue ? intValue : defaultValue;
            }

            /// <summary>
            /// Sets a float value for the active profile
            /// </summary>
            public static void SetFloat(string key, float value)
            {
                SetProfileValue(key, value);
            }

            /// <summary>
            /// Gets a float value from the active profile
            /// </summary>
            public static float GetFloat(string key, float defaultValue = 0f)
            {
                var value = GetProfileValue(key);
                return value is float floatValue ? floatValue : defaultValue;
            }

            /// <summary>
            /// Sets a string value for the active profile
            /// </summary>
            public static void SetString(string key, string value)
            {
                SetProfileValue(key, value ?? "");
            }

            /// <summary>
            /// Gets a string value from the active profile
            /// </summary>
            public static string GetString(string key, string defaultValue = "")
            {
                var value = GetProfileValue(key);
                return value is string stringValue ? stringValue : defaultValue;
            }

            /// <summary>
            /// Sets a boolean value for the active profile
            /// </summary>
            public static void SetBool(string key, bool value)
            {
                SetProfileValue(key, value);
            }

            /// <summary>
            /// Gets a boolean value from the active profile
            /// </summary>
            public static bool GetBool(string key, bool defaultValue = false)
            {
                var value = GetProfileValue(key);
                return value is bool boolValue ? boolValue : defaultValue;
            }

            /// <summary>
            /// Sets a Vector2 value for the active profile
            /// </summary>
            public static void SetVector2(string key, Vector2 value)
            {
                SetProfileValue(key, new SerializableVector2(value));
            }

            /// <summary>
            /// Gets a Vector2 value from the active profile
            /// </summary>
            public static Vector2 GetVector2(string key, Vector2 defaultValue = default)
            {
                var value = GetProfileValue(key);
                return value is SerializableVector2 vec2 ? vec2.ToVector2() : defaultValue;
            }

            /// <summary>
            /// Sets a Vector3 value for the active profile
            /// </summary>
            public static void SetVector3(string key, Vector3 value)
            {
                SetProfileValue(key, new SerializableVector3(value));
            }

            /// <summary>
            /// Gets a Vector3 value from the active profile
            /// </summary>
            public static Vector3 GetVector3(string key, Vector3 defaultValue = default)
            {
                var value = GetProfileValue(key);
                return value is SerializableVector3 vec3 ? vec3.ToVector3() : defaultValue;
            }

            /// <summary>
            /// Sets a Color value for the active profile
            /// </summary>
            public static void SetColor(string key, Color value)
            {
                SetProfileValue(key, new SerializableColor(value));
            }

            /// <summary>
            /// Gets a Color value from the active profile
            /// </summary>
            public static Color GetColor(string key, Color defaultValue = default)
            {
                var value = GetProfileValue(key);
                return value is SerializableColor color ? color.ToColor() : defaultValue;
            }

            /// <summary>
            /// Sets a DateTime value for the active profile
            /// </summary>
            public static void SetDateTime(string key, System.DateTime value)
            {
                SetProfileValue(key, new SerializableDateTime(value));
            }

            /// <summary>
            /// Gets a DateTime value from the active profile
            /// </summary>
            public static System.DateTime GetDateTime(string key, System.DateTime defaultValue = default)
            {
                var value = GetProfileValue(key);
                return value is SerializableDateTime dateTime ? dateTime.ToDateTime() : defaultValue;
            }

            /// <summary>
            /// Checks if a key exists in the active profile
            /// </summary>
            public static bool HasKey(string key)
            {
                var profileData = LoadProfileData(_profilesConfig.activeProfile);
                return profileData.ContainsKey(key);
            }

            /// <summary>
            /// Deletes a key from the active profile
            /// </summary>
            public static void DeleteKey(string key)
            {
                var profileData = LoadProfileData(_profilesConfig.activeProfile);
                if (profileData.Remove(key))
                {
                    SaveProfileData(_profilesConfig.activeProfile, profileData);
                }
            }

            /// <summary>
            /// Deletes multiple keys from the active profile
            /// </summary>
            public static void DeleteKeys(string[] keys)
            {
                var profileData = LoadProfileData(_profilesConfig.activeProfile);
                bool changed = false;

                foreach (var key in keys)
                {
                    if (profileData.Remove(key))
                        changed = true;
                }

                if (changed)
                {
                    SaveProfileData(_profilesConfig.activeProfile, profileData);
                }
            }

            /// <summary>
            /// Deletes all data from the active profile
            /// </summary>
            public static void DeleteAll()
            {
                SaveProfileData(_profilesConfig.activeProfile, new Dictionary<string, object>());
            }

            /// <summary>
            /// Gets all keys from the active profile
            /// </summary>
            public static string[] GetAllKeys()
            {
                var profileData = LoadProfileData(_profilesConfig.activeProfile);
                var keys = new string[profileData.Keys.Count];
                profileData.Keys.CopyTo(keys, 0);
                return keys;
            }

            /// <summary>
            /// Gets all data from the active profile
            /// </summary>
            public static Dictionary<string, object> GetAllData()
            {
                return new Dictionary<string, object>(LoadProfileData(_profilesConfig.activeProfile));
            }

            /// <summary>
            /// Gets the number of keys in the active profile
            /// </summary>
            public static int GetKeyCount()
            {
                return LoadProfileData(_profilesConfig.activeProfile).Count;
            }

            /// <summary>
            /// Direct access to a specific profile's data (without switching active profile)
            /// </summary>
            /// <param name="profileName">Name of the profile to access</param>
            /// <returns>ProfileDataAccessor for the specified profile</returns>
            public static ProfileDataAccessor GetProfileAccessor(string profileName)
            {
                return new ProfileDataAccessor(profileName);
            }

            #region Profile Data Helpers

            private static void SetProfileValue(string key, object value)
            {
                EnsureLoaded();
                var profileData = LoadProfileData(_profilesConfig.activeProfile);
                profileData[key] = value;
                SaveProfileData(_profilesConfig.activeProfile, profileData);
            }

            private static object GetProfileValue(string key)
            {
                EnsureLoaded();
                var profileData = LoadProfileData(_profilesConfig.activeProfile);
                return profileData.TryGetValue(key, out var value) ? value : null;
            }

            #endregion Profile Data Helpers

            #endregion Profile Data API - Explicit Profile Operations
        }

        #endregion Profile System Public API

        #region Helper Methods

        private static bool IsDefaultProfile(string profileName)
        {
            return string.Equals(profileName, DEFAULT_PROFILE_NAME, StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureDefaultProfileExists()
        {
            if (_profilesConfig.profiles == null || !_profilesConfig.profiles.Any(p => IsDefaultProfile(p.name)))
            {
                Debug.LogWarning("[KPlayerPrefs] Default profile missing - recreating it");

                if (_profilesConfig.profiles == null)
                    _profilesConfig.profiles = new List<ProfileInfo>();

                var defaultProfile = new ProfileInfo
                {
                    name = DEFAULT_PROFILE_NAME,
                    creationDate = System.DateTime.Now,
                    lastAccessDate = System.DateTime.Now
                };

                _profilesConfig.profiles.Insert(0, defaultProfile);

                if (string.IsNullOrEmpty(_profilesConfig.activeProfile) ||
                    !_profilesConfig.profiles.Any(p => p.name == _profilesConfig.activeProfile))
                {
                    _profilesConfig.activeProfile = DEFAULT_PROFILE_NAME;
                }

                SaveProfilesConfig();
            }
        }

        private static void DebugLog(string message)
        {
            if (ENABLE_DEBUG_LOGS)
                Debug.Log(message);
        }

        #endregion Helper Methods

        #region ProfileDataAccessor

        public class ProfileDataAccessor
        {
            private readonly string _profileName;

            internal ProfileDataAccessor(string profileName)
            {
                _profileName = profileName;
            }

            public void SetInt(string key, int value)
            {
                SetValue(key, value);
            }

            public int GetInt(string key, int defaultValue = 0)
            {
                var value = GetValue(key);
                return value is int intValue ? intValue : defaultValue;
            }

            public void SetFloat(string key, float value)
            {
                SetValue(key, value);
            }

            public float GetFloat(string key, float defaultValue = 0f)
            {
                var value = GetValue(key);
                return value is float floatValue ? floatValue : defaultValue;
            }

            public void SetString(string key, string value)
            {
                SetValue(key, value ?? "");
            }

            public string GetString(string key, string defaultValue = "")
            {
                var value = GetValue(key);
                return value is string stringValue ? stringValue : defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                SetValue(key, value);
            }

            public bool GetBool(string key, bool defaultValue = false)
            {
                var value = GetValue(key);
                return value is bool boolValue ? boolValue : defaultValue;
            }

            public void SetVector2(string key, Vector2 value)
            {
                SetValue(key, new SerializableVector2(value));
            }

            public Vector2 GetVector2(string key, Vector2 defaultValue = default)
            {
                var value = GetValue(key);
                return value is SerializableVector2 vec2 ? vec2.ToVector2() : defaultValue;
            }

            public void SetVector3(string key, Vector3 value)
            {
                SetValue(key, new SerializableVector3(value));
            }

            public Vector3 GetVector3(string key, Vector3 defaultValue = default)
            {
                var value = GetValue(key);
                return value is SerializableVector3 vec3 ? vec3.ToVector3() : defaultValue;
            }

            public void SetColor(string key, Color value)
            {
                SetValue(key, new SerializableColor(value));
            }

            public Color GetColor(string key, Color defaultValue = default)
            {
                var value = GetValue(key);
                return value is SerializableColor color ? color.ToColor() : defaultValue;
            }

            public void SetDateTime(string key, System.DateTime value)
            {
                SetValue(key, new SerializableDateTime(value));
            }

            public System.DateTime GetDateTime(string key, System.DateTime defaultValue = default)
            {
                var value = GetValue(key);
                return value is SerializableDateTime dateTime ? dateTime.ToDateTime() : defaultValue;
            }

            private void SetValue(string key, object value)
            {
                EnsureLoaded();

                if (!Profiles.Exists(_profileName))
                {
                    Debug.LogError($"[KPlayerPrefs] Profile '{_profileName}' does not exist");
                    return;
                }

                var profileData = LoadProfileData(_profileName);
                profileData[key] = value;
                SaveProfileData(_profileName, profileData);

                OnDataChanged?.Invoke();
            }

            private object GetValue(string key)
            {
                EnsureLoaded();

                if (!Profiles.Exists(_profileName))
                {
                    Debug.LogError($"[KPlayerPrefs] Profile '{_profileName}' does not exist");
                    return null;
                }

                var profileData = LoadProfileData(_profileName);
                return profileData.TryGetValue(key, out var profileValue) ? profileValue : null;
            }
        }

        #endregion ProfileDataAccessor

        #region Global Data API

        /// <summary>
        /// Sets an integer value in global data (shared across all profiles)
        /// </summary>
        public static void SetInt(string key, int value)
        {
            EnsureLoaded();
            _globalData[key] = value;
            SaveGlobalData();
        }

        /// <summary>
        /// Gets an integer value from global data
        /// </summary>
        public static int GetInt(string key, int defaultValue = 0)
        {
            EnsureLoaded();
            return _globalData.TryGetValue(key, out var value) && value is int intValue ? intValue : defaultValue;
        }

        /// <summary>
        /// Sets a float value in global data (shared across all profiles)
        /// </summary>
        public static void SetFloat(string key, float value)
        {
            EnsureLoaded();
            _globalData[key] = value;
            SaveGlobalData();
        }

        /// <summary>
        /// Gets a float value from global data
        /// </summary>
        public static float GetFloat(string key, float defaultValue = 0f)
        {
            EnsureLoaded();
            return _globalData.TryGetValue(key, out var value) && value is float floatValue ? floatValue : defaultValue;
        }

        /// <summary>
        /// Sets a string value in global data (shared across all profiles)
        /// </summary>
        public static void SetString(string key, string value)
        {
            EnsureLoaded();
            _globalData[key] = value ?? "";
            SaveGlobalData();
        }

        /// <summary>
        /// Gets a string value from global data
        /// </summary>
        public static string GetString(string key, string defaultValue = "")
        {
            EnsureLoaded();
            return _globalData.TryGetValue(key, out var value) && value is string stringValue ? stringValue : defaultValue;
        }

        /// <summary>
        /// Sets a boolean value in global data (shared across all profiles)
        /// </summary>
        public static void SetBool(string key, bool value)
        {
            EnsureLoaded();
            _globalData[key] = value;
            SaveGlobalData();
        }

        /// <summary>
        /// Gets a boolean value from global data
        /// </summary>
        public static bool GetBool(string key, bool defaultValue = false)
        {
            EnsureLoaded();
            return _globalData.TryGetValue(key, out var value) && value is bool boolValue ? boolValue : defaultValue;
        }

        /// <summary>
        /// Sets a Vector2 value in global data (shared across all profiles)
        /// </summary>
        public static void SetVector2(string key, Vector2 value)
        {
            EnsureLoaded();
            _globalData[key] = new SerializableVector2(value);
            SaveGlobalData();
        }

        /// <summary>
        /// Gets a Vector2 value from global data
        /// </summary>
        public static Vector2 GetVector2(string key, Vector2 defaultValue = default)
        {
            EnsureLoaded();
            return _globalData.TryGetValue(key, out var value) && value is SerializableVector2 vec2 ? vec2.ToVector2() : defaultValue;
        }

        /// <summary>
        /// Sets a Vector3 value in global data (shared across all profiles)
        /// </summary>
        public static void SetVector3(string key, Vector3 value)
        {
            EnsureLoaded();
            _globalData[key] = new SerializableVector3(value);
            SaveGlobalData();
        }

        /// <summary>
        /// Gets a Vector3 value from global data
        /// </summary>
        public static Vector3 GetVector3(string key, Vector3 defaultValue = default)
        {
            EnsureLoaded();
            return _globalData.TryGetValue(key, out var value) && value is SerializableVector3 vec3 ? vec3.ToVector3() : defaultValue;
        }

        /// <summary>
        /// Sets a Color value in global data (shared across all profiles)
        /// </summary>
        public static void SetColor(string key, Color value)
        {
            EnsureLoaded();
            _globalData[key] = new SerializableColor(value);
            SaveGlobalData();
        }

        /// <summary>
        /// Gets a Color value from global data
        /// </summary>
        public static Color GetColor(string key, Color defaultValue = default)
        {
            EnsureLoaded();
            return _globalData.TryGetValue(key, out var value) && value is SerializableColor color ? color.ToColor() : defaultValue;
        }

        /// <summary>
        /// Sets a DateTime value in global data (shared across all profiles)
        /// </summary>
        public static void SetDateTime(string key, System.DateTime value)
        {
            EnsureLoaded();
            _globalData[key] = new SerializableDateTime(value);
            SaveGlobalData();
        }

        /// <summary>
        /// Gets a DateTime value from global data
        /// </summary>
        public static System.DateTime GetDateTime(string key, System.DateTime defaultValue = default)
        {
            EnsureLoaded();
            return _globalData.TryGetValue(key, out var value) && value is SerializableDateTime dateTime ? dateTime.ToDateTime() : defaultValue;
        }

        /// <summary>
        /// Checks if a key exists in global data
        /// </summary>
        public static bool HasKey(string key)
        {
            EnsureLoaded();
            return _globalData.ContainsKey(key);
        }

        /// <summary>
        /// Deletes a key from global data
        /// </summary>
        public static void DeleteKey(string key)
        {
            EnsureLoaded();
            if (_globalData.Remove(key))
                SaveGlobalData();
        }

        /// <summary>
        /// Deletes all global data
        /// </summary>
        public static void DeleteAll()
        {
            EnsureLoaded();
            _globalData.Clear();
            SaveGlobalData();
        }

        /// <summary>
        /// Gets all keys from global data
        /// </summary>
        public static string[] GetAllKeys()
        {
            EnsureLoaded();
            var keys = new string[_globalData.Keys.Count];
            _globalData.Keys.CopyTo(keys, 0);
            return keys;
        }

        /// <summary>
        /// Gets all global data
        /// </summary>
        public static Dictionary<string, object> GetAllData()
        {
            EnsureLoaded();
            return new Dictionary<string, object>(_globalData);
        }

        /// <summary>
        /// Manually saves global data
        /// </summary>
        public static void Save()
        {
            SaveGlobalData();
        }

        #endregion Global Data API

        #region Initialization and Core System

        static KPlayerPrefs()
        {
            LoadData();
        }

        private static void EnsureLoaded()
        {
            if (!_isLoaded)
                LoadData();
        }

        private static void LoadData()
        {
            var startTime = Time.realtimeSinceStartup;

            try
            {
                // Create directories if they don't exist
                Directory.CreateDirectory(PROFILES_ROOT_PATH);

                // Handle legacy migration
                MigrateLegacyData();

                // Load profiles configuration
                LoadProfilesConfig();

                // Ensure Default profile always exists
                EnsureDefaultProfileExists();

                // Load global data
                LoadGlobalData();

                _loadCount++;
                _totalLoadTime += Time.realtimeSinceStartup - startTime;

                DebugLog($"[KPlayerPrefs] Multi-profile system loaded. Active profile: {_profilesConfig.activeProfile}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[KPlayerPrefs] Failed to load profile system: {e.Message}");
                // Initialize with defaults
                _profilesConfig = new ProfilesConfig();
                _globalData = new Dictionary<string, object>();
            }
            finally
            {
                _isLoaded = true;
                OnDataChanged?.Invoke();
            }
        }

        private static void MigrateLegacyData()
        {
            // Check if legacy file exists and profiles config doesn't
            if (File.Exists(LEGACY_SAVE_PATH) && !File.Exists(PROFILES_CONFIG_PATH))
            {
                Debug.Log("[KPlayerPrefs] Migrating legacy data to profile system...");

                try
                {
                    // Load legacy data
                    var encrypted = File.ReadAllBytes(LEGACY_SAVE_PATH);
                    var decrypted = AESDecrypt(encrypted);
                    var json = Encoding.UTF8.GetString(decrypted);
                    var legacyData = JsonUtility.FromJson<SerializableData>(json);

                    // Save as Default profile
                    var defaultProfilePath = GetProfilePath("Default");
                    Directory.CreateDirectory(Path.GetDirectoryName(defaultProfilePath));
                    File.Copy(LEGACY_SAVE_PATH, defaultProfilePath);

                    // Create backup of legacy file
                    var backupPath = LEGACY_SAVE_PATH + ".migrated_backup";
                    File.Copy(LEGACY_SAVE_PATH, backupPath);

                    DebugLog("[KPlayerPrefs] Legacy data migrated successfully");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[KPlayerPrefs] Failed to migrate legacy data: {e.Message}");
                }
            }
        }

        #endregion Initialization and Core System

        #region File Path Helpers

        private static string GetProfilePath(string profileName)
        {
            return Path.Combine(PROFILES_ROOT_PATH, $"{profileName}.dat");
        }

        #endregion File Path Helpers

        #region Save/Load Operations

        private static void LoadProfilesConfig()
        {
            try
            {
                if (File.Exists(PROFILES_CONFIG_PATH))
                {
                    var encrypted = File.ReadAllBytes(PROFILES_CONFIG_PATH);
                    var decrypted = AESDecrypt(encrypted);
                    var json = Encoding.UTF8.GetString(decrypted);
                    _profilesConfig = JsonUtility.FromJson<ProfilesConfig>(json) ?? new ProfilesConfig();
                }
                else
                {
                    _profilesConfig = new ProfilesConfig();
                    SaveProfilesConfig();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[KPlayerPrefs] Failed to load profiles config: {e.Message}");
                _profilesConfig = new ProfilesConfig();
            }
        }

        private static void SaveProfilesConfig()
        {
            try
            {
                _profilesConfig.lastAccessed = System.DateTime.Now;
                var json = JsonUtility.ToJson(_profilesConfig);
                var data = Encoding.UTF8.GetBytes(json);
                var encrypted = AESEncrypt(data);
                File.WriteAllBytes(PROFILES_CONFIG_PATH, encrypted);
            }
            catch (Exception e)
            {
                Debug.LogError($"[KPlayerPrefs] Failed to save profiles config: {e.Message}");
            }
        }

        private static void LoadGlobalData()
        {
            try
            {
                if (File.Exists(GLOBAL_DATA_PATH))
                {
                    var encrypted = File.ReadAllBytes(GLOBAL_DATA_PATH);
                    var decrypted = AESDecrypt(encrypted);
                    var json = Encoding.UTF8.GetString(decrypted);
                    var data = JsonUtility.FromJson<SerializableData>(json);
                    _globalData = data.ToDictionary();
                }
                else
                {
                    _globalData = new Dictionary<string, object>();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[KPlayerPrefs] Failed to load global data: {e.Message}");
                _globalData = new Dictionary<string, object>();
            }
        }

        private static void SaveGlobalData()
        {
            try
            {
                var json = JsonUtility.ToJson(new SerializableData(_globalData));
                var data = Encoding.UTF8.GetBytes(json);
                var encrypted = AESEncrypt(data);
                File.WriteAllBytes(GLOBAL_DATA_PATH, encrypted);
                OnDataChanged?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[KPlayerPrefs] Failed to save global data: {e.Message}");
            }
        }

        private static Dictionary<string, object> LoadProfileData(string profileName)
        {
            try
            {
                var profilePath = GetProfilePath(profileName);
                if (File.Exists(profilePath))
                {
                    var encrypted = File.ReadAllBytes(profilePath);
                    var decrypted = AESDecrypt(encrypted);
                    var json = Encoding.UTF8.GetString(decrypted);
                    var data = JsonUtility.FromJson<SerializableData>(json);
                    return data.ToDictionary();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[KPlayerPrefs] Failed to load profile '{profileName}': {e.Message}");
            }

            return new Dictionary<string, object>();
        }

        private static void SaveProfileData(string profileName, Dictionary<string, object> data)
        {
            var startTime = Time.realtimeSinceStartup;

            try
            {
                var profilePath = GetProfilePath(profileName);
                Directory.CreateDirectory(Path.GetDirectoryName(profilePath));

                var json = JsonUtility.ToJson(new SerializableData(data));
                var bytes = Encoding.UTF8.GetBytes(json);
                var encrypted = AESEncrypt(bytes);
                File.WriteAllBytes(profilePath, encrypted);

                _saveCount++;
                _totalSaveTime += Time.realtimeSinceStartup - startTime;

                OnDataChanged?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[KPlayerPrefs] Failed to save profile '{profileName}': {e.Message}");
            }
        }

        #endregion Save/Load Operations

        #region Encryption

        private static void EnsureKeysGenerated()
        {
            if (_cachedKey == null)
            {
                using (var keyDerivation = new Rfc2898DeriveBytes(AES_PASSWORD, AES_SALT, 10000))
                {
                    _cachedKey = keyDerivation.GetBytes(32);
                }
                DebugLog("[KPlayerPrefs] AES keys generated and cached");
            }
        }

        private static byte[] AESEncrypt(byte[] data)
        {
            if (!_encryptionEnabled)
                return data;

            try
            {
                EnsureKeysGenerated();

                using (var aes = Aes.Create())
                {
                    aes.Key = _cachedKey;
                    aes.GenerateIV();
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
                        var result = new byte[16 + encrypted.Length];
                        Array.Copy(aes.IV, 0, result, 0, 16);
                        Array.Copy(encrypted, 0, result, 16, encrypted.Length);
                        return result;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[KPlayerPrefs] AES Encryption failed: {e.Message}");
                throw;
            }
        }

        private static byte[] AESDecrypt(byte[] encryptedData)
        {
            if (!_encryptionEnabled)
                return encryptedData;

            try
            {
                if (encryptedData == null || encryptedData.Length < 16)
                {
                    throw new ArgumentException("Invalid encrypted data - too short or null");
                }

                EnsureKeysGenerated();

                using (var aes = Aes.Create())
                {
                    aes.Key = _cachedKey;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    var iv = new byte[16];
                    Array.Copy(encryptedData, 0, iv, 0, 16);
                    aes.IV = iv;

                    if (encryptedData.Length <= 16)
                    {
                        throw new ArgumentException("Invalid encrypted data - no payload after IV");
                    }

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        var encrypted = new byte[encryptedData.Length - 16];
                        Array.Copy(encryptedData, 16, encrypted, 0, encrypted.Length);
                        return decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[KPlayerPrefs] AES Decryption failed: {e.Message}");
                throw;
            }
        }

        #endregion Encryption

        #region Data Export/Import

        /// <summary>
        /// Exports active profile data to a file
        /// </summary>
        public static void ExportActiveProfile(string filePath)
        {
            try
            {
                EnsureLoaded();
                var profileData = LoadProfileData(_profilesConfig.activeProfile);
                var json = JsonUtility.ToJson(new SerializableData(profileData), true);
                File.WriteAllText(filePath, json);
                DebugLog($"[KPlayerPrefs] Active profile data exported to: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[KPlayerPrefs] Export failed: {e.Message}");
            }
        }

        /// <summary>
        /// Imports data into the active profile
        /// </summary>
        public static void ImportActiveProfile(string filePath, bool merge = false)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[KPlayerPrefs] Import file not found: {filePath}");
                    return;
                }

                var json = File.ReadAllText(filePath);
                var importedData = JsonUtility.FromJson<SerializableData>(json);

                var profileData = merge ? LoadProfileData(_profilesConfig.activeProfile) : new Dictionary<string, object>();

                foreach (var kvp in importedData.ToDictionary())
                {
                    profileData[kvp.Key] = kvp.Value;
                }

                SaveProfileData(_profilesConfig.activeProfile, profileData);
                DebugLog($"[KPlayerPrefs] Data imported to active profile from: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[KPlayerPrefs] Import failed: {e.Message}");
            }
        }

        /// <summary>
        /// Exports a specific profile to a file
        /// </summary>
        public static void ExportProfile(string profileName, string filePath)
        {
            try
            {
                EnsureLoaded();
                if (!Profiles.Exists(profileName))
                {
                    Debug.LogError($"[KPlayerPrefs] Profile '{profileName}' does not exist");
                    return;
                }

                var profileData = LoadProfileData(profileName);
                var json = JsonUtility.ToJson(new SerializableData(profileData), true);
                File.WriteAllText(filePath, json);
                DebugLog($"[KPlayerPrefs] Profile '{profileName}' exported to: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[KPlayerPrefs] Profile export failed: {e.Message}");
            }
        }

        /// <summary>
        /// Imports data into a specific profile
        /// </summary>
        public static void ImportProfile(string profileName, string filePath, bool merge = false)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[KPlayerPrefs] Import file not found: {filePath}");
                    return;
                }

                if (!Profiles.Exists(profileName))
                {
                    Debug.LogError($"[KPlayerPrefs] Profile '{profileName}' does not exist");
                    return;
                }

                var json = File.ReadAllText(filePath);
                var importedData = JsonUtility.FromJson<SerializableData>(json).ToDictionary();

                var profileData = merge ? LoadProfileData(profileName) : new Dictionary<string, object>();

                foreach (var kvp in importedData)
                {
                    profileData[kvp.Key] = kvp.Value;
                }

                SaveProfileData(profileName, profileData);
                DebugLog($"[KPlayerPrefs] Data imported into profile '{profileName}' from: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[KPlayerPrefs] Profile import failed: {e.Message}");
            }
        }

        /// <summary>
        /// Exports global data to a file
        /// </summary>
        public static void ExportGlobalData(string filePath)
        {
            try
            {
                EnsureLoaded();
                var json = JsonUtility.ToJson(new SerializableData(_globalData), true);
                File.WriteAllText(filePath, json);
                DebugLog($"[KPlayerPrefs] Global data exported to: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[KPlayerPrefs] Global data export failed: {e.Message}");
            }
        }

        /// <summary>
        /// Imports global data from a file
        /// </summary>
        public static void ImportGlobalData(string filePath, bool merge = false)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[KPlayerPrefs] Import file not found: {filePath}");
                    return;
                }

                var json = File.ReadAllText(filePath);
                var importedData = JsonUtility.FromJson<SerializableData>(json);

                if (!merge)
                    _globalData.Clear();

                foreach (var kvp in importedData.ToDictionary())
                {
                    _globalData[kvp.Key] = kvp.Value;
                }

                SaveGlobalData();
                DebugLog($"[KPlayerPrefs] Global data imported from: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[KPlayerPrefs] Global data import failed: {e.Message}");
            }
        }

        #endregion Data Export/Import

        #region Performance Stats

        public struct PerformanceStats
        {
            public int SaveCount;
            public int LoadCount;
            public float AverageSaveTime;
            public float AverageLoadTime;
            public int ActiveProfileItemCount;
            public int GlobalItemCount;
            public int TotalProfiles;
            public string ActiveProfile;
        }

        public static PerformanceStats GetPerformanceStats()
        {
            EnsureLoaded();
            var activeProfileData = LoadProfileData(_profilesConfig.activeProfile);
            return new PerformanceStats
            {
                SaveCount = _saveCount,
                LoadCount = _loadCount,
                AverageSaveTime = _saveCount > 0 ? _totalSaveTime / _saveCount : 0f,
                AverageLoadTime = _loadCount > 0 ? _totalLoadTime / _loadCount : 0f,
                ActiveProfileItemCount = activeProfileData.Count,
                GlobalItemCount = _globalData.Count,
                TotalProfiles = _profilesConfig.profiles.Count,
                ActiveProfile = _profilesConfig.activeProfile
            };
        }

        #endregion Performance Stats
    }

    #region Serializable Data Classes

    [Serializable]
    public class SerializableData
    {
        public List<string> keys = new List<string>();
        public List<string> values = new List<string>();
        public List<string> types = new List<string>();

        public SerializableData()
        { }

        public SerializableData(Dictionary<string, object> data)
        {
            foreach (var kvp in data)
            {
                keys.Add(kvp.Key);
                types.Add(kvp.Value.GetType().Name);

                if (kvp.Value is int intVal)
                    values.Add(intVal.ToString());
                else if (kvp.Value is float floatVal)
                    values.Add(floatVal.ToString());
                else if (kvp.Value is bool boolVal)
                    values.Add(boolVal.ToString());
                else if (kvp.Value is string stringVal)
                    values.Add(stringVal);
                else if (kvp.Value is SerializableVector2 vec2)
                    values.Add(JsonUtility.ToJson(vec2));
                else if (kvp.Value is SerializableVector3 vec3)
                    values.Add(JsonUtility.ToJson(vec3));
                else if (kvp.Value is SerializableColor color)
                    values.Add(JsonUtility.ToJson(color));
                else if (kvp.Value is SerializableDateTime dateTime)
                    values.Add(JsonUtility.ToJson(dateTime));
                else
                    values.Add(kvp.Value.ToString());
            }
        }

        public Dictionary<string, object> ToDictionary()
        {
            var result = new Dictionary<string, object>();
            for (int i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                var type = types[i];
                var value = values[i];

                switch (type)
                {
                    case "Int32":
                        if (int.TryParse(value, out int intVal))
                            result[key] = intVal;
                        break;

                    case "Single":
                        if (float.TryParse(value, out float floatVal))
                            result[key] = floatVal;
                        break;

                    case "Boolean":
                        if (bool.TryParse(value, out bool boolVal))
                            result[key] = boolVal;
                        break;

                    case "String":
                        result[key] = value;
                        break;

                    case "SerializableVector2":
                        try
                        {
                            result[key] = JsonUtility.FromJson<SerializableVector2>(value);
                        }
                        catch
                        {
                            result[key] = new SerializableVector2();
                        }
                        break;

                    case "SerializableVector3":
                        try
                        {
                            result[key] = JsonUtility.FromJson<SerializableVector3>(value);
                        }
                        catch
                        {
                            result[key] = new SerializableVector3();
                        }
                        break;

                    case "SerializableColor":
                        try
                        {
                            result[key] = JsonUtility.FromJson<SerializableColor>(value);
                        }
                        catch
                        {
                            result[key] = new SerializableColor();
                        }
                        break;

                    case "SerializableDateTime":
                        try
                        {
                            result[key] = JsonUtility.FromJson<SerializableDateTime>(value);
                        }
                        catch
                        {
                            result[key] = new SerializableDateTime();
                        }
                        break;

                    default:
                        result[key] = value;
                        break;
                }
            }
            return result;
        }
    }

    [Serializable]
    public class SerializableVector2
    {
        public float x, y;

        public SerializableVector2()
        { }

        public SerializableVector2(Vector2 v)
        { x = v.x; y = v.y; }

        public Vector2 ToVector2() => new Vector2(x, y);
    }

    [Serializable]
    public class SerializableVector3
    {
        public float x, y, z;

        public SerializableVector3()
        { }

        public SerializableVector3(Vector3 v)
        { x = v.x; y = v.y; z = v.z; }

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [Serializable]
    public class SerializableColor
    {
        public float r, g, b, a;

        public SerializableColor()
        { }

        public SerializableColor(Color c)
        { r = c.r; g = c.g; b = c.b; a = c.a; }

        public Color ToColor() => new Color(r, g, b, a);
    }

    [Serializable]
    public class SerializableDateTime
    {
        public long ticks;

        public SerializableDateTime()
        { }

        public SerializableDateTime(System.DateTime dateTime)
        { ticks = dateTime.Ticks; }

        public System.DateTime ToDateTime() => new System.DateTime(ticks);
    }

    #endregion Serializable Data Classes
}