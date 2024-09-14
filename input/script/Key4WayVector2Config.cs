using Godot;

namespace shootergame.input;

[GlobalClass]
public partial class Key4WayVector2Config : Vector2EventMapping
{
    [Export]
    public InputEvent Up { get; set; }

    [Export]
    public InputEvent Down { get; set; }

    [Export]
    public InputEvent Left { get; set; }

    [Export]
    public InputEvent Right { get; set; }
}