using Godot;
using shootergame.weapon;

namespace shootergame.player.script;

public partial class CharacterWeapons : Node3D
{
    [Export]
    public PlayerCharacter Player;
    
    [Export]
    public Weapon CurrentWeapon;

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("shoot"))
        {
            CurrentWeapon.Shoot(Player.AimOrigin, Player.AimVector);
        } else if (@event.IsActionPressed("reload"))
        {
            CurrentWeapon.Reload();
        }
    }
}