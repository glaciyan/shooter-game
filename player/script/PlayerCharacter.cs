using System;
using Godot;

namespace shootergame.player.script;

public partial class PlayerCharacter : CharacterBody3D
{
    public enum MovementState
    {
        Walking,
        Crouching,
        Sprinting
    }

    [ExportGroup("Moving")]
    [Export]
    public float MaxSpeedWalking = 5.0f;

    [Export]
    public float MaxSpeedSprinting = 7.0f;

    [Export]
    public float SprintInitialMaxAcceleration = 20f;

    [Export]
    public float MovementForce = 2700.0f;

    [Export]
    public float MaxVelocity = 500f;

    [Export]
    public float Friction = 45f;

    [Export]
    public float GroundFriction = 1f;

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
    public float MaxSpeedCrouching = 3.0f;

    [Export]
    public float StandingHeight = 1.8f;

    [Export]
    public float CrouchingHeight = 1.2f;

    [Export]
    public Vector3 CrouchShapeOffset = new(0, -0.3f, 0);

    [ExportGroup("Camera")]
    [Export(PropertyHint.Range, "0.1, 10, 0.1")]
    public float LookaroundSpeed = 1.0f;

    // public vars

    // private vars
    private Vector2 ForwardVector => Vector2.Down.Rotated(_viewPoint.X);

    private Vector2 RightVector
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
    private float _wishSpeed;
    private MovementState _state;
    private Vector3 _cameraPosition;
    private bool _crouchJumped;
    private const float CrouchLerpFollowSpeed = 6f;

    public Vector3 AimVector =>
        Vector3.Forward.Rotated(Vector3.Right, -_viewPoint.Y).Rotated(Vector3.Up, -_viewPoint.X);

    public Vector3 AimOrigin => _cameraController.GlobalPosition;

    // Nodes
    private CameraController _cameraController;
    private CollisionShape3D _standingCollision;
    private CollisionShape3D _crouchingCollision;
    private ShapeCast3D _shapeCast = new();

    public override void _Ready()
    {
        _wishSpeed = MaxSpeedWalking;
        _cameraController = GetNode<CameraController>("%CameraController");
        _cameraPosition = _cameraController.Position;
        _standingCollision = GetNode<CollisionShape3D>("StandingCollision");
        _crouchingCollision = GetNode<CollisionShape3D>("CrouchingCollision");

        _shapeCast.Shape = _standingCollision.Shape;
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
        }

