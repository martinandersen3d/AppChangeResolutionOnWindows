namespace AppChangeResolutionOnWindows.Models;

public sealed class MonitorResolutionInfo
{
    public required string DeviceName { get; init; }

    public required string DisplayName { get; init; }

    public required DisplayResolution CurrentResolution { get; init; }

    public required IReadOnlyList<DisplayResolution> AvailableResolutions { get; init; }
}
