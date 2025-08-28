// Assets/HandPassthroughStability/Runtime/Passthrough/OculusIntegrationPassthroughAdapter.cs
#define USE_OCULUS_INTEGRATION // <-- remove or keep depending on your project

using UnityEngine;

namespace HPS.Stability.Passthrough
{
#if USE_OCULUS_INTEGRATION
    public class OculusIntegrationPassthroughAdapter : IPassthroughAdapter
    {
        private readonly OVRPassthroughLayer _layer;

        public OculusIntegrationPassthroughAdapter()
        {
            _layer = Object.FindObjectOfType<OVRPassthroughLayer>();
        }

        public bool IsAvailable => _layer != null;

        public void SetRGB(bool enabled)
        {
            if (!_layer) return;
            _layer.hidden = !enabled;
            Debug.Log($"[OVR Passthrough] RGB -> {enabled}");
        }

        public void SetDepth(bool enabled)
        {
            if (!_layer) return;
            _layer.edgeRenderingEnabled = enabled; // NOTE: depth is limited in OVR; adjust per your use
            Debug.Log($"[OVR Passthrough] (sim) Depth -> {enabled}");
        }

        public void SetBoth(bool rgbEnabled, bool depthEnabled)
        {
            SetRGB(rgbEnabled);
            SetDepth(depthEnabled);
        }

        public void ShutdownAll()
        {
            SetBoth(false, false);
        }
    }
#else
    public class OculusIntegrationPassthroughAdapter : IPassthroughAdapter
    {
        public bool IsAvailable => false;
        public void SetRGB(bool enabled) { }
        public void SetDepth(bool enabled) { }
        public void SetBoth(bool rgbEnabled, bool depthEnabled) { }
        public void ShutdownAll() { }
    }
#endif
}
