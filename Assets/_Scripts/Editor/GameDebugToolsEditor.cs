using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameDebugTools))]
public class GameDebugToolsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameDebugTools debugTools = (GameDebugTools)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Daily Event Triggers", EditorStyles.boldLabel);

        if (GUILayout.Button("Trigger Selected Event"))
        {
            if (debugTools.eventToTrigger != null && DailyEventManager.Instance != null)
            {
                DailyEventManager.Instance.Debug_TriggerEvent(debugTools.eventToTrigger);
                Debug.Log($"DEBUG: Manually triggered event '{debugTools.eventToTrigger.EventName}'");
            }
            else
            {
                Debug.LogWarning("DEBUG: No event selected or DailyEventManager not found!");
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Progression Triggers", EditorStyles.boldLabel);

        if (GUILayout.Button("Trigger Station Unlock"))
        {
            if (debugTools.stationToUnlock != null && ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.Debug_UnlockStation(debugTools.stationToUnlock);
                Debug.Log($"DEBUG: Manually triggered unlock for '{debugTools.stationToUnlock.StationName}'");
            }
            else
            {
                Debug.LogWarning("DEBUG: No station selected or ProgressionManager not found!");
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("VIP Trigger", EditorStyles.boldLabel);

        if (GUILayout.Button("Spawn VIP Now"))
        {
            if (VIPManager.Instance != null)
            {
                VIPManager.Instance.Debug_SpawnVip();
                Debug.Log("DEBUG: Manually spawning VIP.");
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Economy Tools", EditorStyles.boldLabel);

        if (GUILayout.Button($"Add ${debugTools.cashToAdd} Cash"))
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCash(debugTools.cashToAdd);
                Debug.Log($"DEBUG: Added ${debugTools.cashToAdd} cash.");
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Game State Tools", EditorStyles.boldLabel);

        if (GUILayout.Button($"Add {debugTools.unsatisfiedCustomersToAdd} Unsatisfied Customer(s)"))
        {
            if (GameLoopManager.Instance != null)
            {
                for (int i = 0; i < debugTools.unsatisfiedCustomersToAdd; i++)
                {
                    GameLoopManager.Instance.CustomerReachedExitUnsatisfied();
                }
                Debug.Log($"DEBUG: Added {debugTools.unsatisfiedCustomersToAdd} unsatisfied customer(s).");
            }
            else
            {
                Debug.LogWarning("DEBUG: GameLoopManager not found!");
            }
        }

        if (GUILayout.Button("Trigger Defeat"))
        {
            if (GameLoopManager.Instance != null)
            {
                GameLoopManager.Instance.Debug_TriggerGameOver();
            }
            else
            {
                Debug.LogWarning("DEBUG: GameLoopManager not found!");
            }
        }
    }
}