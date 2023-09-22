using System;
using Godot;

namespace shootergame.player;

public partial class PlayerCharacter : CharacterBody3D
{
	[ExportGroup("Movement")]
	[Export]
	public float MaxVelocity = 5.0f;

	[Export]
	public float MovementForce = 2700.0f;

	[Export]
	public float Friction = 25.0f;

	[Export]
	public float AirFrictionFactor = 0.05f;

	[ExportSubgroup("Air Control")]
	[Export]
	public float AirSpeedControl = 1.0f;

	[Export]
	public float AirStrafeControl = 1.0f;

	[ExportSubgroup("")]
	[Export]
	public float TerminalVelocity = 55.0f;

	[Export]
	public float MassKg = 60.0f;

	[Export]
	public float JumpForceN = 300.0f;

	[ExportGroup("Camera")]
	[Export]
	public float LookaroundSpeed = 1.0f;

	
	private float _lookaroundSpeedReduction = 0.002f;

	private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	private Camera3D _camera;

	private Vector2 _viewPoint = new(0.0f,0.0f);

	private float _currentSpeed = 0.0f;

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("Camera3D");
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventMouseMotion mouseMotion) return;
		
		_viewPoint.X += mouseMotion.Relative.X * LookaroundSpeed * _lookaroundSpeedReduction;
		_viewPoint.Y += mouseMotion.Relative.Y * LookaroundSpeed * _lookaroundSpeedReduction;
		_viewPoint.Y = (float)Math.Clamp(_viewPoint.Y, -Math.PI/2, Math.PI/2);

		var transform = _camera.Transform;
		transform.Basis = Basis.Identity;
		_camera.Transform = transform;
		_camera.RotateObjectLocal(Vector3.Up, -_viewPoint.X);
		_camera.RotateObjectLocal(Vector3.Right, -_viewPoint.Y);
	}

	public override void _Process(double delta)
	{
		Velocity = Movement(delta, Gravity(delta, Jump(Velocity)));
		MoveAndSlide();
	}

	private Vector3 Movement(double delta, Vector3 velocity)
	{
		// GD.Print(-_camera.Transform.Basis.Z);
		var onFloor = IsOnFloor();

		var inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back").Normalized();

		var camVec = Vector2.Down.Rotated(_viewPoint.X); 
		var forwardVector = new Vector2(camVec.X, camVec.Y);
		// using rotation matrix to rotate by 90
		var sidewaysVector = new Vector2(forwardVector.Y, -forwardVector.X);
		var forwardDirection = forwardVector * inputDir.Y;
		var sidewaysDirection = sidewaysVector * inputDir.X;
		var direction2D = forwardDirection + sidewaysDirection;
		var direction = new Vector3(direction2D.X, 0, direction2D.Y).Normalized();
		
		GD.Print(direction);

		var directionStrength = direction;
		if (!onFloor)
		{
			directionStrength.Z *= AirSpeedControl;
			directionStrength.X *= AirStrafeControl;
		}

		velocity.X = Acceleration(delta, velocity.X, directionStrength.X, direction.X, onFloor);
		velocity.Z = Acceleration(delta, velocity.Z, directionStrength.Z, direction.Z, onFloor);

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
		}
		else
		{
			// decelerate with friction
			var friction = onFloor ? Friction : Friction * AirFrictionFactor;
			v = Mathf.MoveToward(v, 0, friction * (float)d);
		}

		return v;
	}

	private Vector3 Gravity(double delta, Vector3 velocity)
	{
		if (!IsOnFloor())
			velocity.Y -= _gravity * (float)delta;

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
