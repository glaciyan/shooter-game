using Godot;
using shootergame.player.script;

namespace shootergame.player;

public partial class Smoothing : Node3D
{
    [Export]
    public PlayerCharacter Target;

    private Vector3 _currentPosition;
    private Vector3 _oldPosition;

    public override void _Ready()
    {
        ProcessPriority = 100;
        TopLevel = true;
        Engine.PhysicsJitterFix = 0.0;
        _oldPosition = Position;
    }

    private const float FollowSpeed = 25.0f;

    private bool _enabled = true;
    private bool _ySmoothing = true;

    public override void _Process(double delta)
    {
        if (!_enabled) return;

        var fraction = Engine.GetPhysicsInterpolationFraction();

        var diff = _currentPosition - _oldPosition;

        // var lerped = Position.Lerp(_currentPosition, (float)delta * FollowSpeed);

        var physicsSmoothed = _oldPosition + diff * (float)fraction;

        // if (Target.IsOnFloor() || Target.SteppedDownStair)
        //     EnableYSmoothing();
        // else
        //     DisableYSmoothing();
        //
        // if (_ySmoothing)
        //     physicsSmoothed.Y = lerped.Y;

        Position = physicsSmoothed;
    }

    private void DisableYSmoothing()
    {
        _ySmoothing = false;
    }

    private void EnableYSmoothing()
    {
        if (_ySmoothing) return;
        _ySmoothing = true;
        var p = Position;
        Position = new Vector3(p.X, _currentPosition.Y, p.Z);
    }

    public override void _PhysicsProcess(double delta)
    {
        _oldPosition = _currentPosition;
        _currentPosition = Target.GlobalPosition;
    }
}