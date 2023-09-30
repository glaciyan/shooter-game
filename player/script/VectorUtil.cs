using Godot;

namespace shootergame.player.script;

public static class VectorUtil
{
    public static Vector3 NormalizedOr(this Vector3 value, Vector3 onZeroValue)
    {
        var lq = value.LengthSquared();
        return lq == 0.0f ? onZeroValue : value / Mathf.Sqrt(lq);
    }}