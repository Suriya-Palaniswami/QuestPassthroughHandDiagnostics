// Assets/HandPassthroughStability/Runtime/Diagnostics/HeartbeatLogger.cs
using UnityEngine;

namespace HPS.Stability.Diagnostics
{
    public class HeartbeatLogger : MonoBehaviour
    {
        public float interval = 10f;
        float t;
        void OnEnable()
        {
            LogFile.Init("quest_hand_passthrough");
            LogFile.WriteLine("[HB] HeartbeatLogger enabled.");
        }
        void Update()
        {
            t += Time.unscaledDeltaTime;
            if (t >= interval)
            {
                t = 0f;
                LogFile.WriteLine("[HB] tick");
            }
        }
    }
}
