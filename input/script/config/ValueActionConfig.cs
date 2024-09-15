using Godot;
using Godot.Collections;

namespace shootergame.input;

[GlobalClass]
public partial class ValueActionConfig : ActionConfig
{
    [Export]
    public Array<ValueEventMapping> ValueEventMapping { get; set; }
}