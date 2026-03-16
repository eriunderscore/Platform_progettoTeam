// ============================================================
//  ContenitoreMonete.cs
//  Gestisce il conteggio delle monete del giocatore.
//
//  SETUP:
//  1. Crea un GameObject vuoto nella scena chiamato "ContenitoreMonete"
//  2. Aggiungi questo script
//  3. In NPCInteraction sostituisci le righe:
//       coinsGetter:  () => testCoins,
//       coinsSpender: (cost) => testCoins -= cost,
//     con:
//       coinsGetter:  () => ContenitoreMonete.Instance.Monete,
//       coinsSpender: (cost) => ContenitoreMonete.Instance.SpendMonete(cost),
//
//  SETUP TAG:
//  - Assicurati che il tuo Player abbia il Tag "Player"
//    (seleziona il Player → Inspector → Tag → Player)
// ============================================================

using UnityEngine;

public class ContenitoreMonete : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────
    public static ContenitoreMonete Instance { get; private set; }

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR
    // ══════════════════════════════════════════════════════════

    [Header("── Monete ───────────────────────────────────────")]
    [Tooltip("Monete iniziali del giocatore")]
    public int moneteIniziali = 0;

    [Header("── DEBUG (read-only in Play Mode) ───────────────")]
    [Tooltip("Monete correnti — visibile in tempo reale nell'Inspector")]
    [SerializeField] private int moneteCorrente = 0;

    // ══════════════════════════════════════════════════════════
    //  PROPRIETÀ PUBBLICA
    // ══════════════════════════════════════════════════════════

    /// <summary>Monete attuali del giocatore (0..∞).</summary>
    public int Monete => moneteCorrente;

    // Evento: altri script (es. HUD) possono iscriversi per aggiornarsi
    // quando le monete cambiano.
    // Uso: ContenitoreMonete.OnMoneteChanged += mioMetodo;
    public static event System.Action<int> OnMoneteChanged;

    // ══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════

    void Awake()
    {
        // Singleton: uno solo nella scena, persiste tra le scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        moneteCorrente = moneteIniziali;
    }

    // ══════════════════════════════════════════════════════════
    //  API PUBBLICA
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Aggiunge monete al conteggio (chiamato da Moneta.cs).
    /// </summary>
    public void AggiungiMoneta(int valore)
    {
        if (valore <= 0) return;
        moneteCorrente += valore;
        NotifyChanged();
        Debug.Log($"[ContenitoreMonete] +{valore} monete → totale: {moneteCorrente}");
    }

    /// <summary>
    /// Spende monete (chiamato dallo ShopUI).
    /// Ritorna true se la spesa è andata a buon fine, false se non ci sono fondi.
    /// </summary>
    public bool SpendMonete(int costo)
    {
        if (costo <= 0) return true;
        if (moneteCorrente < costo)
        {
            Debug.Log($"[ContenitoreMonete] Fondi insufficienti! Hai {moneteCorrente}, servono {costo}.");
            return false;
        }
        moneteCorrente -= costo;
        NotifyChanged();
        Debug.Log($"[ContenitoreMonete] -{costo} monete → totale: {moneteCorrente}");
        return true;
    }

    /// <summary>
    /// Controlla se il giocatore può permettersi un costo.
    /// </summary>
    public bool PuoPermettere(int costo) => moneteCorrente >= costo;

    /// <summary>
    /// Resetta le monete (es. nuova partita).
    /// </summary>
    public void Reset()
    {
        moneteCorrente = moneteIniziali;
        NotifyChanged();
    }

    // ══════════════════════════════════════════════════════════
    //  INTERNO
    // ══════════════════════════════════════════════════════════

    void NotifyChanged() => OnMoneteChanged?.Invoke(moneteCorrente);
}
