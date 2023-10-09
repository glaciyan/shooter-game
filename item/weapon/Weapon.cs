namespace shootergame.item.weapon;

public abstract partial class Weapon : EquipmentItem
{
    public override EquipSlot Slot => EquipSlot.Hands;
}