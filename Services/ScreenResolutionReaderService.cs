using AppChangeResolutionOnWindows.Models;
using System.Runtime.InteropServices;

namespace AppChangeResolutionOnWindows.Services;

public interface IScreenResolutionReaderService
{
    IReadOnlyList<MonitorResolutionInfo> GetMonitorResolutions();
}

public sealed class ScreenResolutionReaderService : IScreenResolutionReaderService
{
    public IReadOnlyList<MonitorResolutionInfo> GetMonitorResolutions()
    {
        List<MonitorResolutionInfo> monitors = [];

        for (uint displayIndex = 0; ; displayIndex++)
        {
            DisplayNativeMethods.DISPLAY_DEVICE displayDevice = CreateDisplayDevice();

            if (!DisplayNativeMethods.EnumDisplayDevices(null, displayIndex, ref displayDevice, 0))
            {
                break;
            }

            if ((displayDevice.StateFlags & DisplayNativeMethods.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) == 0)
            {
                continue;
            }

            DisplayNativeMethods.DEVMODE currentMode = CreateDevMode();
            if (!DisplayNativeMethods.EnumDisplaySettingsEx(displayDevice.DeviceName, DisplayNativeMethods.ENUM_CURRENT_SETTINGS, ref currentMode, 0))
            {
                continue;
            }

            HashSet<DisplayResolution> resolutionSet = [];

            for (int modeIndex = 0; ; modeIndex++)
            {
                DisplayNativeMethods.DEVMODE mode = CreateDevMode();

                if (!DisplayNativeMethods.EnumDisplaySettingsEx(displayDevice.DeviceName, modeIndex, ref mode, 0))
                {
                    break;
                }

                resolutionSet.Add(new DisplayResolution(mode.dmPelsWidth, mode.dmPelsHeight));
            }

            List<DisplayResolution> resolutions =
            [
                .. resolutionSet
                    .Where(r => r.Width >= 1920)
                    .OrderByDescending(r => r.Width)
                    .ThenByDescending(r => r.Height)
            ];

            monitors.Add(new MonitorResolutionInfo
            {
                DeviceName = displayDevice.DeviceName,
                DisplayName = string.IsNullOrWhiteSpace(displayDevice.DeviceString) ? displayDevice.DeviceName : displayDevice.DeviceString,
                CurrentResolution = new DisplayResolution(currentMode.dmPelsWidth, currentMode.dmPelsHeight),
                AvailableResolutions = resolutions
            });
        }

        return monitors;
    }

    private static DisplayNativeMethods.DISPLAY_DEVICE CreateDisplayDevice() =>
        new()
        {
            cb = Marshal.SizeOf<DisplayNativeMethods.DISPLAY_DEVICE>()
        };

    private static DisplayNativeMethods.DEVMODE CreateDevMode() =>
        new()
        {
            dmSize = (short)Marshal.SizeOf<DisplayNativeMethods.DEVMODE>()
        };
}
