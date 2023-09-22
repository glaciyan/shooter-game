using Godot;

namespace shootergame.player;

public partial class CameraController : Node3D
{
	private Camera3D _camera;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("Camera3D");
	}

	public void RotateTo(Vector2 viewPoint)
	{
		var transform = _camera.Transform;
		transform.Basis = Basis.Identity;
		_camera.Transform = transform;
		_camera.RotateObjectLocal(Vector3.Up, -viewPoint.X);
		_camera.RotateObjectLocal(Vector3.Right, -viewPoint.Y);
	}
}
