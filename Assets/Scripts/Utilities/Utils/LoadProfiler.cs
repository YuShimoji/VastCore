using UnityEngine;
using UnityEngine.Profiling;
using System.Diagnostics;

namespace Vastcore.Utilities
{
    public static class LoadProfiler
    {
        public struct Scope : System.IDisposable
        {
            private readonly string _label;
            private readonly long _startTotalAllocated;
            private readonly Stopwatch _stopwatch;

            public Scope(string label)
            {
                _label = string.IsNullOrEmpty(label) ? "(unnamed)" : label;
                _startTotalAllocated = Profiler.GetTotalAllocatedMemoryLong();
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                long endAllocated = Profiler.GetTotalAllocatedMemoryLong();
                long deltaBytes = endAllocated - _startTotalAllocated;
                float ms = (float)_stopwatch.Elapsed.TotalMilliseconds;
                UnityEngine.Debug.Log($"[LoadProfiler] {_label} took {ms:F2} ms, ΔAlloc {deltaBytes / 1024f:F1} KB");
            }
        }

        public static Scope Measure(string label) => new Scope(label);
    }
}
