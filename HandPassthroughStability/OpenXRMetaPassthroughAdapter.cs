// Assets/HandPassthroughStability/Runtime/Passthrough/OpenXRMetaPassthroughAdapter.cs
#define USE_META_OPENXR // <-- remove or keep depending on your project

using UnityEngine;

namespace HPS.Stability.Passthrough
{
#if USE_META_OPENXR
    // Replace these with your actual Meta XR Core passthrough controller calls.
    // Many teams wrap Meta's OpenXR feature in a scene component for runtime toggles.
    public class OpenXRMetaPassthroughAdapter : IPassthroughAdapter
    {
        // Example: a MonoBehaviour in your scene that knows how to enable/disable passthrough
        private readonly MetaPassthroughController _ctrl;

        public OpenXRMetaPassthroughAdapter()
        {
            _ctrl = Object.FindObjectOfType<MetaPassthroughController>();
        }

        public bool IsAvailable => _ctrl != null;

        public void SetRGB(bool enabled)
        {
            if (!_ctrl) return;
            _ctrl.EnableRGB(enabled);
        }

        public void SetDepth(bool enabled)
        {
            if (!_ctrl) return;
            _ctrl.EnableDepth(enabled);
        }

        public void SetBoth(bool rgbEnabled, bool depthEnabled)
        {
            if (!_ctrl) return;
            _ctrl.EnableRGB(rgbEnabled);
            _ctrl.EnableDepth(depthEnabled);
        }

        public void ShutdownAll()
        {
            if (!_ctrl) return;
            _ctrl.EnableRGB(false);
            _ctrl.EnableDepth(false);
        }
    }

    // Example placeholder controller you can implement in your project:
    // Attach to a GameObject and wire up actual Meta XR OpenXR feature toggles here.
    public class MetaPassthroughController : MonoBehaviour
    {
        public void EnableRGB(bool on)
        {
            Debug.Log($"[MetaPassthrough] RGB -> {on}");
            // TODO: Call Meta OpenXR feature to toggle RGB passthrough rendering
        }

        public void EnableDepth(bool on)
        {
            Debug.Log($"[MetaPassthrough] Depth -> {on}");
            // TODO: Call Meta OpenXR feature to toggle depth usage (if exposed)
        }
    }
#else
    public class OpenXRMetaPassthroughAdapter : IPassthroughAdapter
    {
        public bool IsAvailable => false;
        public void SetRGB(bool enabled) { }
        public void SetDepth(bool enabled) { }
        public void SetBoth(bool rgbEnabled, bool depthEnabled) { }
        public void ShutdownAll() { }
    }
#endif
}
