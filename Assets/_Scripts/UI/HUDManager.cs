// ============================================================
//  HUDManager.cs
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR — Manager References
    // ══════════════════════════════════════════════════════════

    [Header("── Manager References ──────────────────────────")]
    [Tooltip("Trascina il GameObject ContenitoreMonete")]
    public ContenitoreMonete contenitoreMonete;
    [Tooltip("Trascina il GameObject ContenitoreChiavi")]
    public ContenitoreChiavi contenitoreChiavi;
    [Tooltip("Trascina il GameObject LivesManager")]
    public LivesManager livesManager;

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR — Timer
    // ══════════════════════════════════════════════════════════

    [Header("── Timer (Top Left) ────────────────────────────")]
    public TextMeshProUGUI timerText;
    public bool pauseTimerDuringDialogue = true;

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR — Monete
    // ══════════════════════════════════════════════════════════

    [Header("── Monete (Top Right) ──────────────────────────")]
    public TextMeshProUGUI coinText;
    public Image coinIcon;

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR — Chiavi
    // ══════════════════════════════════════════════════════════

    [Header("── Chiavi (Top Right) ──────────────────────────")]
    public TextMeshProUGUI keyText;
    public Image keyIcon;

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR — Vite
    // ══════════════════════════════════════════════════════════

    [Header("── Vite (Bottom Right) ─────────────────────────")]
    public Image livesIcon;
    public TextMeshProUGUI livesText;

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR — Ice Overlay
    // ══════════════════════════════════════════════════════════

    [Header("── Ice Overlay (PowerUp) ───────────────────────")]
    public Image iceOverlayImage;

    // ══════════════════════════════════════════════════════════
    //  STATO INTERNO
    // ══════════════════════════════════════════════════════════

    private float elapsedTime = 0f;
    private bool timerRunning = true;

    // ══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        ContenitoreMonete.OnMoneteChanged += UpdateCoins;
        ContenitoreChiavi.OnChiaviChanged += UpdateKeys;
        LivesManager.OnLivesChanged += UpdateLivesDisplay;
    }

    void OnDisable()
    {
        ContenitoreMonete.OnMoneteChanged -= UpdateCoins;
        ContenitoreChiavi.OnChiaviChanged -= UpdateKeys;
        LivesManager.OnLivesChanged -= UpdateLivesDisplay;
    }

    void Start()
    {
        HideIceOverlay();
        RefreshAll();
    }

    void Update()
    {
        if (!timerRunning) return;
        elapsedTime += Time.deltaTime;
        UpdateTimerDisplay(elapsedTime);
    }

    // ══════════════════════════════════════════════════════════
    //  REFRESH ALL — sincronizza tutto dai manager
    // ══════════════════════════════════════════════════════════

    public void RefreshAll()
    {
        // Monete
        if (contenitoreMonete != null)
            UpdateCoins(contenitoreMonete.Monete);

        // Chiavi
        if (contenitoreChiavi != null)
            UpdateKeys(contenitoreChiavi.Chiavi, contenitoreChiavi.ChiaviTotali);

        // Vite
        if (livesManager != null)
            UpdateLivesDisplay(livesManager.Lives);

        UpdateTimerDisplay(elapsedTime);
    }

    // ══════════════════════════════════════════════════════════
    //  TIMER
    // ══════════════════════════════════════════════════════════

    void UpdateTimerDisplay(float seconds)
    {
        if (timerText == null) return;
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        timerText.text = $"{mins:00}:{secs:00}";
    }

    public void PauseTimer() => timerRunning = false;
    public void ResumeTimer() => timerRunning = true;
    public void ResetTimer() { elapsedTime = 0f; UpdateTimerDisplay(0f); }

    // ══════════════════════════════════════════════════════════
    //  MONETE
    // ══════════════════════════════════════════════════════════

    void UpdateCoins(int amount)
    {
        if (coinText != null)
            coinText.text = amount.ToString("D4");
    }

    // ══════════════════════════════════════════════════════════
    //  CHIAVI
    // ══════════════════════════════════════════════════════════

    void UpdateKeys(int current, int total)
    {
        if (keyText != null)
            keyText.text = $"{current}/{total}";
    }

    // ══════════════════════════════════════════════════════════
    //  VITE
    // ══════════════════════════════════════════════════════════

    void UpdateLivesDisplay(int current)
    {
        if (livesText != null)
            livesText.text = current.ToString();
    }

    public void UpdateLives(int current, int max) => UpdateLivesDisplay(current);

    // ══════════════════════════════════════════════════════════
    //  ICE OVERLAY
    // ══════════════════════════════════════════════════════════

    public void ShowIceOverlay()
    {
        if (iceOverlayImage != null)
            iceOverlayImage.gameObject.SetActive(true);
    }

    public void HideIceOverlay()
    {
        if (iceOverlayImage != null)
            iceOverlayImage.gameObject.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════
    //  DIALOGUE
    // ══════════════════════════════════════════════════════════

    public void OnDialogueStart() { if (pauseTimerDuringDialogue) PauseTimer(); }
    public void OnDialogueEnd() { if (pauseTimerDuringDialogue) ResumeTimer(); }
}