using System.Collections.Generic;
using Godot;
using shootergame.input.script;

public partial class InputSystem : Node
{
	[Export]
	private InputActionMapConfig _actionMap;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		UpdateInputMap();
	}

	private void UpdateInputMap()
	{
		foreach (var actionMapAction in _actionMap.Actions)
		{
			actionMapAction.
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
