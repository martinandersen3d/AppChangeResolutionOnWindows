using AppChangeResolutionOnWindows.Models;
using System.Runtime.InteropServices;

namespace AppChangeResolutionOnWindows.Services;

public interface IScreenResolutionSetterService
{
    bool TrySetResolution(string deviceName, DisplayResolution resolution, out string? errorMessage);
}

public sealed class ScreenResolutionSetterService : IScreenResolutionSetterService
{
    public bool TrySetResolution(string deviceName, DisplayResolution resolution, out string? errorMessage)
    {
        DisplayNativeMethods.DEVMODE mode = new()
        {
            dmSize = (short)Marshal.SizeOf<DisplayNativeMethods.DEVMODE>()
        };

        if (!DisplayNativeMethods.EnumDisplaySettingsEx(deviceName, DisplayNativeMethods.ENUM_CURRENT_SETTINGS, ref mode, 0))
        {
            errorMessage = "Could not read current display settings.";
            return false;
        }

        mode.dmPelsWidth = resolution.Width;
        mode.dmPelsHeight = resolution.Height;
        mode.dmFields = DisplayNativeMethods.DM_PELSWIDTH | DisplayNativeMethods.DM_PELSHEIGHT;

        int result = DisplayNativeMethods.ChangeDisplaySettingsEx(deviceName, ref mode, IntPtr.Zero, 0, IntPtr.Zero);
        if (result == DisplayNativeMethods.DISP_CHANGE_SUCCESSFUL)
        {
            errorMessage = null;
            return true;
        }

        errorMessage = $"Display change failed with code {result}.";
        return false;
    }
}
