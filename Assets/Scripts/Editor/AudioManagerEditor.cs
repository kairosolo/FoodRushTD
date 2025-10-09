using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

[CustomEditor(typeof(AudioManager))]
public class AudioManagerEditor : Editor
{
    private SerializedProperty musicMixerProp;
    private SerializedProperty sfxMixerProp;
    private SerializedProperty musicAudioSourceProp;
    private SerializedProperty sfxAudioSourceProp;
    private SerializedProperty loopingSfxAudioSourceProp;
    private SerializedProperty musicTracksProp;
    private SerializedProperty sfxClipsProp;

    private string musicSearchQuery = "";
    private string sfxSearchQuery = "";
    private bool musicListExpanded = true;
    private bool sfxListExpanded = true;

    private string newMusicName = "";
    private AudioClip newMusicClip = null;
    private string newSfxName = "";
    private AudioClip newSfxClip = null;

    private static System.Action<AudioClip> _playClip;
    private static System.Action _stopAllClips;
    private static System.Func<AudioClip, bool> _isClipPlaying;
    private AudioClip _currentlyPlayingClip = null;

    private void OnEnable()
    {
        musicMixerProp = serializedObject.FindProperty("musicMixer");
        sfxMixerProp = serializedObject.FindProperty("sfxMixer");
        musicAudioSourceProp = serializedObject.FindProperty("musicAudioSource");
        sfxAudioSourceProp = serializedObject.FindProperty("sfxAudioSource");
        loopingSfxAudioSourceProp = serializedObject.FindProperty("loopingSfxAudioSource");
        musicTracksProp = serializedObject.FindProperty("musicTracks");
        sfxClipsProp = serializedObject.FindProperty("sfxClips");

        var audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        if (audioUtil != null)
        {
            var playMethod = audioUtil.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            var stopMethod = audioUtil.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public);
            var isPlayingMethod = audioUtil.GetMethod("IsPreviewClipPlaying", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(AudioClip) }, null);

