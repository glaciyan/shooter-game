using Godot;
using shootergame.bullet;

namespace shootergame.item.weapon;

public abstract partial class ShootingWeapon : Weapon
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

    public virtual bool Shoot(Vector3 from, Vector3 direction)
    {
        if (MagazineBullets-- > 0)
        {
            var bullet = Bullet.Instantiate<DebugBullet>();
            AddChild(bullet);
            bullet.Shoot(from, direction);
            return true;
        }

        GD.Print("no bullets left");
        return false;
    }

    public virtual void Reload()
    {
        MagazineBullets = MagazineSize;
    }
}