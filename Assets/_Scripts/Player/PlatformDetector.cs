using UnityEngine;

/// <summary>
/// Attach this to the Player.
/// Detects FallingPlatform and IcePlatform beneath the player.
/// </summary>
public class PlatformDetector : MonoBehaviour
{
    [Header("Detection")]
    public float     detectionRadius   = 0.3f;
    public float     detectionDistance = 0.2f;
    public LayerMask platformLayer;

    private PlayerController3D player;

    // Tracking
    private FallingPlatform currentFalling;
    private IcePlatform     currentIce;
    private bool            wasGrounded = false;

    // Valori originali del player — salvati prima di applicare il ghiaccio
    private float origAcceleration;
    private float origDeceleration;
    private float origAirAcceleration;

    // ──────────────────────────────────────────────────────────

    void Awake()
    {
        player = GetComponent<PlayerController3D>();

        // Salva i valori originali una volta sola
        origAcceleration    = player.acceleration;
        origDeceleration    = player.deceleration;
        origAirAcceleration = player.airAcceleration;
    }

    void Update()
    {
        if (player == null) return;

        bool isGrounded = player.IsGrounded;

        if (isGrounded)
        {
            // Controlla la piattaforma sotto ogni frame (può cambiare)
            CheckPlatformBelow(isLanding: !wasGrounded);
        }
        else
        {
            // Il player è in aria — rimuovi effetti piattaforma
            ClearFalling();
            ClearIce();
        }

        wasGrounded = isGrounded;
    }

    // ──────────────────────────────────────────────────────────
    //  DETECTION
    // ──────────────────────────────────────────────────────────

    void CheckPlatformBelow(bool isLanding)
    {
        if (!Physics.SphereCast(transform.position, detectionRadius, Vector3.down,
            out RaycastHit hit, detectionDistance, platformLayer))
        {
            // Niente sotto → rimuovi effetti
            ClearFalling();
            ClearIce();
            return;
        }

        // ── Falling Platform ──────────────────────────────────
        FallingPlatform falling = hit.collider.GetComponent<FallingPlatform>();
        if (falling != null && isLanding && falling != currentFalling)
        {
            currentFalling = falling;
            falling.TriggerFall();
        }
        else if (falling == null)
        {
            ClearFalling();
        }

        // ── Ice Platform ──────────────────────────────────────
        IcePlatform ice = hit.collider.GetComponent<IcePlatform>();
        if (ice != null && ice != currentIce)
        {
            ClearIce(); // rimuovi vecchio ghiaccio se c'era
            currentIce = ice;
            ice.ApplyIce(player);
        }
        else if (ice == null)
        {
            ClearIce();
        }
    }

    // ──────────────────────────────────────────────────────────
    //  CLEAR
    // ──────────────────────────────────────────────────────────

    void ClearFalling()
    {
        currentFalling = null;
    }

    void ClearIce()
    {
        if (currentIce == null) return;
        currentIce.RemoveIce(player, origAcceleration, origDeceleration, origAirAcceleration);
        currentIce = null;
    }

    // Chiamato se il player muore — resetta tutto
    public void ResetPlatformEffects()
    {
        ClearIce();
        ClearFalling();
    }
}
