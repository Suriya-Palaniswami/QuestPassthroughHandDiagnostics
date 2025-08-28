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

        XRHandSubsystem _hands;
        IPassthroughAdapter _pt;

        Coroutine _loop;

        void Awake()
        {
            LogFile.Init("quest_hand_passthrough");
            LogFile.WriteLine($"[TEST] Scenario={scenario} warmup={warmupMinutes}m run={runMinutes}m softReset={enableSoftReset} every={resetEveryMinutes}m");
        }

        void OnEnable()
        {
            // Subsystems
            _hands = GetHandSubsystem();

            // Choose passthrough adapter (OpenXR Meta vs Oculus Integration).
            if (preferOculusIntegrationAdapter)
                _pt = new OculusIntegrationPassthroughAdapter();
            else
                _pt = new OpenXRMetaPassthroughAdapter();

            if (_hands==null) Debug.LogWarning("[StabilityTest] XRHandSubsystem not found.");
            if (_pt == null || !_pt.IsAvailable) Debug.LogWarning("[StabilityTest] Passthrough adapter not available (RGB/Depth controls may be no-op).");

            _loop = StartCoroutine(RunTest());
        }

        void OnDisable()
        {
            if (_loop != null) StopCoroutine(_loop);
            // Shutdown passthrough on exit
            _pt?.ShutdownAll();
        }

        IEnumerator RunTest()
        {
            yield return StartCoroutine(ApplyScenarioAndWarmup());

            float elapsed = 0f;
            float nextReset = resetEveryMinutes * 60f;
            float total = runMinutes * 60f;

            LogFile.WriteLine("[TEST] Entering main run…");

            while (elapsed < total)
            {
                // Soft reset cadence
                if (enableSoftReset && elapsed >= nextReset)
                {
                    yield return StartCoroutine(DoSoftReset());
                    nextReset += resetEveryMinutes * 60f;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            LogFile.WriteLine("[TEST] Completed run.");
        }

        IEnumerator ApplyScenarioAndWarmup()
        {
            LogFile.WriteLine("[TEST] Applying scenario & warmup…");

            // Ensure hands running
            if (_hands != null && !_hands.running)
            {
                LogFile.WriteLine("[TEST] Starting XRHandSubsystem…");
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

            // Warmup
            yield return new WaitForSeconds(warmupMinutes * 60f);
            LogFile.WriteLine("[TEST] Warmup complete.");
        }

        IEnumerator DoSoftReset()
        {
            LogFile.WriteLine($"[RESET] Initiating soft reset (hands={resetHands}, passthrough={resetPassthrough})…");

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
                // Quick toggle: off then back to scenario state
                LogFile.WriteLine("[RESET] Toggling passthrough off…");
                _pt.ShutdownAll();
                yield return new WaitForSeconds(0.25f);

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

            LogFile.WriteLine("[RESET] Complete.");
            yield return null;
        }

        static XRHandSubsystem GetHandSubsystem()
        {
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            return subsystems.Count > 0 ? subsystems[0] : null;
        }
    }
}
