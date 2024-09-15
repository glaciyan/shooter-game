using Godot;

namespace shootergame.input;

[GlobalClass]
public abstract partial class ActionConfig : Resource
{
    [Export]
    public string Name { get; set; }
}