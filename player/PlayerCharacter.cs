using System;
using System.Globalization;
using Godot;

namespace shootergame.player;

public partial class PlayerCharacter : CharacterBody3D
{
	private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	private Camera3D _camera;

	[ExportGroup("Movement")]
	[Export]
	public float MaxVelocity = 5.0f;

	[Export]
	public float MovementForce = 2000.0f;

	[Export]
	public float Friction = 20.0f;

	[Export]
	public float AirFrictionFactor = 0.05f;

	[ExportSubgroup("Air Control")]
	[Export]
	public float AirSpeedControl = 0.3f;

	[Export]
	public float AirStrafeControl = 0.1f;

	[ExportSubgroup("")]
	[Export]
	public float TerminalVelocity = 55.0f;

	[Export]
	public float MassKg = 60.0f;

	[Export]
	public float JumpForceN = 300.0f;

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("Camera3D");
		GD.Print(_camera.Transform.Basis);
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			// GD.Print(mouseMotion.Relative);
		}
	}

	public override void _Process(double delta)
	{
		Velocity = Movement(delta, Gravity(delta, Jump(Velocity)));
		MoveAndSlide();

		// GD.Print(
		// 	$"{Velocity.X.ToString("F", CultureInfo.InvariantCulture)},{Velocity.Y.ToString("F", CultureInfo.InvariantCulture)},{Velocity.Z.ToString("F", CultureInfo.InvariantCulture)}");
	}

	private Vector3 Movement(double delta, Vector3 velocity)
	{
		var onFloor = IsOnFloor();

		var inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back").Normalized();

		var direction = new Vector3(inputDir.X, 0, inputDir.Y).Normalized();
		if (!onFloor)
		{
			direction.Z *= AirSpeedControl;
			direction.X *= AirStrafeControl;
		}
		
		// TODO: suddenly moving slow when in air
		
		velocity.X = Acceleration(delta, velocity.X, direction.X);
		velocity.Z = Acceleration(delta, velocity.Z, direction.Z);

		return velocity;

		float Acceleration(double d, float v, float strength)
		{
			if (strength != 0.0f)
			{
				// accelerate
				v += strength * (MovementForce - Friction) / MassKg * (float)d;
				var max = MaxVelocity * Math.Abs(strength);
				v = Math.Clamp(v, -max, max);
			}
			else
			{
				// decelerate
				var friction = onFloor ? Friction : Friction * AirFrictionFactor;
				v = Mathf.MoveToward(v, 0, friction * (float)d);
			}

			return v;
		}
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
