using UnityEngine;
using System.Collections;

public class SquishEffect : MonoBehaviour
{
    [Header("Squish Settings")]
    public float landSquishAmount = 0.4f;
    public float jumpSquishAmount = 0.3f;
    public float dashSquishAmount = 0.3f;
    public float squishSpeed = 18f;
    public float recoverySpeed = 12f;

    [Header("Slope Fix")]
    [Tooltip("Secondi minimi in aria prima che lo squish di salto si attivi — evita falsi positivi su pendii")]
    public float minAirTimeForJumpSquish = 0.12f;

    [Header("Reference")]
    public PlayerController3D player;

    private Vector3 targetScale = Vector3.one;
    private Vector3 currentScale = Vector3.one;
    private bool wasGrounded = false;
    private bool wasDashing = false;
    private float airTimer = 0f;

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
        bool isDashing = player.IsDashing;

        // Traccia tempo in aria
        if (!isGrounded) airTimer += Time.deltaTime;
        else airTimer = 0f;

        // Atterraggio — solo se era in aria abbastanza
        if (isGrounded && !wasGrounded && airTimer > minAirTimeForJumpSquish - Time.deltaTime)
            TriggerLandSquish();

        // Salto — aspetta un po' prima di triggerare per evitare slope glitch
        if (!isGrounded && wasGrounded && !isDashing)
            StartCoroutine(DelayedJumpSquish());

        // Dash
        if (isDashing && !wasDashing)
            TriggerDashSquish();

        wasGrounded = isGrounded;
        wasDashing = isDashing;
    }

    IEnumerator DelayedJumpSquish()
    {
        yield return new WaitForSeconds(minAirTimeForJumpSquish);

        // Applica solo se siamo ancora in aria dopo il delay
        if (player != null && !player.IsGrounded)
            targetScale = new Vector3(
                1f - jumpSquishAmount,
                1f + jumpSquishAmount,
                1f - jumpSquishAmount);
    }

    void AnimateScale()
    {
        currentScale = Vector3.Lerp(currentScale, targetScale, squishSpeed * Time.deltaTime);
        targetScale = Vector3.Lerp(targetScale, Vector3.one, recoverySpeed * Time.deltaTime);
        transform.localScale = currentScale;
    }

    // ── Public triggers ───────────────────────────────────────

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