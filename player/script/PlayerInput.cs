using Godot;

namespace shootergame.player.script;

public partial class PlayerInput : GodotObject
{
    public Vector2 MovementInput;
    public Vector2 VirtualViewAngle;

    public void AddViewAngle(Vector2 delta, float sensitivity)
    {
        VirtualViewAngle.X += delta.X * sensitivity;
        // normalize yaw angle in range [0, 360]
        VirtualViewAngle.X %= 360f;
        if (VirtualViewAngle.X < 0) VirtualViewAngle.X += 360f;

        // clamp y for looking down and up all the way
        VirtualViewAngle.Y += delta.Y * sensitivity;
        VirtualViewAngle.Y = Mathf.Clamp(VirtualViewAngle.Y, -90f, 90f);
    }

    public Vector3 GetRequestedMovementDirection()
    {
        var yaw = Mathf.DegToRad(-VirtualViewAngle.X);

        // rotate input by yaw with rotation matrix
        return new Vector3(MovementInput.X * Mathf.Cos(yaw) - MovementInput.Y * Mathf.Sin(yaw), 0f,
            MovementInput.X * Mathf.Sin(yaw) + MovementInput.Y * Mathf.Cos(yaw));
    }
    
    public Vector3 GetLineOfSight()
    {
        var yawRadians = Mathf.DegToRad(VirtualViewAngle.X);
        var pitchRadians = Mathf.DegToRad(VirtualViewAngle.Y);

        // Precompute cos and sin of pitch
        var cosPitch = Mathf.Cos(pitchRadians);
        var sinPitch = Mathf.Sin(pitchRadians);

        // Precompute sin and cos of yaw
        var sinYaw = Mathf.Sin(yawRadians);
        var cosYaw = Mathf.Cos(yawRadians);

        // Calculate aiming vector (including Y component)
        return new Vector3(
            cosPitch * sinYaw,
            sinPitch,
            cosPitch * cosYaw
        );
    }
}