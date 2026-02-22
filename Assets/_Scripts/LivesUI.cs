using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SETUP:
/// 1. Hierarchy: right-click → UI → Canvas
///    Render Mode = Screen Space - Overlay
/// 2. Right-click Canvas → Create Empty → rename "LivesUI"
///    Add THIS script to "LivesUI"
/// 3. Fill in the Player field in the Inspector
/// That's it. The script builds everything else.
/// </summary>
public class LivesUI : MonoBehaviour
{
    public static LivesUI Instance { get; private set; }

    [Header("References")]
    public PlayerController3D player;

    [Header("Settings")]
    public int maxLives = 3;
    public float showOnDeathTime = 3f;
    public float idleTimeToShow = 3f;
    public float idleShowTime = 2f;
    public float animDuration = 0.3f;
    public float slideAmount = 50f;

    [Header("Heart Appearance")]
    public float heartSize = 40f;
    public float heartSpacing = 10f;
    public float marginLeft = 20f;
    public float marginBottom = 20f;
    public Color aliveColor = new Color(0.9f, 0.1f, 0.1f, 1f);
    public Color deadColor = new Color(0.25f, 0.25f, 0.25f, 1f);

    // ── Private ───────────────────────────────────────────────
    private RectTransform[] heartRects;
    private Image[] heartImages;
    private CanvasGroup group;
    private RectTransform rt;
    private int currentLives;
    private float hideTimer;
    private float idleTimer;
    private bool visible;
    private Coroutine anim;

    // ──────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        currentLives = maxLives;

        // RectTransform of this object — anchor to bottom-left
        rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(marginLeft, marginBottom);
        rt.sizeDelta = new Vector2(
            maxLives * heartSize + (maxLives - 1) * heartSpacing,
            heartSize);

        // CanvasGroup for fade
        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        // Build hearts
        heartRects = new RectTransform[maxLives];
        heartImages = new Image[maxLives];

        for (int i = 0; i < maxLives; i++)
        {
            GameObject go = new GameObject("Heart_" + i);
            go.transform.SetParent(transform, false);

            RectTransform hrt = go.AddComponent<RectTransform>();
            hrt.anchorMin = Vector2.zero;
            hrt.anchorMax = Vector2.zero;
            hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.sizeDelta = new Vector2(heartSize, heartSize);
            hrt.anchoredPosition = new Vector2(
                i * (heartSize + heartSpacing) + heartSize * 0.5f,
                heartSize * 0.5f);

            Image img = go.AddComponent<Image>();
            img.color = aliveColor;

            // White square texture
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            img.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));

            heartRects[i] = hrt;
            heartImages[i] = img;
        }

        // Start hidden (shifted down)
        rt.anchoredPosition = new Vector2(marginLeft, marginBottom - slideAmount);
    }

    // ──────────────────────────────────────────────────────────
    //  UPDATE — idle detection
    // ──────────────────────────────────────────────────────────
    void Update()
    {
        if (player == null) return;

        bool anyInput = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f
                     || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f
                     || Input.GetButton("Jump")
                     || Input.GetKey(KeyCode.LeftShift);

        if (anyInput)
        {
            idleTimer = 0f;
            // Hide as soon as player moves — but only if shown from idle (not from death)
            if (visible && hideTimer <= 0f)
            {
                TriggerHide();
                idleTimer = 0f;  // reset so next idle period can trigger again
            }
        }
        else
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleTimeToShow && !visible)
            {
                TriggerShow(0f);   // 0 = stay until player moves
                idleTimer = float.MinValue;  // prevent re-triggering
            }
        }

        // Death timer auto-hide
        if (visible && hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f) TriggerHide();
        }
    }

    // ──────────────────────────────────────────────────────────
    //  PUBLIC
    // ──────────────────────────────────────────────────────────
    public void OnPlayerDied(int livesLeft)
    {
        currentLives = livesLeft;
        RefreshHearts();
        TriggerShow(showOnDeathTime);
    }

    public void OnGameReset(int lives)
    {
        currentLives = lives;
        RefreshHearts();
    }

    // ──────────────────────────────────────────────────────────
    //  HEARTS
    // ──────────────────────────────────────────────────────────
    void RefreshHearts()
    {
        if (heartImages == null) return;
        for (int i = 0; i < heartImages.Length; i++)
            heartImages[i].color = i < currentLives ? aliveColor : deadColor;
    }

    // ──────────────────────────────────────────────────────────
    //  ANIMATION
    // ──────────────────────────────────────────────────────────
    void TriggerShow(float duration)
    {
        hideTimer = duration;
        if (visible) return;
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Animate(true));
    }

    void TriggerHide()
    {
        if (!visible) return;
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Animate(false));
    }

    IEnumerator Animate(bool show)
    {
        visible = show;

        Vector2 startPos = show
            ? new Vector2(marginLeft, marginBottom - slideAmount)
            : new Vector2(marginLeft, marginBottom);
        Vector2 endPos = show
            ? new Vector2(marginLeft, marginBottom)
            : new Vector2(marginLeft, marginBottom - slideAmount);

        float startAlpha = show ? 0f : 1f;
        float endAlpha = show ? 1f : 0f;

        float t = 0f;
        while (t < animDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / animDuration);
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, p);
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, p);
            yield return null;
        }

        rt.anchoredPosition = endPos;
        group.alpha = endAlpha;
        anim = null;
    }
}