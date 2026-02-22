using UnityEngine;

/// <summary>
/// Attach this to the Player GameObject.
/// Uses OnControllerColliderHit (the correct callback for CharacterController)
/// to detect FallingPlatform and IcePlatform beneath the player's feet.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlatformDetector : MonoBehaviour
{
    // ── Private ───────────────────────────────────────────────
    private PlayerController3D player;

    // Ice tracking
    private IcePlatform currentIce = null;
    private float origAcceleration;
    private float origDeceleration;
    private float origAirAcceleration;

    // ──────────────────────────────────────────────────────────

    void Awake()
    {
        player = GetComponent<PlayerController3D>();

        // Cache original movement values once at start
        origAcceleration = player.acceleration;
        origDeceleration = player.deceleration;
        origAirAcceleration = player.airAcceleration;
    }

    // ──────────────────────────────────────────────────────────
    //  MAIN DETECTION — fires every frame the CC touches geometry
    // ──────────────────────────────────────────────────────────
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Only care about surfaces below the player (landing on top)
        // hit.moveDirection.y < -0.5 means we were moving downward into it
        if (hit.moveDirection.y > -0.3f) return;

        // ── Falling Platform ──────────────────────────────────
        FallingPlatform falling = hit.collider.GetComponent<FallingPlatform>();
        if (falling != null)
            falling.TriggerFall();

        // ── Ice Platform ──────────────────────────────────────
        IcePlatform ice = hit.collider.GetComponent<IcePlatform>();

        if (ice != null && ice != currentIce)
        {
            // Left a previous ice platform without triggering exit — clean up first
            if (currentIce != null)
                currentIce.RemoveIce(player, origAcceleration, origDeceleration, origAirAcceleration);

            // Apply new ice
            currentIce = ice;
            ice.ApplyIce(player);
        }
    }

    // ──────────────────────────────────────────────────────────
    //  CHECK EVERY FRAME IF PLAYER IS STILL ON ICE
    //  OnControllerColliderHit only fires while moving,
    //  so we also need to detect when the player walks OFF ice
    // ──────────────────────────────────────────────────────────
    void Update()
    {
        if (currentIce == null) return;

        // Check if the ice platform is still directly below the player
        bool stillOnIce = Physics.Raycast(
            transform.position,
            Vector3.down,
            out RaycastHit hit,
            1.2f);

        if (!stillOnIce || hit.collider.GetComponent<IcePlatform>() != currentIce)
        {
            // Player left the ice — restore values
            currentIce.RemoveIce(player, origAcceleration, origDeceleration, origAirAcceleration);
            currentIce = null;
        }
    }
}