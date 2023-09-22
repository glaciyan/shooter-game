using System;
using Godot;

namespace shootergame.player;

public partial class PlayerCharacter : CharacterBody3D
{
    [ExportGroup("Moving")]
    [Export]
    public float MaxVelocity = 5.0f;

    [Export]
    public float MovementForce = 2700.0f;

    [Export]
    public float Friction = 25.0f;

    [Export]
    public float MassKg = 60.0f;

    [ExportGroup("Jumping")]
    [Export]
    public float JumpForceN = 450.0f;

    [Export]
    public float TerminalVelocity = 55.0f;

    [Export]
    public float GravityMultiplier = 2.0f;

    [ExportSubgroup("Air Control")]
    [Export(PropertyHint.Range, "0, 1, 0.1")]
    public float AirSpeedControl = 1.0f;

    [Export(PropertyHint.Range, "0, 1, 0.1")]
    public float AirStrafeControl = 1.0f;

    [Export]
    public float AirFrictionFactor = 0.05f;

    [ExportSubgroup("")]
    [ExportGroup("Camera")]
    [Export(PropertyHint.Range, "0.1, 10, 0.1")]
    public float LookaroundSpeed = 1.0f;


    private const float LookaroundSpeedReduction = 0.002f;

    private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    private Vector2 _viewPoint = new(0.0f, 0.0f);

    private CameraController _cameraController;

    public override void _Ready()
    {
        _cameraController = GetNode<CameraController>("CameraController");
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseMotion mouseMotion) return;

        _viewPoint.X += mouseMotion.Relative.X * LookaroundSpeed * LookaroundSpeedReduction;
        _viewPoint.Y += mouseMotion.Relative.Y * LookaroundSpeed * LookaroundSpeedReduction;
        _viewPoint.Y = (float)Math.Clamp(_viewPoint.Y, -Math.PI / 2, Math.PI / 2);

        _cameraController.RotateTo(_viewPoint);
    }

    public override void _Process(double delta)
    {
        Velocity = Movement(delta, Gravity(delta, Jump(Velocity)));
        MoveAndSlide();
    }

    private Vector3 Movement(double delta, Vector3 velocity)
    {
        // GD.Print(-_camera.Transform.Basis.Z);
        var inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");

        var forwardVector = Vector2.Down.Rotated(_viewPoint.X);
        // using rotation matrix to rotate by 90
        var sidewaysVector = new Vector2(forwardVector.Y, -forwardVector.X);
        var forwardDirection = forwardVector * inputDir.Y;
        var sidewaysDirection = sidewaysVector * inputDir.X;
        var direction = (forwardDirection + sidewaysDirection).Normalized();

        DebugDraw3D.DrawRay(Position, new Vector3(direction.X, 0, direction.Y), 1, Colors.DarkCyan);

        var directionStrength = direction;
        if (!IsOnFloor())
        {
            directionStrength *= 0.2f;
        }

        DebugDraw3D.DrawArrowRay(Position, new Vector3(directionStrength.X, 0, directionStrength.Y), 1,
            Colors.DarkCyan);
        velocity.X = Acceleration(delta, velocity.X, directionStrength.X, direction.X, IsOnFloor());
        velocity.Z = Acceleration(delta, velocity.Z, directionStrength.Y, direction.Y, IsOnFloor());

        if (!Mathf.IsZeroApprox(direction.Length()))
        {
            DebugDraw3D.DrawRay(Position - new Vector3(0, 0.5f, 0), velocity, velocity.Length(), Colors.Blue);
            return velocity;
        }

        // decelerate with friction
        var friction = IsOnFloor() ? Friction : Friction * AirFrictionFactor;

        // using movementDirection here because MoveToward would equally slow down on both axes causing
        // shifting to the side in deceleration
        var movementDirection = new Vector2(velocity.X, velocity.Z).Normalized();
        velocity.X = Mathf.MoveToward(velocity.X, 0, friction * (float)delta * Math.Abs(movementDirection.X));
        velocity.Z = Mathf.MoveToward(velocity.Z, 0, friction * (float)delta * Math.Abs(movementDirection.Y));

        DebugDraw3D.DrawRay(Position - new Vector3(0, 0.5f, 0), velocity, velocity.Length(), Colors.Red);
        var graph = DebugDraw2D.GetGraph("speed") ?? DebugDraw2D.CreateGraph("speed");
        graph.BufferSize = 1000;
        DebugDraw2D.GraphUpdateData("speed", velocity.Length());

        return velocity;
    }

    private float Acceleration(double d, float v, float strength, float maxRatio, bool onFloor)
    {
        if (strength != 0.0f)
        {
            // accelerate
            v += strength * (MovementForce - Friction) / MassKg * (float)d;

            // limit velocity
            var max = MaxVelocity * Math.Abs(maxRatio);
            v = Math.Clamp(v, -max, max);
            // var graph = DebugDraw2D.GetGraph("maxspeed") ?? DebugDraw2D.CreateGraph("maxspeed");
            // graph.BufferSize = 1000;
            // DebugDraw2D.GraphUpdateData("maxspeed", max);
        }

        return v;
    }

    private Vector3 Gravity(double delta, Vector3 velocity)
    {
        if (!IsOnFloor())
            velocity.Y -= _gravity * GravityMultiplier * (float)delta;

        velocity.Y = Math.Clamp(velocity.Y, -TerminalVelocity, float.MaxValue);

        return velocity;
    }

    private Vector3 Jump(Vector3 velocity)
    {
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            velocity.Y = JumpForceN / MassKg;

        return velocity;
    }
}