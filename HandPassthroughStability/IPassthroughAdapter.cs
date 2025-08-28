// Assets/HandPassthroughStability/Runtime/Passthrough/IPassthroughAdapter.cs
namespace HPS.Stability.Passthrough
{
    public interface IPassthroughAdapter
    {
        bool IsAvailable { get; }
        void SetRGB(bool enabled);
        void SetDepth(bool enabled);
        void SetBoth(bool rgbEnabled, bool depthEnabled);
        void ShutdownAll();
    }
}
