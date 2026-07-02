using System.Diagnostics;
using System.Text;

namespace AppChangeResolutionOnWindows.Services;

public interface ITeamsMeetingWindowDetectorService : IDisposable
{
    event Action<int>? MeetingWindowDetected;

    void Start();

    void Stop();
}

public sealed class TeamsMeetingWindowDetectorService : ITeamsMeetingWindowDetectorService
{
    private const string TeamsWindowClassName = "TeamsWebView";
    private const string MeetingTitlePrefix = "Meeting with";

    private readonly System.Windows.Forms.Timer _timer;
    private string? _lastTriggeredMeetingTitle;

    public event Action<int>? MeetingWindowDetected;

    public TeamsMeetingWindowDetectorService()
    {
        _timer = new System.Windows.Forms.Timer
        {
            Interval = 1000
        };

        _timer.Tick += Timer_Tick;
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        IntPtr foregroundWindow = WindowNativeMethods.GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
        {
            return;
        }

        StringBuilder classNameBuilder = new(256);
        if (WindowNativeMethods.GetClassName(foregroundWindow, classNameBuilder, classNameBuilder.Capacity) <= 0)
        {
            return;
        }

        if (!string.Equals(classNameBuilder.ToString(), TeamsWindowClassName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        StringBuilder titleBuilder = new(512);
        if (WindowNativeMethods.GetWindowText(foregroundWindow, titleBuilder, titleBuilder.Capacity) <= 0)
        {
            return;
        }

        if (!titleBuilder.ToString().StartsWith(MeetingTitlePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string meetingTitle = titleBuilder.ToString();
        if (string.Equals(meetingTitle, _lastTriggeredMeetingTitle, StringComparison.Ordinal))
        {
            return;
        }

        WindowNativeMethods.GetWindowThreadProcessId(foregroundWindow, out uint processIdValue);
        if (processIdValue == 0)
        {
            return;
        }

        int processId = (int)processIdValue;

        Process? process;
        try
        {
            process = Process.GetProcessById(processId);
        }
        catch
        {
            return;
        }

        string processName = process.ProcessName;
        if (!string.Equals(processName, "ms-teams", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(processName, "ms-teams.exe", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _lastTriggeredMeetingTitle = meetingTitle;
        MeetingWindowDetected?.Invoke(processId);
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= Timer_Tick;
        _timer.Dispose();
    }
}
