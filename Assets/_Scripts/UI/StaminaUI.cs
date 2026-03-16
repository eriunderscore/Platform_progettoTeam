using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    [Header("References")]
    public Transform           playerTransform;
    public Transform           cameraTransform;
    public Camera              hudCamera;
    public Image               fillImage;
    public Image               backgroundImage;
    [Tooltip("Riferimento al PlayerController3D — assegna dall'Inspector")]
    public PlayerController3D  player;

    [Header("Position")]
    public Vector3 offset = new Vector3(0.45f, 1.8f, 0f);

    [Header("Appearance")]
    public float circleSize          = 0.35f;
    public Color fullColor           = new Color(0.3f, 0.85f, 1f, 1f);
    public Color lowColor            = new Color(1f, 0.25f, 0.1f, 1f);
    public Color backgroundColor     = new Color(0.1f, 0.1f, 0.1f, 0.7f);
    public float lowStaminaThreshold = 0.3f;
    public float smoothSpeed         = 8f;

    [Header("Fade")]
    public float fadeInSpeed  = 5f;
    public float fadeOutSpeed = 3f;
    [Tooltip("Secondi di attesa prima del fade out dopo che le condizioni sono soddisfatte")]
    public float fadeOutDelay = 0.8f;

    // ── Private ───────────────────────────────────────────────
    private float       displayFill  = 1f;
    private float       targetFill   = 1f;
    private bool        shouldShow   = false;
    private float       hideTimer    = 0f;
    private Canvas      canvas;
    private CanvasGroup canvasGroup;

    // ──────────────────────────────────────────────────────────

    void Awake()
    {
        canvas      = GetComponentInChildren<Canvas>();
        canvasGroup = GetComponentInChildren<CanvasGroup>();

        if (canvas != null)
        {
            canvas.renderMode  = RenderMode.WorldSpace;
            canvas.worldCamera = hudCamera != null ? hudCamera : Camera.main;

            RectTransform rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(100f, 100f);
            rt.localScale = Vector3.one * (circleSize / 100f);
        }

        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    void LateUpdate()
    {
        if (playerTransform == null || cameraTransform == null) return;

        // ── Segui il player ───────────────────────────────────
        transform.position = playerTransform.position
                           + cameraTransform.right   * offset.x
                           + cameraTransform.up      * offset.y
                           + cameraTransform.forward * offset.z;

        // ── Billboard ─────────────────────────────────────────
        transform.rotation = Quaternion.LookRotation(
            transform.position - cameraTransform.position,
            cameraTransform.up);

        // ── Auto show/hide basato sullo stato del player ──────
        if (player != null)
        {
            bool staminaFull   = player.Stamina >= 1f;
            bool isGrounded    = player.IsGrounded;
            bool isClimbing    = player.IsClimbing;

            // Mostra se: stai arrampicando OPPURE stamina non è piena
            // Nascondi se: sei a terra + non stai arrampicando + stamina piena
            bool wantsVisible = isClimbing || !staminaFull;

            if (wantsVisible)
                Show();
            else
                Hide();
        }

        // ── Anima il fill ─────────────────────────────────────
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

        // ── Fade in/out ───────────────────────────────────────
        if (canvasGroup != null)
        {
            if (shouldShow)
            {
                canvasGroup.alpha = Mathf.MoveTowards(
                    canvasGroup.alpha, 1f, fadeInSpeed * Time.deltaTime);
            }
            else
            {
                // Aspetta fadeOutDelay prima di iniziare il fade out
                if (hideTimer > 0f)
                    hideTimer -= Time.deltaTime;
                else
                    canvasGroup.alpha = Mathf.MoveTowards(
                        canvasGroup.alpha, 0f, fadeOutSpeed * Time.deltaTime);
            }
        }
    }

    // ── Public API (chiamata anche da PlayerController3D) ─────
    public void SetStamina(float normalized) => targetFill = Mathf.Clamp01(normalized);

    public void Show()
    {
        shouldShow = true;
        hideTimer  = fadeOutDelay;
    }

    public void Hide()
    {
        shouldShow = false;
        // Non azzera hideTimer — lascia che il delay faccia il suo corso
    }
}
