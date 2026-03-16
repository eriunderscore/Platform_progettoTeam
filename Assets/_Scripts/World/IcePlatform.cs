using UnityEngine;

public class IcePlatform : MonoBehaviour
{
    [Header("Ice Movement Modifiers")]
    public float iceAcceleration    = 4f;
    public float iceDeceleration    = 2f;
    public float iceAirAcceleration = 6f;

    public void ApplyIce(PlayerController3D player)
    {
        player.acceleration    = iceAcceleration;
        player.deceleration    = iceDeceleration;
        player.airAcceleration = iceAirAcceleration;
    }

    public void RemoveIce(PlayerController3D player, float origAccel, float origDecel, float origAirAccel)
    {
        player.acceleration    = origAccel;
        player.deceleration    = origDecel;
        player.airAcceleration = origAirAccel;
    }
}
