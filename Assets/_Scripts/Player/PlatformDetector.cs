using UnityEngine;

/// <summary>
/// Attach this to the Player.
/// Usa OnControllerColliderHit per rilevare qualsiasi tipo di collider
/// (BoxCollider, MeshCollider, SphereCollider, ecc.)
/// </summary>
public class PlatformDetector : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("LayerMask delle piattaforme speciali (FallingPlatform, IcePlatform)")]
    public LayerMask platformLayer;

    private PlayerController3D player;

    private FallingPlatform currentFalling;
    private IcePlatform currentIce;
    private bool wasGrounded = false;

    private float origAcceleration;
    private float origDeceleration;
    private float origAirAcceleration;

    // ──────────────────────────────────────────────────────────

    void Awake()
    {
        player = GetComponent<PlayerController3D>();
        origAcceleration = player.acceleration;
        origDeceleration = player.deceleration;
        origAirAcceleration = player.airAcceleration;
    }

    void Update()
    {
        if (player == null) return;

        bool isGrounded = player.IsGrounded;

        // Quando il player lascia terra → rimuovi effetti
        if (!isGrounded && wasGrounded)
        {
            ClearFalling();
            ClearIce();
        }

        wasGrounded = isGrounded;
    }

    // ── Chiamato da PlayerController3D.OnControllerColliderHit ──
    public void OnHit(ControllerColliderHit hit, bool isLanding)
    {
        int objLayer = hit.collider.gameObject.layer;
        bool inMask = (platformLayer.value & (1 << objLayer)) != 0;
        Debug.Log($"[PlatformDetector] obj:{hit.collider.name} layer:{objLayer} inMask:{inMask} maskValue:{platformLayer.value} normalY:{hit.normal.y}");

        if (!inMask) return;
        if (hit.normal.y < 0.5f) return;

        // ── Falling Platform ──────────────────────────────────
        FallingPlatform falling = hit.collider.GetComponent<FallingPlatform>();
        if (falling != null && falling != currentFalling)
        {
            currentFalling = falling;
            if (isLanding) falling.TriggerFall();
        }

        // ── Ice Platform ──────────────────────────────────────
        IcePlatform ice = hit.collider.GetComponent<IcePlatform>();
        if (ice != null && ice != currentIce)
        {
            ClearIce();
            currentIce = ice;
            ice.ApplyIce(player);
        }
        else if (ice == null && currentIce != null)
        {
            // Non stiamo più toccando ghiaccio
            ClearIce();
        }
    }

    // ──────────────────────────────────────────────────────────

    void ClearFalling() => currentFalling = null;

    void ClearIce()
    {
        if (currentIce == null) return;
        currentIce.RemoveIce(player, origAcceleration, origDeceleration, origAirAcceleration);
        currentIce = null;
    }

    public void ResetPlatformEffects()
    {
        ClearIce();
        ClearFalling();
    }
}