using System;
using Godot;
using shootergame.bullet;

namespace shootergame.player.script;

public partial class PlayerCharacter : CharacterBody3D
{
    [ExportGroup("Moving")]
    [Export]
    public float MaxSpeedWalking = 5.0f;

    [Export]
    public float MaxAcceleration = 45f;

    [Export]
    public float MovementForce = 2700.0f;

    [Export]
    public float Friction = 25.0f;

    [Export]
    public float MassKg = 60.0f;

    [ExportSubgroup("Walk Stairs")]
    [Export]
    public Vector3 StepUp = new(0, 0.4f, 0);

    [Export]
    public float StepForward = 0.02f;

    [Export]
    public float StepForwardTest = 0.15f;

    [Export]
    public float CosAngleForwardContact = Mathf.Cos(Mathf.DegToRad(75.0f));

    [Export]
    public Vector3 StepDownExtra = Vector3.Zero;

    [ExportSubgroup("Stick To Floor")]
    [Export]
    public Vector3 StickDown = new(0, -0.4f, 0);

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
    [ExportGroup("Crouching")]
    [Export]
    public Shape3D CrouchShape;

    [Export]
    public float CrouchMaxVelocity = 3.0f;

    [Export]
    public float StandingHeight = 1.8f;

    [Export]
    public float CrouchingHeight = 0.8f;

    [ExportGroup("Camera")]
    [Export(PropertyHint.Range, "0.1, 10, 0.1")]
    public float LookaroundSpeed = 1.0f;

    [ExportGroup("Shooting")]
    [Export]
    public PackedScene Bullet;

    // public vars

    // private vars
    private Vector2 ForwardVector => Vector2.Down.Rotated(_viewPoint.X);

    private Vector2 SidewaysVector
    {
        get
        {
            var fw = ForwardVector;
            return new Vector2(fw.Y, -fw.X); // using rotation matrix to rotate by 90
        }
    }

    private Vector2 _viewPoint = new(0.0f, 0.0f);
    private const float LookaroundSpeedReduction = 0.002f;
    private readonly float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    private bool _isControlling;
    private const float FloorMaxAngleThreshold = 0.01f;
    private Shape3D _standingShape;
    private float _maxSpeed;

    private Vector3 AimVector =>
        Vector3.Forward.Rotated(Vector3.Right, -_viewPoint.Y).Rotated(Vector3.Up, -_viewPoint.X);

    private Vector3 AimOrigin => _cameraController.GlobalPosition;

    // Nodes
    private CameraController _cameraController;
    private CollisionShape3D _collisionShape;
    private ShapeCast3D _shapeCast = new();

    public override void _Ready()
    {
        _maxSpeed = MaxSpeedWalking;
        _cameraController = GetNode<CameraController>("%CameraController");
        _collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
        _standingShape = (Shape3D)_collisionShape.Shape.Duplicate();

        _shapeCast.Shape = _collisionShape.Shape;
        _shapeCast.DebugShapeCustomColor = new Color(Colors.Aqua);
        AddChild(_shapeCast);

        StartControlling();
    }

