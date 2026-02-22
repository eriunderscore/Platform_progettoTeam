using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space stamina circle that floats top-right of the player,
/// always faces the camera, and renders on top of all geometry
/// via a dedicated HUD camera on a separate layer.
/// </summary>
public class StaminaUI : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public Transform cameraTransform;
    public Camera hudCamera;       // the dedicated HUD camera
    public Image fillImage;       // the colored arc (Filled, Radial360, origin Top)
    public Image backgroundImage; // the dark ring behind

    [Header("Position")]
    public Vector3 offset = new Vector3(0.45f, 1.8f, 0f); // top-right of player, camera-relative

    [Header("Appearance")]
    public float circleSize = 0.35f;
    public Color fullColor = new Color(0.3f, 0.85f, 1f, 1f);  // cyan
    public Color lowColor = new Color(1f, 0.25f, 0.1f, 1f); // red
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.7f);
    public float lowStaminaThreshold = 0.3f;
    public float smoothSpeed = 8f;

    [Header("Fade")]
    public float fadeInSpeed = 5f;
    public float fadeOutSpeed = 3f;
    public float fadeOutDelay = 0.8f;

    // ── Private ───────────────────────────────────────────────
    private float displayFill = 1f;
    private float targetFill = 1f;
    private bool shouldShow = false;
    private float hideTimer = 0f;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    // ──────────────────────────────────────────────────────────

    void Awake()
    {
        canvas = GetComponentInChildren<Canvas>();
        canvasGroup = GetComponentInChildren<CanvasGroup>();

        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = hudCamera != null ? hudCamera : Camera.main;

            RectTransform rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100f, 100f);
            rt.localScale = Vector3.one * (circleSize / 100f);
        }

        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    void LateUpdate()
    {
        if (playerTransform == null || cameraTransform == null) return;

        // Follow player — offset in camera space so it's always top-right
        transform.position = playerTransform.position
                           + cameraTransform.right * offset.x
                           + cameraTransform.up * offset.y
                           + cameraTransform.forward * offset.z;

        // Billboard — always face the camera
        transform.rotation = Quaternion.LookRotation(
            transform.position - cameraTransform.position,
            cameraTransform.up);

        // Animate fill
        displayFill = Mathf.Lerp(displayFill, targetFill, smoothSpeed * Time.deltaTime);

        if (fillImage != null)
        {
            fillImage.fillAmount = displayFill;
            fillImage.color = displayFill <= lowStaminaThreshold
                ? lowColor
                : Color.Lerp(lowColor, fullColor,
                    Mathf.InverseLerp(lowStaminaThreshold, 1f, displayFill));
        }

        if (backgroundImage != null)
            backgroundImage.color = backgroundColor;

        // Fade
        if (canvasGroup != null)
        {
            if (shouldShow)
            {
                canvasGroup.alpha = Mathf.MoveTowards(
                    canvasGroup.alpha, 1f, fadeInSpeed * Time.deltaTime);
            }
            else
            {
                if (hideTimer > 0f) hideTimer -= Time.deltaTime;
                else canvasGroup.alpha = Mathf.MoveTowards(
                    canvasGroup.alpha, 0f, fadeOutSpeed * Time.deltaTime);
            }
        }
    }

    // ── Public API (called by WallClimb) ──────────────────────
    public void SetStamina(float normalized) => targetFill = Mathf.Clamp01(normalized);
    public void Show() { shouldShow = true; hideTimer = fadeOutDelay; }
    public void Hide() { shouldShow = false; hideTimer = fadeOutDelay; }
}