#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KairosoloSystems
{
    public class KPlayerPrefsEditor : EditorWindow
    {
        private Vector2 _scrollPosition;
        private string _searchFilter = "";
        private string _newKey = "";
        private object _newValue = null;
        private DataType _newValueType = DataType.String;
        private bool _showAddNew = false;
        private List<string> _selectedKeys = new List<string>();
        private Dictionary<string, object> _editingValues = new Dictionary<string, object>();
        private Dictionary<string, object> _cachedGlobalData = new Dictionary<string, object>();
        private Dictionary<string, object> _cachedProfileData = new Dictionary<string, object>();
        private bool _showPerformancePanel = false;
        private bool _autoUpdateEnabled = true;
        private bool _showProfileManagement = true;

        // Sorting
        private SortMode _sortMode = SortMode.AlphabeticalAZ;

        private enum SortMode
        {
            [InspectorName("A-Z")]
            AlphabeticalAZ,

            [InspectorName("Z-A")]
            AlphabeticalZA,

            [InspectorName("Data Type")]
            DataType
        }

        // Profile management
        private string _newProfileName = "";

        private enum DataType
        {
            Int,
            Float,
            String,
            Bool,
            Vector2,
            Vector3,
            Color,
            DateTime
        }

        private enum DataScope
        {
            Global,      // KPlayerPrefs.SetX() - Global data (shared)
            Profile      // KPlayerPrefs.Profiles.SetX() - Profile data (per-profile)
        }

        private DataScope _currentScope = DataScope.Global;

        [MenuItem("Tools/KPlayerPrefs/KPlayerPrefs Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<KPlayerPrefsEditor>("KPlayerPrefs Editor");
            window.minSize = new Vector2(1000, 750);
        }

        [MenuItem("Tools/KPlayerPrefs/Open Data Folder")]
        public static void OpenDataFolder()
        {
            string dataPath = Application.persistentDataPath;

            try
            {
                // Cross-platform folder opening
#if UNITY_EDITOR_WIN
                System.Diagnostics.Process.Start("explorer.exe", dataPath.Replace('/', '\\'));
#elif UNITY_EDITOR_OSX
            System.Diagnostics.Process.Start("open", dataPath);
#elif UNITY_EDITOR_LINUX
            System.Diagnostics.Process.Start("xdg-open", dataPath);
#endif

                Debug.Log($"[KPlayerPrefs] Data folder opened: {dataPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[KPlayerPrefs] Failed to open data folder: {e.Message}");
                EditorUtility.DisplayDialog("Open Data Folder",
                    $"Could not open folder automatically.\n\nData Path:\n{dataPath}", "OK");
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawScopeTabs();
            DrawPerformancePanel();

            // Only show profile management when in Profile scope
            if (_currentScope == DataScope.Profile)
            {
                DrawProfileManagement();
            }

            DrawToolbar();
            DrawDataList();
            DrawAddNewSection();
            HandleKeyboardInput();
        }

        private void OnEnable()
        {
            RefreshCache();
            RefreshProfilesList();

            if (_autoUpdateEnabled)
            {
                KPlayerPrefs.OnDataChanged += OnDataChanged;
                KPlayerPrefs.OnActiveProfileChanged += OnActiveProfileChanged;
            }
        }

        private void OnDisable()
        {
            KPlayerPrefs.OnDataChanged -= OnDataChanged;
            KPlayerPrefs.OnActiveProfileChanged -= OnActiveProfileChanged;
        }

        private void OnFocus()
        {
            RefreshCache();
            RefreshProfilesList();
            Repaint();
        }

        private void OnDataChanged()
        {
            if (_autoUpdateEnabled)
            {
                RefreshCache();
                Repaint();
            }
        }

        private void OnActiveProfileChanged(string newProfile)
        {
            if (_autoUpdateEnabled)
            {
                RefreshCache();
                RefreshProfilesList();
                Repaint();
            }
        }

        private void RefreshCache()
        {
            try
            {
                // ✅ Global data access is simple
                _cachedGlobalData = KPlayerPrefs.GetAllData();

                // ✅ Profile data access is explicit
                _cachedProfileData = KPlayerPrefs.Profiles.GetAllData();
            }
            catch (Exception e)
            {
                Debug.LogError($"[KPlayerPrefs Editor] Error refreshing cache: {e.Message}");
                _cachedGlobalData = new Dictionary<string, object>();
                _cachedProfileData = new Dictionary<string, object>();
            }
        }

        private void RefreshProfilesList()
        {
            var activeProfile = KPlayerPrefs.Profiles.ActiveProfile;
            var allProfiles = KPlayerPrefs.Profiles.GetAll();
        }

#region GUI Drawing Methods

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                var titleStyle = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                EditorGUILayout.LabelField("KPlayerPrefs Editor", titleStyle);
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(5);

            // Current stats
            using (new GUILayout.HorizontalScope())
            {
                var currentData = GetCurrentData();
                var allKeys = GetAllKeys(currentData);
                var filteredKeys = GetFilteredKeys(allKeys);

                string scopeDescription;
                if (_currentScope == DataScope.Global)
                {
                    scopeDescription = "Global Data";
                }
                else
                {
                    var activeProfile = KPlayerPrefs.Profiles.ActiveProfile;
                    var activeProfileInfo = KPlayerPrefs.Profiles.GetInfo(activeProfile);

                    scopeDescription = $"Profile Data ({activeProfile})";
                }

                EditorGUILayout.LabelField($"{scopeDescription} | Total: {allKeys.Length} | Showing: {filteredKeys.Length} | Selected: {_selectedKeys.Count}",
                    EditorStyles.miniLabel, GUILayout.MinWidth(400));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("⚡", EditorStyles.miniButton, GUILayout.Width(25)))
                {
                    _showPerformancePanel = !_showPerformancePanel;
                }

                // Auto-update controls
                EditorGUILayout.LabelField("Auto-Update:", GUILayout.Width(80));
                var newAutoUpdate = EditorGUILayout.Toggle(_autoUpdateEnabled, GUILayout.Width(20));
                if (newAutoUpdate != _autoUpdateEnabled)
                {
                    ToggleAutoUpdate(newAutoUpdate);
                }

                // Status indicator - reduced width
                if (_autoUpdateEnabled)
                {
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField("● Live", GUILayout.Width(50));
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = Color.gray;
                    EditorGUILayout.LabelField("● Manual", GUILayout.Width(60));
                    GUI.color = Color.white;
                }
            }

            EditorGUILayout.Space(10);
            DrawSeparator();
        }

        private void DrawScopeTabs()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUI.backgroundColor = _currentScope == DataScope.Global ? Color.cyan : Color.white;
                if (GUILayout.Button("Global Data (Shared)", EditorStyles.miniButtonLeft, GUILayout.Height(30)))
                {
                    _currentScope = DataScope.Global;
                    _selectedKeys.Clear();
                }

                var activeProfile = KPlayerPrefs.Profiles.ActiveProfile;

                string tabText;

                tabText = $"Profile Data ({activeProfile})";

                GUI.backgroundColor = _currentScope == DataScope.Profile ? Color.cyan : Color.white;
                if (GUILayout.Button(tabText, EditorStyles.miniButtonRight, GUILayout.Height(30)))
                {
                    _currentScope = DataScope.Profile;
                    _selectedKeys.Clear();
                }

                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(5);
        }

        private void DrawPerformancePanel()
        {
            if (!_showPerformancePanel) return;

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Performance Statistics", EditorStyles.boldLabel);

                var stats = KPlayerPrefs.GetPerformanceStats();

                using (new GUILayout.HorizontalScope())
                {
                    using (new GUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField($"Save Operations: {stats.SaveCount}");
                        EditorGUILayout.LabelField($"Load Operations: {stats.LoadCount}");
                        EditorGUILayout.LabelField($"Total Profiles: {stats.TotalProfiles}");
                    }

                    using (new GUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField($"Avg Save Time: {stats.AverageSaveTime:F3}ms");
                        EditorGUILayout.LabelField($"Avg Load Time: {stats.AverageLoadTime:F3}ms");
                        EditorGUILayout.LabelField($"Active Profile Items: {stats.ActiveProfileItemCount}");
                        EditorGUILayout.LabelField($"Global Items: {stats.GlobalItemCount}");
                    }
                }

                EditorGUILayout.Space(5);

                // Performance tips
                using (new GUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Tips:", EditorStyles.boldLabel, GUILayout.Width(40));
                    EditorGUILayout.LabelField("Use Global data for settings shared across all profiles", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.Space(5);
        }

        private void DrawProfileManagement()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showProfileManagement = EditorGUILayout.Foldout(_showProfileManagement, "Profile Management", true);

                if (_showProfileManagement)
                {
                    EditorGUILayout.Space(5);

                    // Active profile selection
                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Active Profile:", GUILayout.Width(100));

                        var profileInfos = KPlayerPrefs.Profiles.GetAllInfo();
                        var profileNames = profileInfos.Select(p => p.name).ToArray();

                        var activeProfile = KPlayerPrefs.Profiles.ActiveProfile;
                        var selectedIndex = Array.IndexOf(profileNames, activeProfile);
                        if (selectedIndex < 0) selectedIndex = 0;

                        var newSelectedIndex = EditorGUILayout.Popup(selectedIndex, profileNames, GUILayout.Width(200));
                        if (newSelectedIndex != selectedIndex && newSelectedIndex >= 0 && newSelectedIndex < profileNames.Length)
                        {
                            KPlayerPrefs.Profiles.SetActive(profileNames[newSelectedIndex]);
                            RefreshCache();
                        }

                        GUILayout.FlexibleSpace();
                    }

                    EditorGUILayout.Space(5);

                    // Profile operations in columns
                    using (new GUILayout.HorizontalScope())
                    {
                        // Create new profile
                        using (new GUILayout.VerticalScope(GUILayout.Width(250)))
                        {
                            EditorGUILayout.LabelField("Create New Profile:", EditorStyles.boldLabel);
                            _newProfileName = EditorGUILayout.TextField("Name:", _newProfileName);

                            using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(_newProfileName)))
                            {
                                if (GUILayout.Button("Create Profile"))
                                {
                                    CreateNewProfile();
                                }
                            }
                        }

                        GUILayout.Space(20);

                        // Profile actions
                        using (new GUILayout.VerticalScope(GUILayout.Width(150)))
                        {
                            EditorGUILayout.LabelField("Profile Actions:", EditorStyles.boldLabel);

                            if (GUILayout.Button("Copy Current"))
                            {
                                CopyCurrentProfile();
                            }

                            using (new EditorGUI.DisabledGroupScope(KPlayerPrefs.Profiles.ActiveProfile == "Default"))
                            {
                                if (GUILayout.Button("Delete Current"))
                                {
                                    DeleteCurrentProfile();
                                }
                            }
                        }
                        using (new GUILayout.VerticalScope(GUILayout.Width(150)))
                        {
                            EditorGUILayout.LabelField("Danger Zone:", EditorStyles.boldLabel);

                            // Get deletable profiles (exclude Default)
                            var allProfiles = KPlayerPrefs.Profiles.GetAll();
                            var deletableProfiles = allProfiles.Where(p => p != "Default").ToArray();
                            var deletableCount = deletableProfiles.Length;
                            var hasProfilesToDelete = deletableCount > 0;

                            // Only show if there are profiles to delete
                            using (new EditorGUI.DisabledGroupScope(!hasProfilesToDelete))
                            {
                                GUI.backgroundColor = Color.red;
                                var buttonText = hasProfilesToDelete
                                    ? $"Delete All ({deletableCount})"
                                    : "Delete All (None)";

                                if (GUILayout.Button(buttonText))
                                {
                                    DeleteAllProfiles();
                                }
                                GUI.backgroundColor = Color.white;
                            }
                        }
                    }

                    // Profile info display
                    var profileInfo = KPlayerPrefs.Profiles.GetInfo(KPlayerPrefs.Profiles.ActiveProfile);
                    if (profileInfo != null)
                    {
                        EditorGUILayout.Space(5);
                        using (new GUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField($"Created: {profileInfo.creationDate:yyyy/MM/dd HH:mm:ss}", EditorStyles.miniLabel);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.LabelField($"Last Access: {profileInfo.lastAccessDate:yyyy/MM/dd HH:mm:ss}", EditorStyles.miniLabel);
                        }
                    }

                    EditorGUILayout.Space(5);
                }
            }

            EditorGUILayout.Space(5);
        }

        private void DeleteAllProfiles()
        {
            var allProfiles = KPlayerPrefs.Profiles.GetAll();
            var deletableProfiles = allProfiles.Where(p => p != "Default").ToArray();
            var deletableCount = deletableProfiles.Length;

            if (deletableCount == 0)
            {
                return; // Should not happen due to button state, but just in case
            }

            // Simple confirmation
            var message = $"Delete {deletableCount} profiles?\n\n(Default cannot be deleted)";

            if (EditorUtility.DisplayDialog("Delete Profiles", message, "Delete", "Cancel"))
            {
                PerformDeleteAllProfiles(deletableProfiles);
            }
        }

        private void PerformDeleteAllProfiles(string[] profilesToDelete)
        {
            var deletedCount = 0;

            try
            {
                // Delete each profile (Default is already excluded)
                foreach (var profile in profilesToDelete)
                {
                    if (KPlayerPrefs.Profiles.Delete(profile))
                    {
                        deletedCount++;
                    }
                }

                // Switch to Default if current profile was deleted
                KPlayerPrefs.Profiles.SetActive("Default");

                // Refresh UI
                RefreshProfilesList();
                RefreshCache();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[KPlayerPrefs] Error deleting profiles: {e.Message}");
                EditorUtility.DisplayDialog("Error",
                    $"Failed to delete profiles: {e.Message}", "OK");

                RefreshProfilesList();
                RefreshCache();
            }
        }

        private void DrawToolbar()
        {
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Search:", GUILayout.Width(50));
                var newSearchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(200));
                if (newSearchFilter != _searchFilter)
                {
                    _searchFilter = newSearchFilter;
                    _selectedKeys.Clear();
                }

                GUILayout.FlexibleSpace();

                // Sorting dropdown
                EditorGUILayout.LabelField("Sort:", GUILayout.Width(35));
                _sortMode = (SortMode)EditorGUILayout.EnumPopup(_sortMode, GUILayout.Width(80));

                // Batch operations
                using (new EditorGUI.DisabledGroupScope(_selectedKeys.Count == 0))
                {
                    if (GUILayout.Button($"Delete Selected ({_selectedKeys.Count})", EditorStyles.toolbarButton, GUILayout.Width(130)))
                    {
                        DeleteSelectedKeys();
                    }
                }

                if (GUILayout.Button("Select All", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    SelectAllVisible();
                }

                if (GUILayout.Button("Select Clear", EditorStyles.toolbarButton, GUILayout.Width(90)))
                {
                    _selectedKeys.Clear();
                }

                // Data operations
                if (GUILayout.Button("Export", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    ExportCurrentData();
                }

                if (GUILayout.Button("Import", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    ImportData();
                }

                // Danger zone
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Delete All", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    DeleteAllData();
                }
                GUI.backgroundColor = Color.white;
            }
        }

        private void DrawDataList()
        {
            var currentData = GetCurrentData();
            var allKeys = GetAllKeys(currentData);
            var filteredKeys = GetFilteredKeys(allKeys);

            if (filteredKeys.Length == 0)
            {
                EditorGUILayout.Space(20);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (string.IsNullOrEmpty(_searchFilter))
                    {
                        var scopeName = _currentScope == DataScope.Global ? "global" : "profile";
                        EditorGUILayout.LabelField($"No {scopeName} data stored yet. Add some keys below!", EditorStyles.centeredGreyMiniLabel);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"No keys match '{_searchFilter}'", EditorStyles.centeredGreyMiniLabel);
                    }
                    GUILayout.FlexibleSpace();
                }
                return;
            }

            using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollView.scrollPosition;

                for (int i = 0; i < filteredKeys.Length; i++)
                {
                    var key = filteredKeys[i];
                    var isSelected = _selectedKeys.Contains(key);
                    var isAlternate = i % 2 == 1;

                    DrawKeyValuePair(key, isSelected, isAlternate, currentData);
                }
            }
        }

        private void DrawKeyValuePair(string key, bool isSelected, bool isAlternate, Dictionary<string, object> currentData)
        {
            if (!currentData.ContainsKey(key)) return;

            var value = currentData[key];
            var valueType = GetDataType(value);

            var rect = EditorGUILayout.BeginHorizontal();

            if (isSelected)
                EditorGUI.DrawRect(rect, new Color(0.3f, 0.6f, 1f, 0.3f));
            else if (isAlternate)
                EditorGUI.DrawRect(rect, new Color(0.9f, 0.9f, 0.9f, 0.1f));

            // Selection checkbox
            var newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
            if (newSelected != isSelected)
            {
                if (newSelected)
                    _selectedKeys.Add(key);
                else
                    _selectedKeys.Remove(key);
            }

            // Key name
            EditorGUILayout.LabelField(key, EditorStyles.boldLabel, GUILayout.Width(200));

            // Type indicator with color and scope info
            var typeColor = GetTypeColor(valueType);
            GUI.color = typeColor;
            var scopeIndicator = _currentScope == DataScope.Global ? "[G]" : "[P]";
            EditorGUILayout.LabelField($"{scopeIndicator}[{valueType}]", GUILayout.Width(90));
            GUI.color = Color.white;

            // Value display and editing
            DrawValueEditor(key, value, valueType);

            // Actions
            if (GUILayout.Button("Edit", GUILayout.Width(40)))
            {
                if (!_editingValues.ContainsKey(key))
                    _editingValues[key] = value;
            }

            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                DeleteKey(key);
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
        }

        private void DrawValueEditor(string key, object value, DataType valueType)
        {
            var isEditing = _editingValues.ContainsKey(key);

            if (!isEditing)
            {
                var displayValue = FormatValueForDisplay(value, valueType);
                EditorGUILayout.LabelField(displayValue, GUILayout.MinWidth(200));
            }
            else
            {
                using (new GUILayout.HorizontalScope())
                {
                    var editedValue = DrawValueInput(_editingValues[key], valueType);
                    _editingValues[key] = editedValue;

                    if (GUILayout.Button("✓", GUILayout.Width(25)))
                    {
                        SaveEditedValue(key, editedValue, valueType);
                        _editingValues.Remove(key);
                    }

                    if (GUILayout.Button("✗", GUILayout.Width(25)))
                    {
                        _editingValues.Remove(key);
                    }
                }
            }
        }

        private void DrawAddNewSection()
        {
            DrawSeparator();

            using (new GUILayout.HorizontalScope())
            {
                var scopeName = _currentScope == DataScope.Global ? "Global" : "Profile";
                _showAddNew = EditorGUILayout.Foldout(_showAddNew, $"Add New Key to {scopeName} Data", true);
                GUILayout.FlexibleSpace();
            }

            if (_showAddNew)
            {
                EditorGUILayout.Space(5);

                using (new GUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Key:", GUILayout.Width(50));
                    _newKey = EditorGUILayout.TextField(_newKey, GUILayout.Width(150));

                    EditorGUILayout.LabelField("Type:", GUILayout.Width(50));
                    _newValueType = (DataType)EditorGUILayout.EnumPopup(_newValueType, GUILayout.Width(100));

                    EditorGUILayout.LabelField("Value:", GUILayout.Width(50));
                    _newValue = DrawValueInput(_newValue, _newValueType);

                    using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(_newKey)))
                    {
                        if (GUILayout.Button("Add Key", GUILayout.Width(70)))
                        {
                            AddNewKey();
                        }
                    }
                }

                EditorGUILayout.Space(10);
            }
        }

