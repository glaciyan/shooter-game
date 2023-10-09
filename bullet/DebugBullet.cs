using Godot;

namespace shootergame.bullet;

public partial class DebugBullet : Node3D
{
    [Export]
    public float Range = 50.0f;

    [Export(PropertyHint.Layers3DPhysics)]
    public uint CollisionMask;
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        SetPhysicsProcess(false);
    }

    private Vector3 _direction;

    public void Shoot(Vector3 from, Vector3 direction)
    {
        GlobalPosition = from;
        _direction = direction;
        SetPhysicsProcess(true);
    }

    public override void _PhysicsProcess(double delta)
    {
        var spaceState = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(GlobalPosition, GlobalPosition + _direction * Range, CollisionMask);
        var result = spaceState.IntersectRay(query);
        if (result.TryGetValue("position", out var res))
        {
            DebugDraw3D.DrawLineHit(GlobalPosition, _direction * Range, res.AsVector3(), true, 0.1f, Colors.Red,
                Colors.Green, 5.0f);
        }

        GD.Print(result);
        QueueFree();
    }
}