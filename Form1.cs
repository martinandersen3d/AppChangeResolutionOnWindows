using AppChangeResolutionOnWindows.Services;

namespace AppChangeResolutionOnWindows;

public partial class Form1 : Form
{
    private readonly ITrayWinFormsService _trayService;

    public Form1()
    {
        InitializeComponent();

        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;

        _trayService = new TrayWinFormsService(
            this,
            new ScreenResolutionReaderService(),
            new ScreenResolutionSetterService());
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        Hide();
        _trayService.Start();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _trayService.Dispose();
        base.OnFormClosing(e);
    }
}