        if (@event.IsActionPressed("sprint"))
        {
            _state = MovementState.Sprinting;
        }
        else if (@event.IsActionReleased("sprint"))
        {
            _state = MovementState.Walking;
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
        DebugDraw2D.SetText("Movement", _state.ToString());

        // visually move the camera when crouching
        if (_state == MovementState.Crouching)
        {
            var diff = CrouchingHeight - StandingHeight;
            CameraOffsetCrouching = CameraOffsetCrouching.Lerp(Vector3.Up * diff, (float)delta * CrouchLerpFollowSpeed);
        }
        else
        {
            // move back up when we are standing
            CameraOffsetCrouching = CameraOffsetCrouching.Lerp(Vector3.Zero, (float)delta * CrouchLerpFollowSpeed);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Crouch((float)delta);

        Velocity = Movement(delta, Gravity(delta, Jump(Velocity)));

        var (oldPosition, desiredVelocity) = (GlobalPosition, Velocity);

        MoveAndSlide();

        StickToFloor();

        DoWalkStairs(delta, desiredVelocity, oldPosition);
    }

    private void StickToFloor()
    {
        if (!IsSupported() && !StickDown.IsZeroApprox() && Velocity.Y is > -1.0f and < 1.0e-2f)
        {
            var col = ShootShapeCast(Vector3.Zero, StickDown);
            if (col.IsColliding())
            {
                var frac = col.GetClosestCollisionSafeFraction();
                GlobalPosition += frac * StickDown;
            }
        }
    }


    private Vector3 _cameraOffsetCrouching = Vector3.Zero;

    private Vector3 CameraOffsetCrouching
    {
        get => _cameraOffsetCrouching;
        set
        {
            _cameraOffsetCrouching = value;
            _cameraController.Position = _cameraPosition + _cameraOffsetCrouching;
        }
    }


    private void Crouch(float delta)
    {
        // when isSprinting is true, could do a slide
        if (Input.IsActionPressed("crouch"))
        {
            var diff = CrouchingHeight - StandingHeight;

            if (!_crouchJumped)
            {
                if (!IsOnFloor()) Velocity += Vector3.Up * -diff / 2.5f * JumpForceN / MassKg;
                _crouchJumped = true;
            }

            _state = MovementState.Crouching;

            // only change collision shape after we are down
            if (Mathf.Abs(CameraOffsetCrouching.Y - diff) > 0.2f) return;
            ChangeCastShape(_crouchingCollision.Shape, CrouchShapeOffset);
            _standingCollision.Disabled = true;
            _crouchingCollision.Disabled = false;
        }
        else if (CanUncrouch())
        {
            ChangeCastShape(_standingCollision.Shape);
            _standingCollision.Disabled = false;
            _crouchingCollision.Disabled = true;
            _state = MovementState.Walking;
        }

        if (_state == MovementState.Walking)
        {
            // move back up when we are standing
            CameraOffsetCrouching = CameraOffsetCrouching.Lerp(Vector3.Zero, delta * CrouchLerpFollowSpeed);

            // and reset the crouch jump
            if (IsOnFloor())
            {
                _crouchJumped = false;
            }
        }
    }

    private bool CanUncrouch()
    {
        var oldShape = _shapeCast.Shape;
        var oldShapeOffset = _shapeCastOffset;
        ChangeCastShape(_standingCollision.Shape);
        var collision = ShootShapeCast(Vector3.Zero, Vector3.Zero);
        ChangeCastShape(oldShape, oldShapeOffset);
        var hit = collision?.IsColliding() ?? false;
        return !hit;
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
            MoveAndCollide(stepForwardTest);
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

    private Vector3 _shapeCastOffset = Vector3.Zero;

    private ShapeCast3D ShootShapeCastGlobal(Vector3 from, Vector3 target)
    {
        _shapeCast.GlobalPosition = from + _shapeCastOffset;
        _shapeCast.TargetPosition = target;
        _shapeCast.ForceShapecastUpdate();
        // DebugDraw3D.DrawPoints(new[] { _shapeCast.GlobalPosition }, 0.2f, Colors.Black, 0.1f);

        return _shapeCast;
    }

    private ShapeCast3D ShootShapeCast(Vector3 from, Vector3 target)
    {
        _shapeCast.Position = from + _shapeCastOffset;
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

    private float _oldWishSpeed;

    private Vector3 Movement(double delta, Vector3 velocity)
    {
        if (_state != MovementState.Crouching && Input.IsActionPressed("sprint")) _state = MovementState.Sprinting;

        var (sMove, fMove) = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        var movement = velocity.ToVector2Flat();

        var desiredDirection = (ForwardVector * fMove + RightVector * sMove).Normalized();

        _oldWishSpeed = _wishSpeed;
        _wishSpeed = _state switch
        {
            MovementState.Walking => MaxSpeedWalking,
            MovementState.Crouching => MaxSpeedCrouching,
            MovementState.Sprinting => MaxSpeedSprinting,
            _ => MaxSpeedWalking
        };

        if (_falling)
        {
            // air movement

            if (desiredDirection.IsZeroApprox())
            {
                // no input in air just apply regular air friction
                var friction = Friction * AirFrictionFactor;
                movement = movement.MoveToward(Vector2.Zero, friction * (float)delta);
            }
            else
            {
                // TODO Goals:
                // 1. Should be able to control themself in the air with the air factors
                // 2. Should not slow down based on _wishSpeed
                // The only slowdown should be friction or player controlling mid air
                GD.Print(movement.Dot(desiredDirection));
            }
        }
        else
        {
            // walking movement

            if (desiredDirection.IsZeroApprox())
            {
                // no input apply friction

                var friction = Friction * GroundFriction;
                movement = movement.MoveToward(Vector2.Zero, friction * (float)delta);
            }
            else
            {
                // acceleration

                var desiredVelocity = desiredDirection * _wishSpeed;
                var acceleration = MovementForce / MassKg * (float)delta;

                movement = movement.MoveToward(desiredVelocity, acceleration);
            }
        }


        return movement.ToVector3Flat(velocity.Y);
    }

    private Vector3 CheckVelocity(Vector3 velocity)
    {
        velocity.X = Mathf.Clamp(velocity.X, -MaxVelocity, MaxVelocity);
        velocity.Z = Mathf.Clamp(velocity.Z, -MaxVelocity, MaxVelocity);
        return velocity;
    }

    private DateTime _lastLandedTime = DateTime.Now;
    private bool _falling;

    private Vector3 Gravity(double delta, Vector3 velocity)
    {
        if (!IsOnFloor())
        {
            velocity.Y -= _gravity * GravityMultiplier * (float)delta;
            _falling = true;
        }
        else
        {
            if (_falling) _lastLandedTime = DateTime.Now;
            _falling = false;
        }

        velocity.Y = Math.Clamp(velocity.Y, -TerminalVelocity, float.MaxValue);

        return velocity;
    }

    private DateTime _lastJumpTime = DateTime.Now;

    private Vector3 Jump(Vector3 velocity)
    {
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            velocity.Y = JumpForceN / MassKg;
            _lastJumpTime = DateTime.Now;
        }

        return velocity;
    }

    private void ChangeCastShape(Shape3D shape, Vector3? offset = null)
    {
        _shapeCast.SetDeferred("shape", shape);
        _shapeCastOffset = offset ?? Vector3.Zero;
    }
}