using Godot;
using Godot.Collections;

namespace shootergame.input.script;

[GlobalClass]
public partial class InputActionMapConfig : Resource
{
    [Export]
    public Array<ActionConfig> Actions { get; set; }
}