#endregion GUI Drawing Methods

#region Profile Management Methods

        private void CreateNewProfile()
        {
            if (string.IsNullOrEmpty(_newProfileName)) return;

            if (KPlayerPrefs.Profiles.Create(_newProfileName))
            {
                var createdName = _newProfileName;

                KPlayerPrefs.Profiles.SetActive(_newProfileName);

                _newProfileName = "";
                RefreshProfilesList();
                RefreshCache();

                EditorUtility.DisplayDialog("Profile Created", $"Profile '{createdName}' created successfully and is now active!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Profile Creation Failed", "Failed to create profile. It may already exist.", "OK");
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
                KPlayerPrefs.Profiles.SetActive(copyName);

                RefreshProfilesList();
                RefreshCache();

                EditorUtility.DisplayDialog("Profile Copied", $"Profile copied to '{copyName}' and is now active!", "OK");
            }
        }

        private void DeleteCurrentProfile()
        {
            var currentProfile = KPlayerPrefs.Profiles.ActiveProfile;

            if (EditorUtility.DisplayDialog("Delete Profile",
                $"Are you sure you want to delete profile '{currentProfile}'?\n\nThis action cannot be undone!", "Delete", "Cancel"))
            {
                if (KPlayerPrefs.Profiles.Delete(currentProfile))
                {
                    RefreshProfilesList();
                    RefreshCache();
                    EditorUtility.DisplayDialog("Profile Deleted", $"Profile '{currentProfile}' has been deleted.", "OK");
                }
            }
        }

