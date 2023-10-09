using Godot;
using shootergame.bullet;

namespace shootergame.weapon.pistol;

public partial class Pistol : Weapon
{
    public override void Shoot(Vector3 from, Vector3 direction)
    {
        if (MagazineBullets-- > 0)
        {
            var bullet = Bullet.Instantiate<DebugBullet>();
            AddChild(bullet);
            bullet.Shoot(from, direction);
        }
        else
        {
            GD.Print("no bullets left");
        }
    }

    public override void Reload()
    {
        MagazineBullets = MagazineSize;
    }
}