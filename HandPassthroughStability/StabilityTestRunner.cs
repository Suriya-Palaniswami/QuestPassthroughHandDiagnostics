// Assets/HandPassthroughStability/Runtime/Stability/StabilityTestRunner.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR.Hands;
using HPS.Stability.Diagnostics;
using HPS.Stability.Passthrough;

namespace HPS.Stability
{
    public enum TestScenario
    {
        HandOnly,
        HandPlusDepth,
        HandPlusRGB,
        HandPlusRGBAndDepth
    }

    public class StabilityTestRunner : MonoBehaviour
    {
        [Header("Scenario")]
        public TestScenario scenario = TestScenario.HandPlusRGBAndDepth;

        [Header("Durations (minutes)")]
        public float warmupMinutes = 2f;
        public float runMinutes = 90f;

        [Header("Soft Reset")]
        public bool enableSoftReset = true;
        public float resetEveryMinutes = 30f;
        [Tooltip("What to reset at each interval")]
        public bool resetHands = true;
        public bool resetPassthrough = true;

        [Header("Adapters")]
        public bool preferOculusIntegrationAdapter = false;

        // Public telemetry for HUD
        [HideInInspector] public int resetCount = 0;
        [HideInInspector] public float lastResetAtSeconds = -1f;  // since test start (unscaled)
        [HideInInspector] public float startedAtRealtime;         // realtimeSinceStartup at test start

        XRHandSubsystem _hands;
        IPassthroughAdapter _pt;

        Coroutine _loop;

        void Awake()
        {
            LogFile.Init("quest_hand_passthrough");
            LogFile.WriteLine("[TEST] StabilityTestRunner Awake.");
            Debug.Log("[StabilityTest] Awake.");

            LogFile.WriteLine($"[TEST] Scenario={scenario} warmup={warmupMinutes}m run={runMinutes}m softReset={enableSoftReset} every={resetEveryMinutes}m resetHands={resetHands} resetPT={resetPassthrough}");
        }

        void OnEnable()
        {
            // Subsystems
            _hands = GetHandSubsystem();

            // Choose passthrough adapter
            if (preferOculusIntegrationAdapter) _pt = new OculusIntegrationPassthroughAdapter();
            else _pt = new OpenXRMetaPassthroughAdapter();

            if (_hands == null) Debug.LogWarning("[StabilityTest] XRHandSubsystem not found.");
            if (_pt == null || !_pt.IsAvailable) Debug.LogWarning("[StabilityTest] Passthrough adapter not available.");

            _loop = StartCoroutine(RunTest());
        }

        void OnDisable()
        {
            if (_loop != null) StopCoroutine(_loop);
            _pt?.ShutdownAll();
            LogFile.WriteLine("[TEST] StabilityTestRunner disabled.");
        }

        IEnumerator RunTest()
        {
            startedAtRealtime = Time.realtimeSinceStartup;
            resetCount = 0;
            lastResetAtSeconds = -1f;

            yield return StartCoroutine(ApplyScenarioAndWarmup());

            LogFile.WriteLine("[TEST] Entering main run…");
            Debug.Log("[StabilityTest] Entering main run…");

            float start = Time.realtimeSinceStartup;
            float elapsed = 0f;
            float total = Mathf.Max(0f, runMinutes) * 60f;

            // schedule first reset strictly after warmup
            float nextReset = (enableSoftReset && resetEveryMinutes > 0.01f)
                ? (Time.realtimeSinceStartup - start) + (resetEveryMinutes * 60f)
                : float.PositiveInfinity;

            while (elapsed < total)
            {
                // Use unscaled time to avoid timescale effects
                elapsed = Time.realtimeSinceStartup - start;

                if (enableSoftReset && resetEveryMinutes > 0.01f && elapsed >= nextReset)
                {
                    // catch up if we missed multiple intervals
                    while (elapsed >= nextReset)
                    {
                        yield return StartCoroutine(DoSoftReset());
                        nextReset += resetEveryMinutes * 60f;
                    }
                }

                yield return null;
            }

            LogFile.WriteLine("[TEST] Completed run.");
            Debug.Log("[StabilityTest] Completed run.");
        }

        IEnumerator ApplyScenarioAndWarmup()
        {
            LogFile.WriteLine("[TEST] Applying scenario & warmup…");
            Debug.Log("[StabilityTest] Applying scenario & warmup…");

            // Ensure hands running
            if (_hands != null && !_hands.running)
            {
                LogFile.WriteLine("[TEST] Starting XRHandSubsystem…");
                Debug.Log("[StabilityTest] Starting XRHandSubsystem…");
                _hands.Start();
            }

            // Configure passthrough per scenario
            switch (scenario)
            {
                case TestScenario.HandOnly:
                    _pt?.ShutdownAll();
                    break;
                case TestScenario.HandPlusDepth:
                    _pt?.SetBoth(false, true);
                    break;
                case TestScenario.HandPlusRGB:
                    _pt?.SetBoth(true, false);
                    break;
                case TestScenario.HandPlusRGBAndDepth:
                    _pt?.SetBoth(true, true);
                    break;
            }

            var warm = Mathf.Max(0f, warmupMinutes) * 60f;
            var warmStart = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - warmStart < warm)
                yield return null;

            LogFile.WriteLine("[TEST] Warmup complete.");
            Debug.Log("[StabilityTest] Warmup complete.");
        }

        IEnumerator DoSoftReset()
        {
            var tNow = Time.realtimeSinceStartup - startedAtRealtime;

            LogFile.WriteLine($"[RESET] Initiating soft reset (hands={resetHands}, passthrough={resetPassthrough}) at {tNow:F2}s…");
            Debug.Log($"[StabilityTest] RESET begin (hands={resetHands}, pt={resetPassthrough})");

            if (resetHands && _hands != null)
            {
                LogFile.WriteLine("[RESET] Stopping XRHandSubsystem…");
                _hands.Stop();
                yield return null; // let a frame pass
                LogFile.WriteLine("[RESET] Starting XRHandSubsystem…");
                _hands.Start();
            }

            if (resetPassthrough && _pt != null)
            {
                LogFile.WriteLine("[RESET] Toggling passthrough off…");
                _pt.ShutdownAll();
                // a brief wait to ensure pipeline tears down
                var end = Time.realtimeSinceStartup + 0.25f;
                while (Time.realtimeSinceStartup < end) yield return null;

                switch (scenario)
                {
                    case TestScenario.HandOnly:
                        _pt.ShutdownAll();
                        break;
                    case TestScenario.HandPlusDepth:
                        _pt.SetBoth(false, true);
                        break;
                    case TestScenario.HandPlusRGB:
                        _pt.SetBoth(true, false);
                        break;
                    case TestScenario.HandPlusRGBAndDepth:
                        _pt.SetBoth(true, true);
                        break;
                }
                LogFile.WriteLine("[RESET] Passthrough restored to scenario.");
            }

            resetCount += 1;
            lastResetAtSeconds = tNow;

            LogFile.WriteLine($"[RESET] Complete. count={resetCount} lastAt={lastResetAtSeconds:F2}s");
            Debug.Log($"[StabilityTest] RESET complete. total={resetCount}");
        }

        static XRHandSubsystem GetHandSubsystem()
        {
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            return subsystems.Count > 0 ? subsystems[0] : null;
        }
    }
}