#endregion Profile Management Methods

#region Data Management Methods

        private Dictionary<string, object> GetCurrentData()
        {
            return _currentScope == DataScope.Global ? _cachedGlobalData : _cachedProfileData;
        }

        private void SaveEditedValue(string key, object value, DataType dataType)
        {
            if (_currentScope == DataScope.Global)
            {
                SaveToGlobalData(key, value, dataType);
            }
            else
            {
                SaveToProfileData(key, value, dataType);
            }
        }

        private void SaveToGlobalData(string key, object value, DataType dataType)
        {
            // Global data uses simple API calls
            switch (dataType)
            {
                case DataType.Int:
                    KPlayerPrefs.SetInt(key, (int)value);
                    break;

                case DataType.Float:
                    KPlayerPrefs.SetFloat(key, (float)value);
                    break;

                case DataType.String:
                    KPlayerPrefs.SetString(key, (string)value);
                    break;

                case DataType.Bool:
                    KPlayerPrefs.SetBool(key, (bool)value);
                    break;

                case DataType.Vector2:
                    KPlayerPrefs.SetVector2(key, (Vector2)value);
                    break;

                case DataType.Vector3:
                    KPlayerPrefs.SetVector3(key, (Vector3)value);
                    break;

                case DataType.Color:
                    KPlayerPrefs.SetColor(key, (Color)value);
                    break;

                case DataType.DateTime:
                    KPlayerPrefs.SetDateTime(key, (System.DateTime)value);
                    break;
            }
        }

        private void SaveToProfileData(string key, object value, DataType dataType)
        {
            // Profile data uses explicit .Profiles API calls
            switch (dataType)
            {
                case DataType.Int:
                    KPlayerPrefs.Profiles.SetInt(key, (int)value);
                    break;

                case DataType.Float:
                    KPlayerPrefs.Profiles.SetFloat(key, (float)value);
                    break;

                case DataType.String:
                    KPlayerPrefs.Profiles.SetString(key, (string)value);
                    break;

                case DataType.Bool:
                    KPlayerPrefs.Profiles.SetBool(key, (bool)value);
                    break;

                case DataType.Vector2:
                    KPlayerPrefs.Profiles.SetVector2(key, (Vector2)value);
                    break;

                case DataType.Vector3:
                    KPlayerPrefs.Profiles.SetVector3(key, (Vector3)value);
                    break;

                case DataType.Color:
                    KPlayerPrefs.Profiles.SetColor(key, (Color)value);
                    break;

                case DataType.DateTime:
                    KPlayerPrefs.Profiles.SetDateTime(key, (System.DateTime)value);
                    break;
            }
        }

        private void AddNewKey()
        {
            if (string.IsNullOrEmpty(_newKey)) return;

            var currentData = GetCurrentData();
            if (currentData.ContainsKey(_newKey))
            {
                if (!EditorUtility.DisplayDialog("Key Exists",
                    $"Key '{_newKey}' already exists. Overwrite?", "Yes", "No"))
                    return;
            }

            SaveEditedValue(_newKey, _newValue ?? GetDefaultValue(_newValueType), _newValueType);

            _newKey = "";
            _newValue = GetDefaultValue(_newValueType);
        }

        private void DeleteKey(string key)
        {
            var scopeName = _currentScope == DataScope.Global ? "global" : "profile";
            if (EditorUtility.DisplayDialog("Delete Key",
                $"Are you sure you want to delete '{key}' from {scopeName} data?", "Yes", "No"))
            {
                if (_currentScope == DataScope.Global)
                {
                    KPlayerPrefs.DeleteKey(key);
                }
                else
                {
                    KPlayerPrefs.Profiles.DeleteKey(key);
                }

                _selectedKeys.Remove(key);
                _editingValues.Remove(key);
            }
        }

        private void DeleteSelectedKeys()
        {
            if (_selectedKeys.Count == 0) return;

            var scopeName = _currentScope == DataScope.Global ? "global" : "profile";
            if (EditorUtility.DisplayDialog("Delete Selected Keys",
                $"Are you sure you want to delete {_selectedKeys.Count} selected keys from {scopeName} data?", "Yes", "No"))
            {
                foreach (var key in _selectedKeys)
                {
                    if (_currentScope == DataScope.Global)
                    {
                        KPlayerPrefs.DeleteKey(key);
                    }
                    else
                    {
                        KPlayerPrefs.Profiles.DeleteKey(key);
                    }
                    _editingValues.Remove(key);
                }
                _selectedKeys.Clear();
            }
        }

        private void DeleteAllData()
        {
            var scopeName = _currentScope == DataScope.Global ? "global" : "profile";
            var apiCall = _currentScope == DataScope.Global ? "KPlayerPrefs.DeleteAll()" : "KPlayerPrefs.Profiles.DeleteAll()";

            if (EditorUtility.DisplayDialog("Delete All Data",
                $"Are you sure you want to delete ALL {scopeName} data?\n\nThis cannot be undone!", "Yes", "No"))
            {
                if (_currentScope == DataScope.Global)
                {
                    KPlayerPrefs.DeleteAll();
                }
                else
                {
                    KPlayerPrefs.Profiles.DeleteAll();
                }

                _selectedKeys.Clear();
                _editingValues.Clear();
            }
        }

        private void SelectAllVisible()
        {
            var currentData = GetCurrentData();
            var filteredKeys = GetFilteredKeys(GetAllKeys(currentData));
            _selectedKeys.Clear();
            _selectedKeys.AddRange(filteredKeys);
        }

        private void ExportCurrentData()
        {
            var scopeName = _currentScope == DataScope.Global ? "Global" : "Profile";
            var defaultFileName = _currentScope == DataScope.Global
                ? "GlobalData"
                : $"Profile_{KPlayerPrefs.Profiles.ActiveProfile}";

            var path = EditorUtility.SaveFilePanel($"Export {scopeName} Data", "", defaultFileName, "json");
            if (!string.IsNullOrEmpty(path))
            {
                if (_currentScope == DataScope.Global)
                {
                    KPlayerPrefs.ExportGlobalData(path);
                }
                else
                {
                    KPlayerPrefs.ExportActiveProfile(path);
                }
                EditorUtility.DisplayDialog("Export Complete", $"{scopeName} data exported to:\n{path}", "OK");
            }
        }

        private void ImportData()
        {
            var scopeName = _currentScope == DataScope.Global ? "Global" : "Profile";
            var path = EditorUtility.OpenFilePanel($"Import {scopeName} Data", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                var merge = EditorUtility.DisplayDialog("Import Data",
                    "How would you like to import the data?", "Merge with existing", "Replace all data");

                if (_currentScope == DataScope.Global)
                {
                    KPlayerPrefs.ImportGlobalData(path, merge);
                }
                else
                {
                    KPlayerPrefs.ImportActiveProfile(path, merge);
                }
                EditorUtility.DisplayDialog("Import Complete", $"{scopeName} data imported successfully!", "OK");
            }
        }

