using Godot;
using shootergame.player.script;

namespace shootergame;

public partial class RealPlayerManager : Node
{
	[Export(PropertyHint.Range, "0.1, 10, 0.1")]
	private float _lookaroundSpeed = 1.0f;
    
	private PlayerInput _input = new();

	public override void _Input(InputEvent @event)
	{
		if (@event.IsAction("move_forward") || @event.IsAction("move_back") || @event.IsAction("move_right") || @event.IsAction("move_left"))
		{
			_input.MovementInput = Input.GetVector("move_back", "move_forward", "move_left", "move_right");
			EmitSignal(SignalName.OnInput, _input);
			return;
		}

		if (@event is InputEventMouseMotion eventMouseMotion)
		{
			_input.AddViewAngle(eventMouseMotion.ScreenRelative, _lookaroundSpeed);
			EmitSignal(SignalName.OnInput, _input);
		}
	}
	
	[Signal]
	public delegate void OnInputEventHandler(PlayerInput input);
}