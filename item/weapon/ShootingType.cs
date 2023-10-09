namespace shootergame.item.weapon;

/// <summary>
/// What kind of behaviour a weapon should have when shoot is held.
/// </summary>
public enum ShootingType
{
    /// <summary>
    /// Shoot only once when shoot is held.
    /// </summary>
    Single,

    /// <summary>
    /// Constantly fire weapon at given rate when shoot is held.
    /// </summary>
    Auto
}