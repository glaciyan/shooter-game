namespace shootergame.item.weapon;

public class Magazine
{
    public readonly int MaxCapacity;

    private int _bullets;
    
    public Magazine(int maxCapacity, bool filled = true)
    {
        MaxCapacity = maxCapacity;
        if (filled)
        {
            _bullets = MaxCapacity;
        }
    }

    /// <summary>
    /// Adds bullets to this magazine.
    /// </summary>
    /// <param name="amount">The amount of bullets to be added to this magazine.</param>
    /// <returns>The amount of bullets that were able to be added.</returns>
    public int AddBullets(int amount)
    {
        if (_bullets + amount <= MaxCapacity)
        {
            _bullets += amount;
            return amount;
        }
        else
        {
            var b = MaxCapacity - _bullets;
            _bullets = MaxCapacity;
            return b;
        }
    }

    /// <summary>
    /// Adds a single bullet from this magazine.
    /// </summary>
    /// <returns>True if it was possible to add a bullet, false otherwise.</returns>
    public bool AddBullet()
    {
        return AddBullets(1) == 1;
    }

    /// <summary>
    /// Removes bullets from this magazine.
    /// </summary>
    /// <param name="amount">The amount of bullets to be removed from this magazine.</param>
    /// <returns>The amount of bullets that have been able to be removed.</returns>
    public int RemoveBullets(int amount)
    {
        if (amount <= _bullets)
        {
            _bullets -= amount;
            return amount;
        }
        else
        {
            var b = _bullets;
            _bullets = 0;
            return b;
        }
    }

    /// <summary>
    /// Removes a single bullet from this magazine.
    /// </summary>
    /// <returns>True if it was possible to remove a bullet, false otherwise.</returns>
    public bool RemoveBullet()
    {
        return RemoveBullets(1) == 1;
    }
}