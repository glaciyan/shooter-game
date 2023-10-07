using Godot;

namespace shootergame.player.script;

public static class VectorUtil
{
    public static Vector3 NormalizedOr(this Vector3 value, Vector3 onZeroValue)
    {
        var lq = value.LengthSquared();
        return lq == 0.0f ? onZeroValue : value / Mathf.Sqrt(lq);
    }

    public static Vector3 ToVector3Flat(this Vector2 v, float y = 0f) => new(v.X, y, v.Y);
    public static Vector2 ToVector2Flat(this Vector3 v) => new(v.X, v.Z);

    /// <summary>
    /// Limits the lenght of the vector to 1.
    /// </summary>
    /// <param name="v">Target vector</param>
    /// <returns>Vector of lenght 1 if lenght was greater than 1, otherwise the same vector.</returns>
    public static Vector3 FastLimit(this Vector3 v)
    {
        var lq = v.LengthSquared();
        return lq > 1f ? v / Mathf.Sqrt(lq) : v;
    }

    /// <summary>
    /// Limits the lenght of the vector to 1.
    /// </summary>
    /// <param name="v">Target vector</param>
    /// <returns>Vector of lenght 1 if lenght was greater than 1, otherwise the same vector.</returns>
    public static Vector2 FastLimit(this Vector2 v)
    {
        var lq = v.LengthSquared();
        return lq > 1f ? v / Mathf.Sqrt(lq) : v;
    }

    // visual explanation: https://www.geogebra.org/calculator/hwqqt9ts
    public static Vector2 RotateToBasis(this Vector2 v, Vector2 basis) =>
        new(basis.Y * v.X + basis.X * v.Y,
            -basis.X * v.X + basis.Y * v.Y);

    public static Vector2 InvertRotationToBasis(this Vector2 v, Vector2 basis) =>
        new(basis.Y * v.X - basis.X * v.Y,
            basis.X * v.X + basis.Y * v.Y);
}