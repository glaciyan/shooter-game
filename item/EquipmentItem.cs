namespace shootergame.item;

public abstract partial class EquipmentItem : Item
{
    public enum EquipSlot
    {
        Hands
    }

    public abstract EquipSlot Slot { get; }
}