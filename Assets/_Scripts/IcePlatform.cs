using UnityEngine;

/// <summary>
/// Attach this to the platform GameObject.
/// Detection is handled by PlatformDetector on the Player.
/// Call ApplyIce() and RemoveIce() from PlatformDetector.
/// </summary>
public class IcePlatform : MonoBehaviour
{
    [Header("Ice Movement Modifiers")]
    public float iceAcceleration = 4f;
    public float iceDeceleration = 2f;
    public float iceAirAcceleration = 6f;

    // Called by PlatformDetector when player lands on this platform
    public void ApplyIce(PlayerController3D player)
    {
        player.acceleration = iceAcceleration;
        player.deceleration = iceDeceleration;
        player.airAcceleration = iceAirAcceleration;
    }

    // Called by PlatformDetector when player leaves this platform
    public void RemoveIce(PlayerController3D player, float origAccel, float origDecel, float origAirAccel)
    {
        player.acceleration = origAccel;
        player.deceleration = origDecel;
        player.airAcceleration = origAirAccel;
    }
}