using Godot;

namespace shootergame.weapon;

public abstract partial class Weapon : Node3D
{
    [Export]
    public int MagazineSize = 12;

    [Export]
    public PackedScene Bullet;

    [Export]
    public ShootingType ShootingType;

    protected int MagazineBullets;
    
    public override void _Ready()
    {
        MagazineBullets = MagazineSize;
    }

    public abstract void Shoot(Vector3 from, Vector3 direction);
    public abstract void Reload();
}