            if (playMethod != null) _playClip = clip => playMethod.Invoke(null, new object[] { clip, 0, false });
            if (stopMethod != null) _stopAllClips = () => stopMethod.Invoke(null, null);
            if (isPlayingMethod != null) _isClipPlaying = clip => (bool)isPlayingMethod.Invoke(null, new object[] { clip });
        }
    }

    private void OnDisable()
    {
        _stopAllClips?.Invoke();
        _currentlyPlayingClip = null;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (_currentlyPlayingClip != null && (_isClipPlaying == null || !_isClipPlaying(_currentlyPlayingClip)))
        {
            _currentlyPlayingClip = null;
            Repaint();
        }

        EditorGUILayout.LabelField("Audio Mixers", EditorStyles.boldLabel);
        // This is where the new master mixer field would be drawn if added to the editor script.
        EditorGUILayout.PropertyField(musicMixerProp);
        EditorGUILayout.PropertyField(sfxMixerProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Audio Sources", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(musicAudioSourceProp);
        EditorGUILayout.PropertyField(sfxAudioSourceProp);
        EditorGUILayout.PropertyField(loopingSfxAudioSourceProp);

        EditorGUILayout.Space(10);

        DrawSoundList("Music Tracks", musicTracksProp, ref musicSearchQuery, ref musicListExpanded, ref newMusicName, ref newMusicClip);
        EditorGUILayout.Space(5);
        DrawSoundList("SFX Clips", sfxClipsProp, ref sfxSearchQuery, ref sfxListExpanded, ref newSfxName, ref newSfxClip);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSoundList(string title, SerializedProperty listProperty, ref string searchQuery, ref bool isExpanded, ref string newName, ref AudioClip newClip)
    {
        isExpanded = EditorGUILayout.Foldout(isExpanded, $"{title} ({listProperty.arraySize})", true, EditorStyles.boldLabel);

        if (isExpanded)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            searchQuery = EditorGUILayout.TextField("Search", searchQuery);
            if (GUILayout.Button("Sort A-Z", GUILayout.Width(80)))
            {
                Undo.RecordObject(target, "Sort Audio List");
                SortSoundArray(listProperty);
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            int elementsToShowCount = 0;
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                var element = listProperty.GetArrayElementAtIndex(i);
                var nameProp = element.FindPropertyRelative("name");
                var clipProp = element.FindPropertyRelative("clip");
                var clip = (AudioClip)clipProp.objectReferenceValue;

                bool isVisible = string.IsNullOrEmpty(searchQuery) || nameProp.stringValue.ToLower().Contains(searchQuery.ToLower());

                if (isVisible)
                {
                    elementsToShowCount++;
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    string buttonLabel = (_currentlyPlayingClip == clip && _isClipPlaying != null && _isClipPlaying(clip)) ? "Stop" : "Play";
                    GUI.enabled = clip != null && _playClip != null;
                    if (GUILayout.Button(buttonLabel, GUILayout.Width(50)))
                    {
                        if (_currentlyPlayingClip == clip && _isClipPlaying != null && _isClipPlaying(clip))
                        {
                            _stopAllClips?.Invoke();
                            _currentlyPlayingClip = null;
                        }
                        else
                        {
                            _stopAllClips?.Invoke();
                            _playClip?.Invoke(clip);
                            _currentlyPlayingClip = clip;
                        }
                    }
                    GUI.enabled = true;

                    EditorGUILayout.PropertyField(clipProp, GUIContent.none, GUILayout.MinWidth(80));

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(nameProp, GUIContent.none);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (DoesNameExist(listProperty, nameProp.stringValue, i))
                        {
                            Debug.LogWarning($"Duplicate audio name '{nameProp.stringValue}' detected. Appending suffix.");
                            nameProp.stringValue += "_copy";
                        }
                    }

                    if (GUILayout.Button("-", GUILayout.Width(25)))
                    {
                        Undo.RecordObject(target, "Remove Audio");
                        _stopAllClips?.Invoke();
                        _currentlyPlayingClip = null;
                        listProperty.DeleteArrayElementAtIndex(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (!string.IsNullOrEmpty(searchQuery) && elementsToShowCount == 0)
            {
                EditorGUILayout.HelpBox("No sounds match your search query.", MessageType.Info);
            }

            EditorGUILayout.Space(10);

            Rect dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag AudioClips here to add", EditorStyles.helpBox);
            HandleDragAndDrop(dropArea, listProperty);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Add New Audio", EditorStyles.miniBoldLabel);
            newName = EditorGUILayout.TextField("Name", newName);
            newClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", newClip, typeof(AudioClip), false);

            bool isDuplicate = !string.IsNullOrEmpty(newName) && DoesNameExist(listProperty, newName, -1);
            if (isDuplicate)
            {
                EditorGUILayout.HelpBox("This name already exists in the list.", MessageType.Warning);
            }

            GUI.enabled = !string.IsNullOrEmpty(newName) && newClip != null && !isDuplicate;
            if (GUILayout.Button("Add Audio"))
            {
                Undo.RecordObject(target, "Add Audio");
                AddNewSound(listProperty, newName, newClip);
                newName = "";
                newClip = null;
                GUI.FocusControl(null);
            }
            GUI.enabled = true;

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
    }

    private void HandleDragAndDrop(Rect dropArea, SerializedProperty listProperty)
    {
        Event evt = Event.current;
        if (!dropArea.Contains(evt.mousePosition)) return;

        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (Object dragged in DragAndDrop.objectReferences)
                {
                    if (dragged is AudioClip clip)
                    {
                        string clipName = clip.name;
                        if (DoesNameExist(listProperty, clipName, -1))
                            clipName += "_copy";

                        Undo.RecordObject(target, "Drag & Drop Audio");
                        AddNewSound(listProperty, clipName, clip);
                    }
                }
            }
            Event.current.Use();
        }
    }

    private void AddNewSound(SerializedProperty listProperty, string name, AudioClip clip)
    {
        int newIndex = listProperty.arraySize;
        listProperty.InsertArrayElementAtIndex(newIndex);
        var newElement = listProperty.GetArrayElementAtIndex(newIndex);
        newElement.FindPropertyRelative("name").stringValue = name;
        newElement.FindPropertyRelative("clip").objectReferenceValue = clip;
    }

    private bool DoesNameExist(SerializedProperty listProperty, string name, int excludeIndex)
    {
        for (int i = 0; i < listProperty.arraySize; i++)
        {
            if (i == excludeIndex) continue;
            if (listProperty.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)
                return true;
        }
        return false;
    }

    private void SortSoundArray(SerializedProperty listProperty)
    {
        var soundList = new List<(string, AudioClip)>();
        for (int i = 0; i < listProperty.arraySize; i++)
        {
            var element = listProperty.GetArrayElementAtIndex(i);
            soundList.Add((element.FindPropertyRelative("name").stringValue, (AudioClip)element.FindPropertyRelative("clip").objectReferenceValue));
        }

        soundList = soundList.OrderBy(s => s.Item1).ToList();

        listProperty.ClearArray();
        for (int i = 0; i < soundList.Count; i++)
        {
            listProperty.InsertArrayElementAtIndex(i);
            var element = listProperty.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("name").stringValue = soundList[i].Item1;
            element.FindPropertyRelative("clip").objectReferenceValue = soundList[i].Item2;
        }
    }
}