namespace AppChangeResolutionOnWindows.Models;

public sealed record DisplayResolution(int Width, int Height)
{
    public override string ToString() => $"{Width} x {Height}";
}
