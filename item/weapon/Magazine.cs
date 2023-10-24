namespace shootergame.item.weapon;

public class Magazine
{
    public readonly int Capacity;
    public int Bullets { get; private set; }

    public Magazine(int capacity, bool filled = true)
    {
        Capacity = capacity;
        if (filled)
        {
            Bullets = Capacity;
        }
    }

    /// <summary>
    /// Adds bullets to this magazine.
    /// </summary>
    /// <param name="amount">The amount of bullets to be added to this magazine.</param>
    /// <returns>The amount of bullets that were able to be added.</returns>
    public int AddBullets(int amount)
    {
        if (Bullets + amount <= Capacity)
        {
            Bullets += amount;
            return amount;
        }
        else
        {
            var b = Capacity - Bullets;
            Bullets = Capacity;
            return b;
        }
    }

    /// <summary>
    /// Adds a single bullet from this magazine.
    /// </summary>
    /// <returns>True if it was possible to add a bullet, false otherwise.</returns>
    public bool AddBullet() => AddBullets(1) == 1;

    /// <summary>
    /// Removes bullets from this magazine.
    /// </summary>
    /// <param name="amount">The amount of bullets to be removed from this magazine.</param>
    /// <returns>The amount of bullets that have been able to be removed.</returns>
    public int RemoveBullets(int amount)
    {
        if (amount <= Bullets)
        {
            Bullets -= amount;
            return amount;
        }
        else
        {
            var b = Bullets;
            Bullets = 0;
            return b;
        }
    }

    /// <summary>
    /// Removes a single bullet from this magazine.
    /// </summary>
    /// <returns>True if it was possible to remove a bullet, false otherwise.</returns>
    public bool RemoveBullet() => RemoveBullets(1) == 1;

    /// <summary>
    /// Moves bullets from one magazine to the other.
    /// </summary>
    /// <param name="from">Magazine to move bullets from.</param>
    /// <returns>The amount of bullets moved.</returns>
    public int MoveBullets(Magazine from) => from.RemoveBullets(AddBullets(from.Bullets));
}