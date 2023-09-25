using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Godot;
using shootergame.bullet;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;

namespace shootergame.player.script;

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
    public float AirSpeedControl = 0.4f;

    [Export(PropertyHint.Range, "0, 1, 0.1")]
    public float AirStrafeControl = 0.2f;

    [Export]
    public float AirFrictionFactor = 0.05f;

    [ExportSubgroup("")]
    [ExportGroup("Camera")]
    [Export(PropertyHint.Range, "0.1, 10, 0.1")]
    public float LookaroundSpeed = 1.0f;

    [ExportGroup("Shooting")]
    [Export]
    public PackedScene Bullet;

    private Vector2 _viewPoint = new(0.0f, 0.0f);

    private Vector2 ForwardVector => Vector2.Down.Rotated(_viewPoint.X);

    private Vector2 SidewaysVector
    {
        get
        {
            var fw = ForwardVector;
            return new Vector2(fw.Y, -fw.X); // using rotation matrix to rotate by 90
        }
    }

    private const float LookaroundSpeedReduction = 0.002f;
    private readonly float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    private bool _isControlling;

    private CameraController _cameraController;

    private Vector3 AimVector => Vector3.Forward.Rotated(Vector3.Right, -_viewPoint.Y).Rotated(Vector3.Up, -_viewPoint.X);
    private Vector3 AimPosition => _cameraController.GlobalPosition;

    public override void _Ready()
    {
        _cameraController = GetNode<CameraController>("VisualSmoothing/CameraController");

        StartControlling();
    }

    public override void _Input(InputEvent @event)
    {
        if (_cameraController != null && Input.MouseMode == Input.MouseModeEnum.Captured &&
            @event is InputEventMouseMotion mouseMotion)
        {
            _viewPoint.X += mouseMotion.Relative.X * LookaroundSpeed * LookaroundSpeedReduction;
            _viewPoint.Y += mouseMotion.Relative.Y * LookaroundSpeed * LookaroundSpeedReduction;
            _viewPoint.Y = (float)Math.Clamp(_viewPoint.Y, -Math.PI / 2, Math.PI / 2);

            _cameraController.RotateTo(_viewPoint);
        }

        if (@event.IsActionPressed("shoot"))
        {
            ShootDebugLaser();
        }
    }

    public override void _Process(double delta)
    {
        var graph = DebugDraw2D.GetGraph("speed") ?? DebugDraw2D.CreateGraph("speed");
        graph.BufferSize = 1000;
        DebugDraw2D.GraphUpdateData("speed", Velocity.Length());
        DebugDraw2D.SetText("OnFloor", IsOnFloor());
    }

    public override void _PhysicsProcess(double delta)
    {
        // for (var i = 0; i < GetSlideCollisionCount(); i++)
        // {
        //     var collision = GetSlideCollision(i);
        //     GD.Print(collision.GetNormal().Cross(Vector3.Down).Length());
        //     
        //     DebugDraw3D.DrawPoints(new []{collision.GetPosition()}, 0.2f, Colors.Red, 0.1f);
        //     DebugDraw3D.DrawArrowRay(collision.GetPosition(), collision.GetNormal(), 0.5f, Colors.DarkGreen, 0.5F, false, 0.1F);
        // }
        
        Velocity = Movement(delta, Gravity(delta, Jump(Velocity)));
        MoveAndSlide();
    }

    private void StartControlling()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _isControlling = true;
    }

    private void StopControlling()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _isControlling = false;
    }

    private void ShootDebugLaser()
    {
        var bullet = Bullet.Instantiate<Bullet>();
        AddChild(bullet);
        bullet.Shoot(AimPosition, AimVector);
        GD.Print("pew");
    }

    private Vector3 Movement(double delta, Vector3 velocity)
    {
        // GD.Print(-_camera.Transform.Basis.Z);
        var inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");

        // direction with view and input
        var forwardDirection = ForwardVector * inputDir.Y;
        var sidewaysDirection = SidewaysVector * inputDir.X;

        // final direction vector by adding horizontal and vertical together
        var direction = (forwardDirection + sidewaysDirection).Normalized();
        if (!IsOnFloor())
        {
            direction = forwardDirection.Normalized() * AirSpeedControl +
                        sidewaysDirection.Normalized() * AirStrafeControl;
        }

        // DebugDraw3D.DrawRay(Position, new Vector3(direction.X, 0, direction.Y), 1, Colors.DarkCyan);

        velocity.X = Acceleration(delta, velocity.X, direction.X);
        velocity.Z = Acceleration(delta, velocity.Z, direction.Y);

        // limit speed on X and Z
        var xzVelocity = new Vector2(velocity.X, velocity.Z);
        if (xzVelocity.Length() > MaxVelocity)
        {
            xzVelocity = xzVelocity.Normalized() * MaxVelocity;
            velocity.X = xzVelocity.X;
            velocity.Z = xzVelocity.Y;
        }

        // we are still moving no need to decelerate
        if (!Mathf.IsZeroApprox(direction.Length()))
        {
            // DebugDraw3D.DrawRay(Position, velocity, velocity.Length(), Colors.Blue);
            return velocity;
        }

        // decelerate with friction
        var friction = IsOnFloor() ? Friction : Friction * AirFrictionFactor;

        // using movementDirection here because MoveToward would equally slow down on both axes causing
        // shifting to the side in deceleration
        var movementDirection = new Vector2(velocity.X, velocity.Z).Normalized();
        velocity.X = Mathf.MoveToward(velocity.X, 0, friction * (float)delta * Math.Abs(movementDirection.X));
        velocity.Z = Mathf.MoveToward(velocity.Z, 0, friction * (float)delta * Math.Abs(movementDirection.Y));

        // DebugDraw3D.DrawRay(Position, velocity, velocity.Length(), Colors.Red);

        return velocity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Acceleration(double d, float v, float strength)
    {
        v += strength * (MovementForce - Friction) / MassKg * (float)d;
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