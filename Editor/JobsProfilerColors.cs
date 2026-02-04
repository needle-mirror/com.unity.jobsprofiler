using UnityEngine;

/// <summary>
/// Simple color constants for the Jobs Profiler. This class has no static constructor
/// and can be safely used from Burst-compiled code.
/// </summary>
internal static class JobsProfilerColors
{
    // Semantic colors (same in both themes - these convey meaning)
    internal static readonly Color32 ScheduleColor = new Color32(114, 114, 255, 255);
    internal static readonly Color StripeColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);
    internal static readonly Color CompletedWaitColor = Color.red;
    internal static readonly Color DependencyColor = Color.yellow;
}
