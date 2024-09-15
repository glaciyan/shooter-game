using Godot;

namespace shootergame.input.script.config;

[GlobalClass]
public partial class Key4WayVector2Config : Vector2EventMapping
{
    [Export]
    public InputEventKey Up { get; set; }

    [Export]
    public InputEventKey Down { get; set; }

    [Export]
    public InputEventKey Left { get; set; }

    [Export]
    public InputEventKey Right { get; set; }
}