#endregion Data Management Methods

#region Helper Methods

        private void ToggleAutoUpdate(bool enabled)
        {
            if (_autoUpdateEnabled == enabled) return;

            _autoUpdateEnabled = enabled;

            if (_autoUpdateEnabled)
            {
                KPlayerPrefs.OnDataChanged -= OnDataChanged;
                KPlayerPrefs.OnDataChanged += OnDataChanged;
                KPlayerPrefs.OnActiveProfileChanged -= OnActiveProfileChanged;
                KPlayerPrefs.OnActiveProfileChanged += OnActiveProfileChanged;
                RefreshCache();
                RefreshProfilesList();
                Repaint();
            }
            else
            {
                KPlayerPrefs.OnDataChanged -= OnDataChanged;
                KPlayerPrefs.OnActiveProfileChanged -= OnActiveProfileChanged;
            }
        }

        private string[] GetAllKeys(Dictionary<string, object> data)
        {
            if (data == null || data.Count == 0)
                return new string[0];

            var keys = new string[data.Keys.Count];
            data.Keys.CopyTo(keys, 0);
            return keys;
        }

        private string[] GetFilteredKeys(string[] allKeys)
        {
            var currentData = GetCurrentData();
            var filteredKeys = allKeys;

            // Apply search filter first
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                filteredKeys = allKeys.Where(key => key.ToLower().Contains(_searchFilter.ToLower())).ToArray();
            }

            // Apply sorting
            switch (_sortMode)
            {
                case SortMode.AlphabeticalAZ:
                    filteredKeys = filteredKeys.OrderBy(k => k).ToArray();
                    break;

                case SortMode.AlphabeticalZA:
                    filteredKeys = filteredKeys.OrderByDescending(k => k).ToArray();
                    break;

                case SortMode.DataType:
                    filteredKeys = filteredKeys.OrderBy(k => GetDataType(currentData[k]).ToString()).ThenBy(k => k).ToArray();
                    break;
            }

            return filteredKeys;
        }

        private DataType GetDataType(object value)
        {
            if (value is int) return DataType.Int;
            if (value is float) return DataType.Float;
            if (value is string) return DataType.String;
            if (value is bool) return DataType.Bool;
            if (value is SerializableVector2) return DataType.Vector2;
            if (value is SerializableVector3) return DataType.Vector3;
            if (value is SerializableColor) return DataType.Color;
            if (value is SerializableDateTime) return DataType.DateTime;
            return DataType.String;
        }

        private Color GetTypeColor(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.Int: return new Color(0.4f, 0.8f, 1f); // Light blue
                case DataType.Float: return new Color(0.4f, 1f, 0.4f); // Light green
                case DataType.String: return new Color(1f, 1f, 0.4f); // Light yellow
                case DataType.Bool: return new Color(1f, 0.4f, 1f); // Light magenta
                case DataType.Vector2: return new Color(1f, 0.6f, 0.4f); // Light orange
                case DataType.Vector3: return new Color(0.6f, 0.4f, 1f); // Light purple
                case DataType.Color: return new Color(1f, 0.7f, 0.4f); // Peach
                case DataType.DateTime: return new Color(0.7f, 1f, 1f); // Light cyan
                default: return Color.white;
            }
        }

        private object GetDefaultValue(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.Int: return 0;
                case DataType.Float: return 0f;
                case DataType.String: return "";
                case DataType.Bool: return false;
                case DataType.Vector2: return Vector2.zero;
                case DataType.Vector3: return Vector3.zero;
                case DataType.Color: return Color.white;
                case DataType.DateTime: return System.DateTime.Now;
                default: return null;
            }
        }

        private object DrawValueInput(object currentValue, DataType dataType)
        {
            switch (dataType)
            {
                case DataType.Int:
                    return EditorGUILayout.IntField(currentValue as int? ?? 0, GUILayout.Width(100));

                case DataType.Float:
                    return EditorGUILayout.FloatField(currentValue as float? ?? 0f, GUILayout.Width(100));

                case DataType.String:
                    return EditorGUILayout.TextField(currentValue as string ?? "", GUILayout.Width(150));

                case DataType.Bool:
                    return EditorGUILayout.Toggle(currentValue as bool? ?? false, GUILayout.Width(20));

                case DataType.Vector2:
                    Vector2 v2 = Vector2.zero;
                    if (currentValue is SerializableVector2 sv2)
                        v2 = sv2.ToVector2();
                    else if (currentValue is Vector2 directV2)
                        v2 = directV2;
                    return EditorGUILayout.Vector2Field("", v2, GUILayout.Width(120));

                case DataType.Vector3:
                    Vector3 v3 = Vector3.zero;
                    if (currentValue is SerializableVector3 sv3)
                        v3 = sv3.ToVector3();
                    else if (currentValue is Vector3 directV3)
                        v3 = directV3;
                    return EditorGUILayout.Vector3Field("", v3, GUILayout.Width(150));

                case DataType.Color:
                    Color c = Color.white;
                    if (currentValue is SerializableColor sc)
                        c = sc.ToColor();
                    else if (currentValue is Color directColor)
                        c = directColor;
                    return EditorGUILayout.ColorField(c, GUILayout.Width(60));

                case DataType.DateTime:
                    System.DateTime dt = System.DateTime.Now;
                    if (currentValue is SerializableDateTime sdt)
                        dt = sdt.ToDateTime();
                    else if (currentValue is System.DateTime directDateTime)
                        dt = directDateTime;

                    using (new GUILayout.HorizontalScope())
                    {
                        var year = EditorGUILayout.IntField(dt.Year, GUILayout.Width(50));
                        EditorGUILayout.LabelField("/", GUILayout.Width(10));
                        var month = EditorGUILayout.IntField(dt.Month, GUILayout.Width(30));
                        EditorGUILayout.LabelField("/", GUILayout.Width(10));
                        var day = EditorGUILayout.IntField(dt.Day, GUILayout.Width(30));
                        EditorGUILayout.LabelField(" ", GUILayout.Width(10));
                        var hour = EditorGUILayout.IntField(dt.Hour, GUILayout.Width(30));
                        EditorGUILayout.LabelField(":", GUILayout.Width(10));
                        var minute = EditorGUILayout.IntField(dt.Minute, GUILayout.Width(30));
                        EditorGUILayout.LabelField(":", GUILayout.Width(10));
                        var second = EditorGUILayout.IntField(dt.Second, GUILayout.Width(30));

                        try
                        {
                            return new System.DateTime(year, month, day, hour, minute, second);
                        }
                        catch
                        {
                            return dt;
                        }
                    }

                default:
                    return currentValue;
            }
        }

        private string FormatValueForDisplay(object value, DataType dataType)
        {
            if (value == null) return "null";

            switch (dataType)
            {
                case DataType.Vector2:
                    if (value is SerializableVector2 sv2)
                        return $"({sv2.x:F2}, {sv2.y:F2})";
                    return value.ToString();

                case DataType.Vector3:
                    if (value is SerializableVector3 sv3)
                        return $"({sv3.x:F2}, {sv3.y:F2}, {sv3.z:F2})";
                    return value.ToString();

                case DataType.Color:
                    if (value is SerializableColor sc)
                        return $"RGBA({sc.r:F2}, {sc.g:F2}, {sc.b:F2}, {sc.a:F2})";
                    return value.ToString();

                case DataType.DateTime:
                    if (value is SerializableDateTime sdt)
                        return sdt.ToDateTime().ToString("yyyy/MM/dd HH:mm:ss");
                    return value.ToString();

                case DataType.String:
                    var str = value.ToString();
                    return str.Length > 40 ? str.Substring(0, 37) + "..." : str;

                case DataType.Float:
                    if (value is float f)
                        return f.ToString("F3");
                    return value.ToString();

                default:
                    return value.ToString();
            }
        }

        private void HandleKeyboardInput()
        {
            var current = Event.current;
            if (current.type == EventType.KeyDown)
            {
                switch (current.keyCode)
                {
                    case KeyCode.Delete:
                        if (_selectedKeys.Count > 0)
                        {
                            DeleteSelectedKeys();
                            current.Use();
                        }
                        break;

                    case KeyCode.A:
                        if (current.control || current.command)
                        {
                            SelectAllVisible();
                            current.Use();
                        }
                        break;

                    case KeyCode.F5:
                        RefreshCache();
                        RefreshProfilesList();
                        current.Use();
                        break;

                    case KeyCode.Escape:
                        _selectedKeys.Clear();
                        _editingValues.Clear();
                        current.Use();
                        break;

                    case KeyCode.Tab:
                        if (current.shift)
                        {
                            // Shift+Tab: switch to previous scope
                            _currentScope = _currentScope == DataScope.Profile
                                ? DataScope.Global
                                : DataScope.Profile;
                        }
                        else
                        {
                            // Tab: switch to next scope
                            _currentScope = _currentScope == DataScope.Profile
                                ? DataScope.Global
                                : DataScope.Profile;
                        }
                        _selectedKeys.Clear();
                        current.Use();
                        break;
                }
            }
        }

        private void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(5);
        }

#endregion Helper Methods
    }
}

#endif