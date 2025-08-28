# Quest Passthrough + Hand Tracking Diagnostics

This Unity package helps developers **diagnose, reproduce, and mitigate** long-session stability issues when using **Passthrough** and **Hand Tracking** together on Meta Quest devices.

It provides:

- ✅ File logging (`/Android/data/<package>/files/logs/`)
- ✅ NaN hand pose detection
- ✅ FrameSetCollator error detection
- ✅ Memory usage monitoring
- ✅ Real-time HUD (TextMeshPro UGUI, toggle with B button)
- ✅ Automatic soft resets (hands + passthrough)
- ✅ Editor tools for convenience

---


## 📜 Scripts Included

- **LogFile.cs** – central logger writing timestamped logs.
- **FrameLogHook.cs** – hooks Unity logs, flags FrameSetCollator + NaN issues.
- **ResourceMonitor.cs** – samples Unity memory usage periodically.
- **HandPoseNaNGuard.cs** – checks XRHands joint poses for NaN values.
- **IPassthroughAdapter.cs** – abstraction for passthrough control.
- **OpenXRMetaPassthroughAdapter.cs** – passthrough adapter for Meta XR Core OpenXR.
- **OculusIntegrationPassthroughAdapter.cs** – passthrough adapter for Oculus Integration (OVRPassthroughLayer).
- **StabilityTestRunner.cs** – orchestrates test scenarios (Hand only, Hand+Depth, Hand+RGB, Hand+Both), warmup + soft resets.  
  - Tracks reset count + last reset time.
- **StabilityTestWindow.cs** – Unity Editor window to view scenario info and open log folder.
- **TestHUD.cs** – TMP UGUI HUD overlay for live in-headset metrics. Toggle with **Right Controller B**. Shows reset info, warnings, NaN count, memory, and FPS.
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
4. Configure passthrough stack:
   - **Oculus Integration**:  
     - Add an `OVRPassthroughLayer` to your rig.  
     - Define `USE_OCULUS_INTEGRATION` in **Player → Scripting Define Symbols**.  
     - Tick “Prefer Oculus Integration Adapter” on the runner.
   - **Meta XR Core OpenXR**:  
     - Implement `MetaPassthroughController` in your scene.  
     - Define `USE_META_OPENXR`.  
5. Build & run on Quest.  
6. Interact with hands + passthrough for **long sessions (30–90 min)**.  
7. Toggle HUD with **Right Controller B** to see live stats (warnings, NaNs, memory, resets).  
8. Pull logs from the headset:

   ```bash
   adb pull "/sdcard/Android/data/<your.package.name>/files/logs" ./Logs
   ```

---

🧪 What You Get

Empirical logs of NaN hand poses + FrameSetCollator errors

Memory usage trends

Soft reset markers in both logs and HUD:
   ```bash

[RESET] Initiating soft reset (hands=True, passthrough=True) at 180.02s…
[RESET] Stopping XRHandSubsystem…
[RESET] Starting XRHandSubsystem…
[RESET] Toggling passthrough off…
[RESET] Passthrough restored to scenario.
[RESET] Complete. count=1 lastAt=180.02s
   ```
---


## 🚀 HUD display shows:

![com oculus vrshell-20250828-165720](https://github.com/user-attachments/assets/0ae1d8ca-fdf8-42e9-a707-0edaf2c126ca)

---

## ⚠️ Notes

During warmup, resets are not executed. They begin once you see:
```bash
[TEST] Warmup complete.
[TEST] Entering main run…
```

If you see [Warning] [StabilityTest] Passthrough adapter not available. at startup, that just means the adapter couldn’t find your passthrough component immediately. Once your OVRPassthroughLayer or MetaPassthroughController is active, resets will include passthrough successfully.

For quick verification, shorten intervals:
```bash

warmupMinutes = 0.05 (~3s)

resetEveryMinutes = 0.2 (~12s)
```
---
## 📝 License

MIT (or your choice)

##🙌 Credits

Created to help Quest developers systematically reproduce and demonstrate Passthrough + Hand Tracking stability issues, and to test workarounds like soft resets.
