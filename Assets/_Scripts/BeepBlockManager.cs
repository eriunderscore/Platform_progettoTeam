using UnityEngine;

/// <summary>
/// Global manager that controls the A/B beep block cycle.
/// Place ONE of these anywhere in the scene.
/// All BeepBlock scripts automatically register with it.
/// </summary>
public class BeepBlockManager : MonoBehaviour
{
    public static BeepBlockManager Instance { get; private set; }

    [Header("Timing")]
    [Tooltip("How long each type stays active (same for A and B)")]
    public float blockDuration = 3f;

    [Header("Warning Flicker")]
    [Tooltip("How many seconds before swap the warning starts")]
    public float warningDuration = 1f;
    [Tooltip("Flicker frequency at the START of the warning (slow)")]
    public float flickerFreqStart = 3f;
    [Tooltip("Flicker frequency at the END of the warning (fast)")]
    public float flickerFreqEnd = 18f;
    [Tooltip("Minimum alpha during flicker (0 = fully transparent dips)")]
    public float flickerMinAlpha = 0.15f;

    // ── State ─────────────────────────────────────────────────
    // true  = A blocks active, B blocks inactive
    // false = B blocks active, A blocks inactive
    public bool AIsActive { get; private set; } = true;
    public float CycleProgress { get; private set; }  // 0 → 1 within current duration
    public bool InWarning { get; private set; }

    // 0 → 1 within the warning window (used by blocks to scale flicker intensity)
    public float WarningProgress { get; private set; }

    private float timer = 0f;

    // ──────────────────────────────────────────────────────────

    public void ResetCycle()
    {
        timer = 0f;
        AIsActive = true;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        timer += Time.deltaTime;

        CycleProgress = timer / blockDuration;

        float timeLeft = blockDuration - timer;

        InWarning = timeLeft <= warningDuration;

        if (InWarning)
            WarningProgress = 1f - (timeLeft / warningDuration);  // 0 at start, 1 at swap
        else
            WarningProgress = 0f;

        // Swap!
        if (timer >= blockDuration)
        {
            timer = 0f;
            AIsActive = !AIsActive;
        }
    }
}