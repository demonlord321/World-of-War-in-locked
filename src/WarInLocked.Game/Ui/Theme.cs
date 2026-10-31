using Microsoft.Xna.Framework;

namespace WarInLocked.Game.Ui;

/// <summary>Colours shared by every screen. Change them here to restyle the game.</summary>
public static class Theme
{
    public static readonly Color Background = new(20, 22, 30);
    public static readonly Color Panel = new(34, 38, 52);
    public static readonly Color PanelBorder = new(70, 76, 100);
    public static readonly Color Text = new(235, 235, 240);
    public static readonly Color TextDim = new(150, 154, 170);
    public static readonly Color Accent = new(224, 168, 64);
    public static readonly Color Positive = new(110, 205, 120);
    public static readonly Color Negative = new(230, 100, 100);
    public static readonly Color Button = new(52, 58, 82);
    public static readonly Color ButtonHover = new(72, 80, 112);
    public static readonly Color ButtonDisabled = new(36, 38, 48);
}
