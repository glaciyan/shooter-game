using Godot;
using Godot.Collections;
using shootergame.item.weapon;

namespace shootergame.player.script;

public partial class PlayerWeapons : Node3D
{
    [Export]
    public PlayerCharacter Player;

    [Export]
    public ShootingWeapon CurrentWeapon;

    [Export]
    public Array<ShootingWeapon> Loadout { get; set; }

    [ExportGroup("Weapons")]
    [Export]
    public PackedScene Pistol;

    [Export]
    public PackedScene Rifle;

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("reload"))
        {
            CurrentWeapon.Reload();
        }
        else if (@event.IsActionPressed("slot_0"))
        {
            SwitchWeapon(0);
        }
        else if (@event.IsActionPressed("slot_1"))
        {
            SwitchWeapon(1);
        }
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionPressed("shoot"))
        {
            CurrentWeapon.RequestShooting(Player);
        }
        else
        {
            CurrentWeapon.StopShooting();
        }
    }

    private void SwitchWeapon(int slot)
    {
        var weapon = Loadout[slot];
        CurrentWeapon.Visible = false;
        CurrentWeapon = weapon;
        CurrentWeapon.Visible = true;
    }
}