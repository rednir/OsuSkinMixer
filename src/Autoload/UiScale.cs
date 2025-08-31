using Godot;
using OsuSkinMixer.Statics;
using System;

namespace OsuSkinMixer.Autoload;

public partial class UiScale : Node
{
    public readonly Vector2I DesignResolution = new(1920, 1080);

    public readonly Vector2I BaseWindowSize = new(750, 500);

    public float? UserUiScale = null;

    private int? _lastScreen = null;

    public override void _Process(double delta)
    {
        int currentScreen = DisplayServer.WindowGetCurrentScreen();
        if (_lastScreen != currentScreen)
        {
            _lastScreen = currentScreen;
            ApplyUiScale();
            ApplyWindowSize();
        }
    }

    private void ApplyUiScale()
    {
        var window = GetWindow();

        window.ContentScaleSize = DesignResolution;
        window.ContentScaleFactor = UserUiScale ?? ComputeAutoScaleFromDpi();

        Settings.Log($"UI scale set to {window.ContentScaleFactor * 100.0f}%");
    }

    private static float ComputeAutoScaleFromDpi()
    {
        int screen = DisplayServer.WindowGetCurrentScreen();
        float dpi = DisplayServer.ScreenGetDpi(screen);

        Settings.Log($"Computing scale for screen {screen} with DPI {dpi}");

        // Some platforms/drivers can report junk (0 or absurd values).
        if (dpi <= 0 || dpi > 10000)
        {
            // Fallback: heuristic by resolution.
            Vector2I s = DisplayServer.ScreenGetSize(screen);

            if (s.X >= 3840 && s.Y >= 2160)
                return 1.5f;

            if (s.X >= 2560 && s.Y >= 1440)
                return 1.25f;

            return 1.0f;
        }

        // Convert physical DPI to a UI scale. 96 DPI ≈ 100% on Windows.
        float raw = dpi / 96.0f;

        // Round to nice steps to avoid awkward fractions.
        float rounded = Mathf.Clamp(Mathf.Round(raw * 4f) / 4f, 0.75f, 3.0f);
        return rounded;
    }

    private void ApplyWindowSize()
    {
        var window = GetWindow();

        Vector2I target = new(
            Mathf.RoundToInt(BaseWindowSize.X * window.ContentScaleFactor),
            Mathf.RoundToInt(BaseWindowSize.Y * window.ContentScaleFactor)
        );

        Rect2I usable = DisplayServer.ScreenGetUsableRect(window.CurrentScreen);
        target.X = Mathf.Min(target.X, usable.Size.X);
        target.Y = Mathf.Min(target.Y, usable.Size.Y);

        window.Size = target;
    }
}
