using System;
using Godot;
using shootergame.input.script;
using shootergame.input.script.config;
using Action = shootergame.input.script.Action;

public partial class InputSystem : Node
{
	[Export]
	private Key4WayVector2Config MoveConfig;

	private Action move;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		move = new Action()
		{
			Name = "move",
			Bindings = new Binding[]
			{
				new Vector2From4WayKeys(MoveConfig)
			}
		};
	}

	public override void _Input(InputEvent @event)
	{
		switch (@event)
		{
			case InputEventKey inputEventKey:
				HandleInputKeyEvent(inputEventKey);
				break;
			case InputEventMouseButton inputEventMouseButton:
				break;
			case InputEventMouseMotion inputEventMouseMotion:
				break;
			case InputEventJoypadButton inputEventJoypadButton:
				break;
			case InputEventJoypadMotion inputEventJoypadMotion:
				break;
		}
	}

	private void HandleInputKeyEvent(InputEventKey inputEventKey)
	{
		
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
