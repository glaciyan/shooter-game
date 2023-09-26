using System;
using System.Runtime.CompilerServices;
using Godot;
using shootergame.bullet;

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

    private Vector3 AimVector =>
        Vector3.Forward.Rotated(Vector3.Right, -_viewPoint.Y).Rotated(Vector3.Up, -_viewPoint.X);

    private Vector3 AimPosition => _cameraController.GlobalPosition;

    private CameraController _cameraController;
    private CollisionShape3D _collisionShape;
    private ShapeCast3D _shapeCast = new();

    public override void _Ready()
    {
        _cameraController = GetNode<CameraController>("VisualSmoothing/CameraController");
        _collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");

        _shapeCast.Shape = _collisionShape.Shape;
        AddChild(_shapeCast);

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

    private bool _shapeCastValid;

    public override void _PhysicsProcess(double delta)
    {
        Velocity = Movement(delta, Gravity(delta, Jump(Velocity)));

        if (!StepUp(delta)) MoveAndSlide();
    }

    private bool StepUp(double delta)
    {
        if (!IsOnFloor()) return false;

        const float stepHeight = 0.5f; // TODO temp
        if (_shapeCastValid || (_shapeCastValid = IsInstanceValid(_shapeCast)))
        {
            var v = Velocity * (float)delta;


            // initialize 
            _shapeCast.Shape = _collisionShape.Shape;
            _shapeCast.CollisionMask = CollisionMask;
            _shapeCast.Position = _collisionShape.Position;
            _shapeCast.MaxResults = 1;
            _shapeCast.TargetPosition = Vector3.Zero;

            var height = new Vector3(0, stepHeight, 0);
            var movement = new Vector3(v.X, 0, v.Z);
            var checkStairHeight = new Vector3(0, -stepHeight, 0);

            if (ShootShapeCast(_collisionShape.Position, height) && _shapeCast.GetCollisionCount() != 0)
            {
                GD.Print("head adjust");
                DebugDraw3D.DrawPoints(new[] { _shapeCast.GetCollisionPoint(0) }, 0.05f, Colors.Red, 0.2f);
                // height.Y = _shapeCast.GetCollisionPoint(0).Y - 1.75f / 2;
            }


            if (ShootShapeCast(height, movement) && _shapeCast.GetCollisionCount() != 0)
            {
                GD.Print("fwd stop");
                return false;
            }

            if (!ShootShapeCast(height + movement, -height) && _shapeCast.GetCollisionCount() == 0)
            {
                GD.Print("No stair");
                return false;
            }
            
            GD.Print("stepping");

            var gPos = GlobalPosition;
            gPos.Y = _shapeCast.GetCollisionPoint(0).Y;
            GlobalPosition = gPos;

            Position += movement;

            return true;


            // _shapeCast.Position = _collisionShape.Position + height + movement;
            // // TODO check for normal to prevent going up too steep stairs
            // if (!ShootShapeCast(checkStairHeight) && _shapeCast.GetCollisionCount() == 0)
            // {
            //     // GD.Print("No stair");
            //     return false;
            // }
            //
            // DebugDraw3D.DrawPoints(new[] { _shapeCast.GetCollisionPoint(0) }, 0.05f, Colors.Red, 0.2f);
            //
            // var collY = _shapeCast.GetCollisionPoint(0).Y;
            //
            // // var goalY = collY + ((BoxShape3D)_collisionShape.Shape).Size.Y / 2;
            // var globalY = collY + 1.75f / 2; // TODO temp value 1.75
            // var localY = globalY - _collisionShape.GlobalPosition.Y;
            //
            // _shapeCast.Position = Vector3.Zero;
            // if (ShootShapeCast(Vector3.Up * localY) && _shapeCast.GetCollisionCount() != 0)
            // {
            //     GD.Print("height block");
            //     return false;
            // }
            //
            // _shapeCast.Position += Vector3.Up * localY;
            // if (ShootShapeCast(movement) && _shapeCast.GetCollisionCount() != 0)
            // {
            //     DebugDraw3D.DrawPoints(new[] { _shapeCast.GetCollisionPoint(0) }, 0.05f, Colors.Green, 0.2f);
            //     GD.Print("fwd block");
            //     return false;
            // }
            //
            // movement.Y = localY;
            // Position += movement;

            //
            // // if (ShootShapeCast(height) || ShootShapeCast(movement) || !ShootShapeCast(checkStairHeight) ||
            // //     _shapeCast.GetCollisionCount() == 0)
            // // {
            // //     return false;
            // // }
            //
            // // step is detected
            // GD.Print(_shapeCast.GetCollisionNormal(0).Cross(Vector3.Up).Length());
            // DebugDraw3D.DrawPoints(new[] { point }, 0.05f, Colors.Red, 0.2f);
            //

            // return true;
        }

        return false;
    }

    private bool ShootShapeCast(Vector3 from, Vector3 target)
    {
        _shapeCast.Position = from;
        _shapeCast.TargetPosition = target;
        _shapeCast.ForceShapecastUpdate();

        return _shapeCast.IsColliding();
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