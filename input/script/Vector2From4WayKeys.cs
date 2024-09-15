using Godot;
using shootergame.input.script.config;

namespace shootergame.input.script;

public class Vector2From4WayKeys : Vector2Binding
{
    public Vector2From4WayKeys(InputEventKey up, InputEventKey down, InputEventKey left, InputEventKey right)
    {
        _up = up;
        _down = down;
        _left = left;
        _right = right;
    }

    public Vector2From4WayKeys(Key4WayVector2Config config)
    {
        _up = config.Up;
        _down = config.Down;
        _left = config.Left;
        _right = config.Right;
    }

    private readonly InputEventKey _up;
    private readonly InputEventKey _down;
    private readonly InputEventKey _left;
    private readonly InputEventKey _right;

    private InputEventKey _lastUp;
    private InputEventKey _lastDown;
    private InputEventKey _lastLeft;
    private InputEventKey _lastRight;

    public bool HandleInput(InputEventKey keyEvent)
    {
        if (keyEvent.IsMatch(_up, false))
        {
            _lastUp = keyEvent;
            return true;
        }

        if (keyEvent.IsMatch(_down, false))
        {
            _lastDown = keyEvent;
            return true;
        }

        if (keyEvent.IsMatch(_left, false))
        {
            _lastLeft = keyEvent;
            return true;
        }

        if (keyEvent.IsMatch(_right, false))
        {
            _lastRight = keyEvent;
            return true;
        }

        return false;
    }

    public Vector2 ResolveInput()
    {
        var direction = new Vector2(
            (_lastUp.Pressed ? 1 : 0) - (_lastLeft.Pressed ? 1 : 0),
            (_lastDown.Pressed ? 1 : 0) - (_lastRight.Pressed ? 1 : 0)
        );

        return direction;
    }
}