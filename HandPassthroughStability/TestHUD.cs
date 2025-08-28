// Assets/HandPassthroughStability/Runtime/UI/TestHUD.cs
// HUD with robust XRHands resolution + OVR fallback + B (right) toggle

using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR.Management;
using System.Collections.Generic;

namespace HPS.Stability.UI
{
    public class TestHUD : MonoBehaviour
    {
        [Header("Refs")]
        public TextMeshProUGUI output;
        public HPS.Stability.StabilityTestRunner runner;
        public CanvasGroup canvasGroup;

        [Header("Options")]
        public float refreshInterval = 0.25f;
        public bool showMemory = true;
        public bool showFps = true;
        public bool startVisible = true;

        // Counters
        int _warnCount, _errorCount, _flagCount, _nanCount;
        string _lastFlagMsg = "-";
        float _lastFlagAt = -1f;

        // Timing
        float _accum, _fps, _nextRefresh, _sessionStart;

        // XR Hands
        XRHandSubsystem _xrHands;

        // OVR fallback
#if USE_OCULUS_INTEGRATION
        OVRHand _ovrLeft, _ovrRight;
#endif

        // XR input (generic) for B button
        InputDevice _rightHand;
        bool _hadRightHand, _prevSecondaryPressed;

        // Visibility
        bool _visible;

        void Awake()
        {
            if (!output)
            {
                output = FindObjectOfType<TextMeshProUGUI>();
                if (!output) Debug.LogWarning("[TestHUD] No TMP Text assigned.");
            }
            _visible = startVisible;
            ApplyVisibility();
        }

        void OnEnable()
        {
            Application.logMessageReceived += OnLog;
            _sessionStart = Time.realtimeSinceStartup;
            if (!runner) runner = FindObjectOfType<HPS.Stability.StabilityTestRunner>();

            TryResolveXRHands();
#if USE_OCULUS_INTEGRATION
            if (!_ovrLeft || !_ovrRight)
            {
                var hands = FindObjectsOfType<OVRHand>(true);
                foreach (var h in hands)
                {
                    if (h.HandType == OVRHand.Hand.HandLeft) _ovrLeft = h;
                    else if (h.HandType == OVRHand.Hand.HandRight) _ovrRight = h;
                }
            }
#endif
            TryGetRightHandDevice();
        }

        void OnDisable()
        {
            Application.logMessageReceived -= OnLog;
        }

