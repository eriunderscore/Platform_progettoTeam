using UnityEngine;

public class SquishEffect : MonoBehaviour
{
    [Header("Squish Settings")]
    public float landSquishAmount = 0.4f;
    public float jumpSquishAmount = 0.3f;
    public float dashSquishAmount = 0.3f;
    public float squishSpeed = 18f;
    public float recoverySpeed = 12f;

    [Header("Double Jump Spin")]
    public float spinSpeed = 1080f;  // degrees/sec — 1080 = full 360 in ~0.33s

    [Header("Reference")]
    public PlayerController3D player;

    private Vector3 targetScale = Vector3.one;
    private Vector3 currentScale = Vector3.one;
    private bool wasGrounded = false;

    // Spin state
    private bool isSpinning = false;
    private float spinProgress = 0f;   // accumulates degrees 0 → 360

    // ──────────────────────────────────────────────────────────

    void Update()
    {
        DetectEvents();
        AnimateScale();
        AnimateSpin();
    }

    void DetectEvents()
    {
        bool isGrounded = player.IsGrounded;

        if (isGrounded && !wasGrounded)
            targetScale = new Vector3(1f + landSquishAmount, 1f - landSquishAmount, 1f + landSquishAmount);

        if (!isGrounded && wasGrounded)
            targetScale = new Vector3(1f - jumpSquishAmount, 1f + jumpSquishAmount, 1f - jumpSquishAmount);

        wasGrounded = isGrounded;
    }

    void AnimateScale()
    {
        currentScale = Vector3.Lerp(currentScale, targetScale, squishSpeed * Time.deltaTime);
        targetScale = Vector3.Lerp(targetScale, Vector3.one, recoverySpeed * Time.deltaTime);
        transform.localScale = currentScale;
    }

    void AnimateSpin()
    {
        if (!isSpinning) return;

        spinProgress += spinSpeed * Time.deltaTime;

        // X axis = forward somersault (vertical 360)
        transform.localRotation = Quaternion.Euler(spinProgress, 0f, 0f);

        if (spinProgress >= 360f)
        {
            spinProgress = 0f;
            isSpinning = false;
            transform.localRotation = Quaternion.identity;  // snap clean to zero
        }
    }

    // ──────────────────────────────────────────────────────────
    //  PUBLIC TRIGGERS
    // ──────────────────────────────────────────────────────────

    public void TriggerJumpSquish()
    {
        targetScale = new Vector3(1f - jumpSquishAmount, 1f + jumpSquishAmount, 1f - jumpSquishAmount);
    }

    public void TriggerDashSquish()
    {
        targetScale = new Vector3(1f + dashSquishAmount, 1f - dashSquishAmount * 0.5f, 1f + dashSquishAmount);
    }

    /// <summary>Called by PlayerController3D.DoubleJump() — spins the visual 360° vertically.</summary>
    public void TriggerDoubleJumpSpin()
    {
        spinProgress = 0f;
        isSpinning = true;
        targetScale = new Vector3(1f - jumpSquishAmount, 1f + jumpSquishAmount, 1f - jumpSquishAmount);
    }
}