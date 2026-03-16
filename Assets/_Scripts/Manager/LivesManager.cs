using UnityEngine;
using System.Collections;

public class LivesManager : MonoBehaviour
{
    public static LivesManager Instance { get; private set; }

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR
    // ══════════════════════════════════════════════════════════

    [Header("── Vite ─────────────────────────────────────────")]
    public int maxLives = 3;
    public int startingLives = 3;

    [Header("── Rilevamento caduta ──────────────────────────")]
    public float fallDeathY = -20f;
    public Transform playerTransform;

    [Header("── Conversione cuore in monete ──────────────────")]
    public int heartToCoinsValue = 50;

    [Header("── Respawn delay ───────────────────────────────")]
    [Tooltip("Secondi di attesa prima del respawn dopo una caduta")]
    public float respawnDelay = 1f;

    [Header("── DEBUG (read only) ───────────────────────────")]
    [SerializeField] private int currentLives;
    [SerializeField] private bool isProcessingDeath = false;

    // ══════════════════════════════════════════════════════════
    //  EVENTI
    // ══════════════════════════════════════════════════════════

    public static event System.Action<int> OnLivesChanged;
    public static event System.Action OnGameOver;

    // ══════════════════════════════════════════════════════════
    //  PROPRIETÀ
    // ══════════════════════════════════════════════════════════

    public int Lives => currentLives;
    public int MaxLives => maxLives;
    public bool IsAlive => currentLives > 0;

    // ══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        currentLives = startingLives;
    }

    void Start()
    {
        currentLives = startingLives;
        isProcessingDeath = false;
        NotifyChanged();
    }

    void Update()
    {
        // Il rilevamento caduta è gestito da PlayerController3D.Update()
        // che chiama LivesManager.Instance.LoseLife() quando y < deathYThreshold
    }

    // ══════════════════════════════════════════════════════════
    //  API PUBBLICA
    // ══════════════════════════════════════════════════════════

    public void LoseLife()
    {
        if (isProcessingDeath) return;
        StartCoroutine(LoseLifeRoutine());
    }

    IEnumerator LoseLifeRoutine()
    {
        isProcessingDeath = true;

        currentLives = Mathf.Max(0, currentLives - 1);
        NotifyChanged();

        Debug.Log($"[LivesManager] Vita persa! Rimaste: {currentLives}/{maxLives}");

        if (currentLives <= 0)
        {
            Debug.Log("[LivesManager] Game Over!");
            OnGameOver?.Invoke();
            DeathScreen.Instance?.Show();
            // Non resettare isProcessingDeath — rimane bloccato fino al retry
        }
        else
        {
            // Aspetta prima di respawnare — impedisce loop di morte
            yield return new WaitForSeconds(respawnDelay);

            PlayerController3D player = playerTransform?.GetComponent<PlayerController3D>();
            if (player != null)
                player.Die(); // teletrasporta al respawn point
            else
                Debug.LogWarning("[LivesManager] PlayerController3D non trovato!");

            FallingPlatform.ResetAll();

            // Solo ora riabilita il rilevamento
            isProcessingDeath = false;
        }
    }

    public void AddLife()
    {
        if (currentLives >= maxLives)
        {
            ContenitoreMonete.Instance?.AggiungiMoneta(heartToCoinsValue);
            Debug.Log($"[LivesManager] Vita piena → +{heartToCoinsValue} monete!");
        }
        else
        {
            currentLives++;
            NotifyChanged();
            Debug.Log($"[LivesManager] +1 vita → {currentLives}/{maxLives}");
        }
    }

    public void ResetLives()
    {
        StopAllCoroutines();
        currentLives = startingLives;
        isProcessingDeath = false;
        NotifyChanged();
    }

    // ══════════════════════════════════════════════════════════
    //  INTERNO
    // ══════════════════════════════════════════════════════════

    void NotifyChanged()
    {
        OnLivesChanged?.Invoke(currentLives);
        HUDManager.Instance?.UpdateLives(currentLives, maxLives);
        Debug.Log($"[LivesManager] HUD aggiornato: {currentLives} vite");
    }
}