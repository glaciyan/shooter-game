using Godot;
using shootergame.bullet;
using shootergame.player.script;

namespace shootergame.item.weapon;

public abstract partial class ShootingWeapon : Weapon
{
    [Export]
    public int MagazineCapacity = 12;

    [Export]
    public PackedScene Bullet;

    [Export]
    public ShootingType ShootingType;

    [Export]
    public double FireRateRpm = 60;

    private Timer _fireRateTimer;
    private bool _onCooldown;
    protected Magazine Magazine;

    public override void _Ready()
    {
        Magazine = new Magazine(MagazineCapacity);

        _fireRateTimer = new Timer()
        {
            OneShot = true,
            Autostart = false,
            WaitTime = 60.0 / FireRateRpm,
        };

        _fireRateTimer.Timeout += () =>
        {
            _onCooldown = false;
            if (ShootingType == ShootingType.Auto)
            {
                if (_requestShoot) Shoot();
            }
        };
        AddChild(_fireRateTimer);
    }

    private CharacterController _current;
    private bool _requestShoot;

    public virtual void RequestShooting(CharacterController player)
    {
        if (_requestShoot) return;
        if (_onCooldown) return;

        _current = player;
        _requestShoot = true;

        Shoot();
    }

    public virtual void StopShooting()
    {
        _requestShoot = false;
    }

    protected virtual void Shoot()
    {
        if (Magazine.RemoveBullet())
        {
            var bullet = Bullet.Instantiate<DebugBullet>();
            AddChild(bullet);
            bullet.Shoot(_current.AimOrigin, _current.AimVector);
            _onCooldown = true;
            _fireRateTimer.Start();
        }
        else
        {
            OnNoAmmoShoot();
        }
    }

    public virtual void Reload()
    {
        Magazine.AddBullets(MagazineCapacity);
    }

    protected virtual void OnNoAmmoShoot()
    {
        // TODO clicking sound for weapon
    }
}