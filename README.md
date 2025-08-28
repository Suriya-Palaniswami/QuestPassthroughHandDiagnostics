# Quest Passthrough + Hand Tracking Diagnostics

This Unity package helps developers **diagnose, reproduce, and mitigate** long-session stability issues when using **Passthrough** and **Hand Tracking** together on Meta Quest devices.

It provides:

- Logging to file (`/Android/data/<package>/files/logs/`)
- NaN hand pose detection
- FrameSetCollator error detection
- Memory usage monitoring
- Real-time HUD (TextMeshPro UGUI)
- Automatic soft reset system
- Editor tools for convenience

---

## 📦 Repository Name

**QuestPassthroughHandDiagnostics**

---

## 📜 Scripts Included

- **LogFile.cs** – central logger writing timestamped logs.
- **FrameLogHook.cs** – hooks Unity logs, flags FrameSetCollator + NaN issues.
- **ResourceMonitor.cs** – samples Unity memory usage periodically.
- **HandPoseNaNGuard.cs** – checks XRHands joint poses for NaN values.
- **IPassthroughAdapter.cs** – interface abstraction for passthrough control.
- **OpenXRMetaPassthroughAdapter.cs** – passthrough adapter for Meta XR Core OpenXR.
- **OculusIntegrationPassthroughAdapter.cs** – passthrough adapter for Oculus Integration (OVRPassthroughLayer).
- **StabilityTestRunner.cs** – orchestrates test scenarios (Hand only, Hand+Depth, Hand+RGB, Hand+Both), warmup + soft resets.
- **StabilityTestWindow.cs** – Unity Editor window to view settings and open log folder.
- **TestHUD.cs** – TextMeshPro HUD overlay for live in-headset metrics. Toggle with **Right Controller B**.
- **HeartbeatLogger.cs** – optional heartbeat writer to ensure logs are created.

---

## 🚀 How to Use

1. Import the package into Unity.
2. In your test scene, add an empty GameObject (e.g., `StabilityHarness`).
3. Attach the following components:
   - `StabilityTestRunner`
   - `FrameLogHook`
   - `ResourceMonitor`
   - `HandPoseNaNGuard`
   - `TestHUD` (assign TMP Text + optional CanvasGroup)
   - `HeartbeatLogger` (optional)
4. Configure your passthrough stack:
   - If using **Meta XR Core OpenXR**, enable `USE_META_OPENXR` and implement the adapter calls.
   - If using **Oculus Integration**, enable `USE_OCULUS_INTEGRATION` in **Player → Scripting Define Symbols**.
5. Build & run on Quest.
6. Interact with hands + passthrough for **30–90 min sessions**.
7. Toggle HUD with **Right Controller B**.
8. Pull logs from the headset:

   ```bash
   adb pull "/sdcard/Android/data/<your.package.name>/files/logs" ./Logs
