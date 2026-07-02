using AppChangeResolutionOnWindows.Models;
using System.ComponentModel;
using System.Drawing;

namespace AppChangeResolutionOnWindows.Services;

public interface ITrayWinFormsService : IDisposable
{
    void Start();
}

public sealed class TrayWinFormsService : ITrayWinFormsService
{
    private readonly Form _hostForm;
    private readonly IScreenResolutionReaderService _readerService;
    private readonly IScreenResolutionSetterService _setterService;

    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly Icon _trayIcon;
    private readonly bool _ownsTrayIcon;

    public TrayWinFormsService(Form hostForm, IScreenResolutionReaderService readerService, IScreenResolutionSetterService setterService)
    {
        _hostForm = hostForm;
        _readerService = readerService;
        _setterService = setterService;

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Opening += ContextMenu_Opening;

        (_trayIcon, _ownsTrayIcon) = LoadTrayIcon();

        _notifyIcon = new NotifyIcon
        {
            Text = "Screen Resolution",
            Icon = _trayIcon,
            ContextMenuStrip = _contextMenu,
            Visible = false
        };

        _notifyIcon.MouseUp += NotifyIcon_MouseUp;
    }

    public void Start()
    {
        BuildMenu();
        _notifyIcon.Visible = true;
    }

    private void ContextMenu_Opening(object? sender, CancelEventArgs e)
    {
        BuildMenu();
    }

    private void NotifyIcon_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            BuildMenu();
            _contextMenu.Show(Cursor.Position);
        }
    }

    private void BuildMenu()
    {
        _contextMenu.SuspendLayout();
        _contextMenu.Items.Clear();

        IReadOnlyList<MonitorResolutionInfo> monitors = _readerService.GetMonitorResolutions();

        if (monitors.Count == 0)
        {
            _contextMenu.Items.Add(new ToolStripMenuItem("No monitors detected") { Enabled = false });
        }
        else
        {
            foreach (MonitorResolutionInfo monitor in monitors)
            {
                ToolStripMenuItem monitorItem = new(monitor.DisplayName);

                foreach (DisplayResolution resolution in monitor.AvailableResolutions)
                {
                    bool isCurrent = resolution == monitor.CurrentResolution;
                    ToolStripMenuItem resolutionItem = new(resolution.ToString())
                    {
                        Checked = isCurrent,
                        Tag = resolution
                    };

                    resolutionItem.Click += (_, _) => ApplyResolution(monitor.DeviceName, resolution);
                    monitorItem.DropDownItems.Add(resolutionItem);
                }

                if (monitor.AvailableResolutions.Count == 0)
                {
                    monitorItem.DropDownItems.Add(new ToolStripMenuItem("No resolutions available") { Enabled = false });
                }

                _contextMenu.Items.Add(monitorItem);
            }
        }

        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(new ToolStripMenuItem("Refresh", null, (_, _) => BuildMenu()));
        _contextMenu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => ExitApplication()));

        _contextMenu.ResumeLayout();
    }

    private void ApplyResolution(string deviceName, DisplayResolution resolution)
    {
        if (_setterService.TrySetResolution(deviceName, resolution, out string? errorMessage))
        {
            BuildMenu();
            _notifyIcon.ShowBalloonTip(1500, "Screen Resolution", $"Changed to {resolution}", ToolTipIcon.Info);
            return;
        }

        _notifyIcon.ShowBalloonTip(2000, "Screen Resolution", errorMessage ?? "Could not change resolution.", ToolTipIcon.Error);
    }

    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        _hostForm.Close();
    }

    public void Dispose()
    {
        _notifyIcon.MouseUp -= NotifyIcon_MouseUp;
        _contextMenu.Opening -= ContextMenu_Opening;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();

        if (_ownsTrayIcon)
        {
            _trayIcon.Dispose();
        }

        _contextMenu.Dispose();
    }

    private static (Icon Icon, bool OwnsIcon) LoadTrayIcon()
    {
        string iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        if (File.Exists(iconPath))
        {
            return (new Icon(iconPath), true);
        }

        return (SystemIcons.Application, false);
    }
}
