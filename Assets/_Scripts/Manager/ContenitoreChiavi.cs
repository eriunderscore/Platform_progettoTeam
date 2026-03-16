// ============================================================
//  ContenitoreChiavi.cs
//  Gestisce il conteggio delle chiavi del giocatore.
//
//  SETUP:
//  1. Crea un GameObject vuoto nella scena chiamato "ContenitoreChiavi"
//  2. Aggiungi questo script
//  3. Imposta "Chiavi Totali" nell'Inspector (es. 3)
//
//  SETUP TAG:
//  - Assicurati che il tuo Player abbia il Tag "Player"
// ============================================================

using UnityEngine;

public class ContenitoreChiavi : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────
    public static ContenitoreChiavi Instance { get; private set; }

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR
    // ══════════════════════════════════════════════════════════

    [Header("── Chiavi ───────────────────────────────────────")]
    [Tooltip("Numero totale di chiavi da raccogliere (denominatore es. 3 3)")]
    public int chiaviTotali = 3;

    [Header("── DEBUG (read-only in Play Mode) ───────────────")]
    [Tooltip("Chiavi raccolte — visibile in tempo reale nell'Inspector")]
    [SerializeField] private int chiaveCorrente = 0;

    // ══════════════════════════════════════════════════════════
    //  PROPRIETÀ PUBBLICA
    // ══════════════════════════════════════════════════════════

    /// <summary>Chiavi raccolte finora.</summary>
    public int Chiavi       => chiaveCorrente;

    /// <summary>Chiavi totali richieste.</summary>
    public int ChiaviTotali => chiaviTotali;

    // Evento: altri script (es. HUD) possono iscriversi
    // Uso: ContenitoreChiavi.OnChiaviChanged += mioMetodo;
    public static event System.Action<int, int> OnChiaviChanged; // (correnti, totali)

    // ══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ══════════════════════════════════════════════════════════
    //  API PUBBLICA
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Aggiunge una chiave (chiamato da Chiave.cs).
    /// </summary>
    public void AggiungiChiave()
    {
        if (chiaveCorrente >= chiaviTotali) return;
        chiaveCorrente++;
        NotifyChanged();
        Debug.Log($"[ContenitoreChiavi] +1 chiave → {chiaveCorrente}/{chiaviTotali}");
    }

    /// <summary>
    /// Usa una chiave (es. per aprire una porta).
    /// Ritorna true se aveva almeno una chiave.
    /// </summary>
    public bool UsaChiave()
    {
        if (chiaveCorrente <= 0)
        {
            Debug.Log("[ContenitoreChiavi] Nessuna chiave disponibile!");
            return false;
        }
        chiaveCorrente--;
        NotifyChanged();
        Debug.Log($"[ContenitoreChiavi] -{1} chiave → {chiaveCorrente}/{chiaviTotali}");
        return true;
    }

    /// <summary>Controlla se il giocatore ha almeno una chiave.</summary>
    public bool HaChiavi() => chiaveCorrente > 0;

    /// <summary>Controlla se tutte le chiavi sono state raccolte.</summary>
    public bool HaTutteLeChiavi() => chiaveCorrente >= chiaviTotali;

    /// <summary>Resetta le chiavi (es. nuova partita).</summary>
    public void Reset()
    {
        chiaveCorrente = 0;
        NotifyChanged();
    }

    // ══════════════════════════════════════════════════════════
    //  INTERNO
    // ══════════════════════════════════════════════════════════

    void NotifyChanged()
    {
        // Aggiorna HUD direttamente con i valori corretti
        if (HUDManager.Instance != null && HUDManager.Instance.keyText != null)
            HUDManager.Instance.keyText.text = $"{chiaveCorrente} {chiaviTotali}";

        OnChiaviChanged?.Invoke(chiaveCorrente, chiaviTotali);
    }
}
