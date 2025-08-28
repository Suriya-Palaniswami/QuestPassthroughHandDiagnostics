// Assets/HandPassthroughStability/Runtime/Diagnostics/ResourceMonitor.cs
using System.Collections;
using UnityEngine;
using UnityEngine.Profiling;

namespace HPS.Stability.Diagnostics
{
    public class ResourceMonitor : MonoBehaviour
    {
        [Tooltip("Seconds between samples")]
        public float sampleInterval = 5f;

        Coroutine _loop;

        void OnEnable() => _loop = StartCoroutine(SampleLoop());
        void OnDisable()
        {
            if (_loop != null) StopCoroutine(_loop);
        }

        IEnumerator SampleLoop()
        {
            while (true)
            {
                var total = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
                var reserved = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);
                var unused = Profiler.GetTotalUnusedReservedMemoryLong() / (1024f * 1024f);

                LogFile.WriteLine($"[RES] memMB alloc={total:F1} reserved={reserved:F1} unused={unused:F1}");
                yield return new WaitForSeconds(sampleInterval);
            }
        }
    }
}
