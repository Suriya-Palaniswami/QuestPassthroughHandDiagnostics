// Assets/HandPassthroughStability/Editor/Stability/StabilityTestWindow.cs
using System.IO;
using UnityEditor;
using UnityEngine;
using HPS.Stability;
using HPS.Stability.Diagnostics;

public class StabilityTestWindow : EditorWindow
{
    StabilityTestRunner runner;

    [MenuItem("Tools/Hand Passthrough Stability Test")]
    public static void ShowWindow() => GetWindow<StabilityTestWindow>("Stability Test");

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Attach 'StabilityTestRunner', 'FrameLogHook', 'ResourceMonitor', and 'HandPoseNaNGuard' to a scene GameObject. Use this window to find the runner and open the latest log.", MessageType.Info);

        if (GUILayout.Button("Find Runner in Scene"))
        {
            runner = FindObjectOfType<StabilityTestRunner>();
            if (runner) Debug.Log("[Editor] Found StabilityTestRunner.");
            else Debug.LogWarning("[Editor] No StabilityTestRunner in scene.");
        }

        using (new EditorGUI.DisabledScope(runner == null))
        {
            if (runner != null)
            {
                EditorGUILayout.LabelField("Scenario", runner.scenario.ToString());
                EditorGUILayout.LabelField("Run (min)", runner.runMinutes.ToString("F0"));
                EditorGUILayout.LabelField("Soft Reset", runner.enableSoftReset ? "On" : "Off");
            }
        }

        GUILayout.Space(8);
        if (GUILayout.Button("Open Latest Log Folder"))
        {
            var dir = Path.Combine(Application.persistentDataPath, "logs");
            Directory.CreateDirectory(dir);
            EditorUtility.RevealInFinder(dir);
        }
    }
}