    public override void _Input(InputEvent @event)
    {
        if (_cameraController != null && Input.MouseMode == Input.MouseModeEnum.Captured &&
            @event is InputEventMouseMotion mouseMotion)
        {
            _viewPoint.X += mouseMotion.Relative.X * LookaroundSpeed * LookaroundSpeedReduction;
            switch (_viewPoint.X)
            {
                case < -2 * Mathf.Pi:
                    _viewPoint.X += 2 * Mathf.Pi;
                    break;
                case > 2 * Mathf.Pi:
                    _viewPoint.X -= 2 * Mathf.Pi;
                    break;
            }

            _viewPoint.Y += mouseMotion.Relative.Y * LookaroundSpeed * LookaroundSpeedReduction;
            _viewPoint.Y = (float)Math.Clamp(_viewPoint.Y, -Math.PI / 2, Math.PI / 2);

            _cameraController.RotateTo(_viewPoint);

            var transform = Transform;
            transform.Basis = Basis.Identity;
            Transform = transform;
            RotateObjectLocal(Vector3.Up, -_viewPoint.X);
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
        DebugDraw2D.GraphUpdateData("speed", Velocity.ToVector2Flat().Length());

        DebugDraw2D.SetText("OnFloor", IsOnFloor());
        DebugDraw2D.SetText("OnWall", IsOnWall());
        DebugDraw2D.SetText("IsSupported", IsSupported());
    }

    public override void _PhysicsProcess(double delta)
    {
        Crouch();

        var oldPosition = GlobalPosition;

        Velocity = Movement(delta, Gravity(delta, Jump(Velocity)));

        var desiredVelocity = Velocity;

        MoveAndSlide();

        StickToFloor();

        DoWalkStairs(delta, desiredVelocity, oldPosition);
    }

    private void StickToFloor()
    {
        if (!IsSupported() && !StickDown.IsZeroApprox())
        {
            if (Velocity.Y is > -1.0f and < 1.0e-2f)
            {
                var col = ShootShapeCast(Vector3.Zero, StickDown);
                if (col.IsColliding())
                {
                    var frac = col.GetClosestCollisionSafeFraction();
                    GlobalPosition += frac * StickDown;
                }
            }
        }
    }

    private void Crouch()
    {
        if (Input.IsActionPressed("crouch"))
        {
            ChangeShape(CrouchShape);
            _maxSpeed = CrouchMaxVelocity;
            _isCrouching = true;
        }
        else if (_collisionShape.Shape == CrouchShape)
        {
            ChangeShape(_standingShape);
            _maxSpeed = MaxSpeedWalking;
            _isCrouching = false;
            // when player is uncrouching they are stuck in the ground, teleport up a little
            var missingHeight = (StandingHeight - CrouchingHeight) / 2;
            var collision = ShootShapeCast(Vector3.Zero, new Vector3(0, -missingHeight, 0));
            if (collision.IsColliding())
            {
                var gp = GlobalPosition;
                GlobalPosition = new Vector3(gp.X,
                    gp.Y + missingHeight * (1.0f - collision.GetClosestCollisionSafeFraction()), gp.Z);
            }
        }
    }

    private void DoWalkStairs(double delta, Vector3 desiredVelocity, Vector3 oldPosition)
    {
        // Walk stairs
        // ported from https://github.com/jrouwe/JoltPhysics/blob/1b21180c67930f5669750a7912c55d90407586ee/Jolt/Physics/Character/CharacterVirtual.cpp#L1343
        if (!StepUp.IsZeroApprox())
        {
            // how much we wanted to move horizontally
            var desiredHorizontalStep = desiredVelocity * (float)delta;
            // set "up" to 0
            desiredHorizontalStep -= desiredHorizontalStep.Dot(Vector3.Up) * Vector3.Up;
            if (desiredHorizontalStep.Length() > 0.0f)
            {
                // how much did we actually move horizontally
                var achievedHorizontalStep = GlobalPosition - oldPosition;
                achievedHorizontalStep -= achievedHorizontalStep.Dot(Vector3.Up) * Vector3.Up;
                var stepForwardNormalized = desiredHorizontalStep.Normalized();
                achievedHorizontalStep = Mathf.Max(0.0f, achievedHorizontalStep.Dot(stepForwardNormalized)) *
                                         stepForwardNormalized;

                if (achievedHorizontalStep.Length() + 1.0e-4f < desiredHorizontalStep.Length() &&
                    CanWalkStairs(desiredVelocity))
                {
                    var stepForward = stepForwardNormalized * Mathf.Max(StepForward,
                        desiredHorizontalStep.Length() - achievedHorizontalStep.Length());

                    // Calculate how far to scan ahead for a floor. This is only used in
                    // case the floor normal at step_forward is too steep. In that case an
                    // additional check will be performed at this distance to check if that
                    // normal is not too steep. Start with the ground normal in the
                    // horizontal plane and normalizing it
                    var stepForwardTest = -GetFloorNormal();
                    stepForwardTest -= stepForwardTest.Dot(UpDirection) * UpDirection;
                    stepForwardTest = stepForwardTest.NormalizedOr(stepForwardNormalized);

                    if (stepForwardTest.Dot(stepForwardNormalized) < CosAngleForwardContact)
                    {
                        stepForwardTest = stepForwardNormalized;
                    }

                    stepForwardTest *= StepForwardTest;

                    var success = WalkStairs(StepUp, stepForward, stepForwardTest, StepDownExtra);
                    // reset any lost velocity from sliding into the stair
                    if (success) Velocity = desiredVelocity;
                }
            }
        }
    }

    private bool WalkStairs(Vector3 stepUp, Vector3 stepForward, Vector3 stepForwardTest,
        Vector3 stepDownExtra)
    {
        var up = stepUp;

        // Move up
        var contact = ShootShapeCastGlobal(GlobalPosition, up);
        if (contact.IsColliding())
        {
            if (contact.GetClosestCollisionSafeFraction() < 1.0e-6f)
            {
#if STAIR_DEBUG
                GD.Print("Walk Stairs: no up movement");
#endif
                return false;
            }

            up *= contact.GetClosestCollisionSafeFraction();
        }

        var upPosition = GlobalPosition + up;

#if STAIR_DEBUG
        DebugDraw3D.DrawArrowLine(GlobalPosition, upPosition, Colors.White, 0.1f, false, 0.1f);
#endif
        // Horizontal movement
        var gp = GlobalPosition;
        GlobalPosition = upPosition;
        MoveAndCollide(stepForward);
        var newPosition = GlobalPosition;
        GlobalPosition = gp;

        var horizontalMovement = newPosition - upPosition;
        var horizontalMovementSq = horizontalMovement.LengthSquared();
        if (horizontalMovementSq < 1.0e-8f)
        {
#if STAIR_DEBUG
            GD.Print("WalkStairs: no movement");
#endif
            return false; // no movement
        }

#if STAIR_DEBUG
        DebugDraw3D.DrawArrowLine(upPosition, newPosition, Colors.Red, 0.1f, false, 0.1f);
#endif

        // Move down towards the floor.
        // Note that we travel the same amount down as we travelled up with the
        // specified extra

        var down = -up + stepDownExtra;
        contact = ShootShapeCastGlobal(newPosition, down);
        if (!contact.IsColliding())
        {
#if STAIR_DEBUG
            GD.Print("WalkStairs: no floor found");
#endif
            return false; // no floor found
        }

#if STAIR_DEBUG
        var debugPos = newPosition + contact.GetClosestCollisionSafeFraction() * down;
        DebugDraw3D.DrawArrowLine(newPosition, debugPos, Colors.White, 0.1f, false, 0.1f);
        DebugDraw3D.DrawArrowLine(contact.GetCollisionPoint(0),
            contact.GetCollisionNormal(0) + contact.GetCollisionPoint(0), Colors.White, 0.1f, false, 0.1f);

        DebugDraw3D.DrawBox(debugPos, new Vector3(0.4f, 1.8f, 0.4f), Colors.White, false, 0.1f);
#endif

        if (IsSlopeTooSteep(contact.GetCollisionNormal(0)))
        {
            if (stepForwardTest.IsZeroApprox())
            {
#if STAIR_DEBUG
                GD.Print("WalkStairs: too steep and stepForwardTest is 0");
#endif
                return false;
            }

            gp = GlobalPosition;
            GlobalPosition = upPosition;
            MoveAndCollide(stepForwardTest); // TODO value might be incorrect
            var testPosition = GlobalPosition;
            GlobalPosition = gp;

            var testHorizontalPositionSq = (testPosition - upPosition).LengthSquared();
            if (testHorizontalPositionSq <= horizontalMovementSq - 1.0e-8f)
            {
#if STAIR_DEBUG
                GD.Print("WalkStairs: stepForwardTest: We didn't move any further than in the previous test");
#endif
                return false;
            }

#if STAIR_DEBUG
            DebugDraw3D.DrawArrowLine(upPosition, testPosition, Colors.Cyan, 0.1f, false, 0.1f);
#endif

            // Then sweep down
            var testContact = ShootShapeCastGlobal(testPosition, down);
            if (!testContact.IsColliding())
            {
#if STAIR_DEBUG
                GD.Print("WalkStairs: sweep down failed");
#endif
                return false;
            }

#if STAIR_DEBUG
            var debugPos2 = testPosition + testContact.GetClosestCollisionSafeFraction() * down;
            DebugDraw3D.DrawArrowLine(testPosition, debugPos2, Colors.White, 0.1f, false, 0.1f);
            DebugDraw3D.DrawArrowLine(testContact.GetCollisionPoint(0),
                testContact.GetCollisionNormal(0) + testContact.GetCollisionPoint(0), Colors.White, 0.1f, false, 0.1f);

            DebugDraw3D.DrawBox(debugPos2, new Vector3(0.4f, 1.8f, 0.4f), Colors.White, false, 0.1f);
#endif

            if (IsSlopeTooSteep(testContact.GetCollisionNormal(0)))
            {
#if STAIR_DEBUG
                GD.Print("WalkStairs: still too steep");
#endif
                return false;
            }
        }

        down *= contact.GetClosestCollisionSafeFraction();
        newPosition += down;

        GlobalPosition = newPosition;

        return true;
    }

    private bool CanWalkStairs(Vector3 velocity)
    {
        // only walk stairs when we have supporting ground
        if (!IsSupported())
            return false;

        var horizontalVelocity = velocity - velocity.Dot(UpDirection) * UpDirection;
        // if we have enough horizontal vel and we are on a steep slope (simplified from jolt original code)
        return !horizontalVelocity.IsZeroApprox();
    }

    private bool IsSlopeTooSteep(Vector3 normal)
    {
        var floorAngle = Mathf.Acos(normal.Dot(UpDirection));
        return floorAngle >= FloorMaxAngle + FloorMaxAngleThreshold;
    }

    // The Player has ground underneath them
    private bool IsSupported()
    {
        if (IsOnFloor()) return true;

        var collision = GetLastSlideCollision();
        if (collision == null) return false;

        for (var i = 0; i < collision.GetCollisionCount(); i++)
        {
            var d = collision.GetNormal().Dot(UpDirection);
            if (d < 1.0e-4f) return false; // we are not up against a wall
        }

        return true;
    }

    private ShapeCast3D ShootShapeCastGlobal(Vector3 from, Vector3 target)
    {
        _shapeCast.GlobalPosition = from;
        _shapeCast.TargetPosition = target;
        _shapeCast.ForceShapecastUpdate();
        // DebugDraw3D.DrawPoints(new[] { _shapeCast.GlobalPosition }, 0.2f, Colors.Black, 0.1f);

        return _shapeCast;
    }

    private ShapeCast3D ShootShapeCast(Vector3 from, Vector3 target)
    {
        _shapeCast.Position = from;
        _shapeCast.TargetPosition = target;
        _shapeCast.ForceShapecastUpdate();
        // DebugDraw3D.DrawPoints(new[] { _shapeCast.GlobalPosition }, 0.2f, Colors.Black, 0.1f);

        return _shapeCast;
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
        bullet.Shoot(AimOrigin, AimVector);
        GD.Print("pew");
    }

    // -y is forward, +x is right
    private Vector2 _velocity = Vector2.Zero;
    private bool _isCrouching;

    private Vector3 Movement(double delta, Vector3 velocity)
    {
        var additive = false;
        var inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");

        var sidewaysInput = SidewaysVector * inputDir.X;
        var forwardInput = ForwardVector * inputDir.Y;

        var input = (forwardInput + sidewaysInput).FastLimit();

        
        // how fast we want to go according to our input
        var desiredVelocity = input * _maxSpeed;
        
        // how fast we are going
        var flatVelocity = velocity.ToVector2Flat();

        // how much can we change the velocity
        var maxSpeedChange = (MovementForce - Friction) / MassKg * (float)delta;

        // set friction if no input
        if (input.IsZeroApprox())
        {
            maxSpeedChange = Friction * (float)delta;
            if (!IsOnFloor()) maxSpeedChange *= AirFrictionFactor;
        } else if (!IsOnFloor())
        {
            maxSpeedChange *= AirSpeedControl;
            additive = _isCrouching;
        }
        
        var moved = flatVelocity.MoveToward(desiredVelocity, maxSpeedChange);
        
        if (additive)
        {
            // take the max
            flatVelocity = moved.LengthSquared() < flatVelocity.LengthSquared() ? flatVelocity : moved;
        }
        else
        {
            flatVelocity = moved;
        }
        
        return flatVelocity.ToVector3Flat(velocity.Y);
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

    private void ChangeShape(Shape3D shape)
    {
        _collisionShape.SetDeferred("shape", shape);
        _shapeCast.SetDeferred("shape", shape);
    }
}