        void Update()
        {
            // Allow late init: if subsystem not found at start, keep trying for a bit
            if (_xrHands == null || !_xrHands.running)
                TryResolveXRHands();

            // B button toggle
            if (GetRightBPressedThisFrame())
            {
                _visible = !_visible;
                ApplyVisibility();
            }

            // FPS
            if (showFps)
            {
                _accum += (Time.timeScale / Time.unscaledDeltaTime);
                if (Time.frameCount % 10 == 0) { _fps = _accum / 10f; _accum = 0f; }
            }

            // Throttle HUD rebuild
            if (Time.realtimeSinceStartup < _nextRefresh) return;
            _nextRefresh = Time.realtimeSinceStartup + refreshInterval;

            if (!output || !_visible) return;

            var sb = new StringBuilder(512);

            // Header / scenario
            var scenario = runner ? runner.scenario.ToString() : "(no runner)";
            sb.AppendLine("<b>Quest Hand+Passthrough Test HUD</b>");
            sb.AppendLine($"Scenario: <b>{scenario}</b>");

            // Timers
            var elapsed = Time.realtimeSinceStartup - _sessionStart;
            var runTarget = runner ? runner.runMinutes * 60f : 0f;
            sb.AppendLine($"Elapsed: {ToHms(elapsed)}  /  Target: {(runTarget > 0 ? ToHms(runTarget) : "n/a")}");

            // Soft reset info
            if (runner && runner.enableSoftReset && runner.resetEveryMinutes > 0.01f)
            {
                var cadence = runner.resetEveryMinutes * 60f;
                var nextK = Mathf.FloorToInt(elapsed / cadence) + 1;
                var eta = Mathf.Max(0f, nextK * cadence - elapsed);
                sb.AppendLine($"Soft Reset: every {runner.resetEveryMinutes:0}m  |  Next in: {ToHms(eta)}");
            }
            else sb.AppendLine("Soft Reset: <i>disabled</i>");

            // XR Hands state or OVR fallback
            if (_xrHands != null)
            {
                sb.AppendLine($"XR Hands Running: {(_xrHands.running ? "<color=#5CFFA8>Yes</color>" : "<color=#FFA85C>No</color>")}");
            }
            else
            {
#if USE_OCULUS_INTEGRATION
                bool l = _ovrLeft && _ovrLeft.IsTracked;
                bool r = _ovrRight && _ovrRight.IsTracked;
                sb.AppendLine($"XR Hands: <color=#FFA85C>Not found</color>  |  OVR L:{(l?"<color=#5CFFA8>On</color>":"Off")} R:{(r?"<color=#5CFFA8>On</color>":"Off")}");
#else
                sb.AppendLine("XR Hands: <color=#FFA85C>Not found</color>");
#endif
            }

            // Errors
            sb.AppendLine($"Warnings: {_warnCount}   Errors: {_errorCount}   Flags: {_flagCount}");
            sb.AppendLine($"NaN Poses: <b>{_nanCount}</b>");
            sb.AppendLine($"Last Flag: {(string.IsNullOrEmpty(_lastFlagMsg) ? "-" : _lastFlagMsg)}");
            if (_lastFlagAt >= 0f) sb.AppendLine($"Last Flag @ {ToHms(_lastFlagAt)}");

            // Memory
            if (showMemory)
            {
                var alloc = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
                var reserved = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);
                var unused = Profiler.GetTotalUnusedReservedMemoryLong() / (1024f * 1024f);
                sb.AppendLine($"Mem(MB): alloc {alloc:F1} | reserved {reserved:F1} | unused {unused:F1}");
            }

            // FPS
            if (showFps) sb.AppendLine($"FPS: {_fps:F1}");

            output.text = sb.ToString();
        }

        // ---------- helpers ----------

        void ApplyVisibility()
        {
            if (canvasGroup)
            {
                canvasGroup.alpha = _visible ? 1f : 0f;
                canvasGroup.interactable = _visible;
                canvasGroup.blocksRaycasts = _visible;
            }
            if (output) output.gameObject.SetActive(_visible);
        }

        void TryResolveXRHands()
        {
            // 1) XR Management active loader path (preferred)
            var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
            if (loader != null)
            {
                var viaLoader = loader.GetLoadedSubsystem<XRHandSubsystem>();
                if (viaLoader != null)
                {
                    _xrHands = viaLoader;
                    return;
                }
            }

            // 2) SubsystemManager path
            var list = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(list);
            if (list.Count > 0) { _xrHands = list[0]; return; }

            // (No manual Create(); avoid owning a separate instance)
            _xrHands = null;
        }

        // Right B button (OVR or generic XR)
        bool GetRightBPressedThisFrame()
        {
#if USE_OCULUS_INTEGRATION
            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch)) return true;
#endif
            if (!_hadRightHand || !_rightHand.isValid) TryGetRightHandDevice();

            if (_rightHand.isValid &&
                _rightHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool pressed))
            {
                bool edge = pressed && !_prevSecondaryPressed;
                _prevSecondaryPressed = pressed;
                return edge;
            }

            _prevSecondaryPressed = false;
            return false;
        }

        void TryGetRightHandDevice()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
            if (devices.Count > 0) { _rightHand = devices[0]; _hadRightHand = true; }
        }

        void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Warning) _warnCount++;
            else if (type == LogType.Error || type == LogType.Exception) _errorCount++;

            bool flagged = false;
            if (condition.Contains("HANDTRACKING NaN") || condition.Contains("[HAND_NaN]"))
            { _nanCount++; flagged = true; }

            if (condition.Contains("FrameSetCollator") ||
                condition.Contains("deadlineMissed") ||
                condition.Contains("muxModeMisMatch") ||
                condition.Contains("releasing incomplete frame set") ||
                condition.Contains("onFrameSetAvailable"))
            { flagged = true; }

            if (flagged)
            {
                _flagCount++;
                _lastFlagMsg = Trim(condition, 140);
                _lastFlagAt = Time.realtimeSinceStartup - _sessionStart;
            }
        }

        static string ToHms(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int s = Mathf.FloorToInt(seconds);
            int h = s / 3600;
            int m = (s % 3600) / 60;
            int ss = s % 60;
            return h > 0 ? $"{h:D2}:{m:D2}:{ss:D2}" : $"{m:D2}:{ss:D2}";
        }

        static string Trim(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "-";
            if (s.Length <= max) return s;
            return s.Substring(0, max - 3) + "...";
        }
    }
}
