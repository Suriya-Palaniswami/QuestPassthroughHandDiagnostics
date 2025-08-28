// Assets/HandPassthroughStability/Runtime/Diagnostics/FrameLogHook.cs
using UnityEngine;

namespace HPS.Stability.Diagnostics
{
    public class FrameLogHook : MonoBehaviour
    {
        void OnEnable()
        {
            LogFile.Init();
            Application.logMessageReceived += HandleLog;
            LogFile.WriteLine("[FrameLogHook] Attached.");
        }

        void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
            LogFile.WriteLine("[FrameLogHook] Detached.");
        }

        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            // Reduce noise; still capture warnings & errors
            if (type == LogType.Warning || type == LogType.Error || type == LogType.Exception)
            {
                LogFile.WriteLine($"[{type}] {condition}");
            }

            // Flag known patterns explicitly
            if (condition.Contains("FrameSetCollator") ||
                condition.Contains("deadlineMissed") ||
                condition.Contains("muxModeMisMatch") ||
                condition.Contains("HANDTRACKING NaN"))
            {
                LogFile.WriteLine($"[FLAG] {condition}");
            }
        }
    }
}
