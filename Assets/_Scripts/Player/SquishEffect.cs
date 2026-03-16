using UnityEngine;

public class SquishEffect : MonoBehaviour
{
    [Header("Squish Settings")]
    public float landSquishAmount = 0.4f;
    public float jumpSquishAmount = 0.3f;
    public float dashSquishAmount = 0.3f;
    public float squishSpeed      = 18f;
    public float recoverySpeed    = 12f;

    [Header("Reference")]
    [Tooltip("Trascina qui il PlayerController3D del tuo Player")]
    public PlayerController3D player;

    private Vector3 targetScale  = Vector3.one;
    private Vector3 currentScale = Vector3.one;
    private bool    wasGrounded  = false;
    private bool    wasDashing   = false;

    // ──────────────────────────────────────────────────────────

    void Update()
    {
        DetectEvents();
        AnimateScale();
    }

    void DetectEvents()
    {
        if (player == null) return;

        bool isGrounded = player.IsGrounded;
        bool isDashing  = player.IsDashing;

        // Atterraggio
        if (isGrounded && !wasGrounded)
            targetScale = new Vector3(1f + landSquishAmount, 1f - landSquishAmount, 1f + landSquishAmount);

        // Salto (lascia terra)
        if (!isGrounded && wasGrounded && !isDashing)
            targetScale = new Vector3(1f - jumpSquishAmount, 1f + jumpSquishAmount, 1f - jumpSquishAmount);

        // Inizio dash
        if (isDashing && !wasDashing)
            TriggerDashSquish();

        wasGrounded = isGrounded;
        wasDashing  = isDashing;
    }

    void AnimateScale()
    {
        currentScale         = Vector3.Lerp(currentScale, targetScale, squishSpeed * Time.deltaTime);
        targetScale          = Vector3.Lerp(targetScale, Vector3.one,  recoverySpeed * Time.deltaTime);
        transform.localScale = currentScale;
    }

    // ──────────────────────────────────────────────────────────
    //  PUBLIC TRIGGERS — chiamabili da altri script se necessario
    // ──────────────────────────────────────────────────────────

    public void TriggerJumpSquish()
    {
        targetScale = new Vector3(1f - jumpSquishAmount, 1f + jumpSquishAmount, 1f - jumpSquishAmount);
    }

    public void TriggerDashSquish()
    {
        targetScale = new Vector3(1f + dashSquishAmount, 1f - dashSquishAmount * 0.5f, 1f + dashSquishAmount);
    }

    public void TriggerLandSquish()
    {
        targetScale = new Vector3(1f + landSquishAmount, 1f - landSquishAmount, 1f + landSquishAmount);
    }
}
