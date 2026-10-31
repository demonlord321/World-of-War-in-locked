using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace WarInLocked.Game.Ui;

/// <summary>Keyboard, mouse and typed text for the current frame.</summary>
public sealed class InputState
{
    private KeyboardState _keys, _prevKeys;
    private MouseState _mouse, _prevMouse;
    private readonly List<char> _incoming = new();
    private readonly List<char> _typed = new();

    public Point MousePosition => _mouse.Position;
    public bool MouseMoved => _mouse.Position != _prevMouse.Position;

    /// <summary>True on the frame the left mouse button went down, until a widget calls <see cref="ConsumeClick"/>.</summary>
    public bool Clicked { get; private set; }

    public bool Shift => _keys.IsKeyDown(Keys.LeftShift) || _keys.IsKeyDown(Keys.RightShift);

    /// <summary>Characters typed this frame (includes '\b' for backspace).</summary>
    public IReadOnlyList<char> TypedChars => _typed;

    /// <summary>Called from the window's TextInput event.</summary>
    public void AddTypedChar(char c) => _incoming.Add(c);

    public void Update()
    {
        _prevKeys = _keys;
        _prevMouse = _mouse;
        _keys = Keyboard.GetState();
        _mouse = Mouse.GetState();
        Clicked = _mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
        _typed.Clear();
        _typed.AddRange(_incoming);
        _incoming.Clear();
    }

    /// <summary>True on the frame the key went down.</summary>
    public bool Pressed(Keys key) => _keys.IsKeyDown(key) && _prevKeys.IsKeyUp(key);

    /// <summary>True on the frame any key went down.</summary>
    public bool AnyKeyPressed()
    {
        foreach (var key in _keys.GetPressedKeys())
            if (_prevKeys.IsKeyUp(key)) return true;
        return false;
    }

    public void ConsumeClick() => Clicked = false;
}
