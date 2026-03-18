// ============================================================
//  KeyDoor.cs
//
//  Attach this to any GameObject (door, barrier, block, etc.)
//  When the player has collected all required keys,
//  the object disappears (mesh + collider disabled).
//
//  SETUP:
//  1. Aggiungi questo script all'oggetto che deve sparire
//  2. Imposta "keysRequired" nell'Inspector
//     (default: usa il totale di ContenitoreChiavi)
// ============================================================

using UnityEngine;

public class KeyDoor : MonoBehaviour
{
    [Header("── Chiavi Richieste ─────────────────────────────")]
    [Tooltip("Numero di chiavi necessarie per far sparire questo oggetto.\n-1 = usa il totale di ContenitoreChiavi automaticamente")]
    public int keysRequired = -1;

    [Header("── Opzioni ──────────────────────────────────────")]
    [Tooltip("Se true, l'oggetto sparisce gradualmente (scala a zero)")]
    public bool animateDisappear = true;
    [Tooltip("Velocità dell'animazione di scomparsa")]
    public float disappearSpeed = 3f;

    // ── Private ───────────────────────────────────────────────
    private bool       isDisappearing = false;
    private Renderer[] renderers;
    private Collider[] colliders;

    // ══════════════════════════════════════════════════════════

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();

        // Se keysRequired = -1, usa il totale di ContenitoreChiavi
        if (keysRequired < 0 && ContenitoreChiavi.Instance != null)
            keysRequired = ContenitoreChiavi.Instance.ChiaviTotali;

        // Controlla subito (nel caso avesse già le chiavi)
        CheckKeys(ContenitoreChiavi.Instance?.Chiavi ?? 0,
                  ContenitoreChiavi.Instance?.ChiaviTotali ?? keysRequired);
    }

    void OnEnable()
    {
        ContenitoreChiavi.OnChiaviChanged += CheckKeys;
    }

    void OnDisable()
    {
        ContenitoreChiavi.OnChiaviChanged -= CheckKeys;
    }

    void Update()
    {
        if (!isDisappearing) return;

        // Animazione scala a zero
        transform.localScale = Vector3.MoveTowards(
            transform.localScale, Vector3.zero, disappearSpeed * Time.deltaTime);

        if (transform.localScale.sqrMagnitude < 0.001f)
        {
            // Disabilita collider e renderer definitivamente
            foreach (var c in colliders) c.enabled = false;
            foreach (var r in renderers) r.enabled  = false;
            transform.localScale = Vector3.zero;
            isDisappearing = false;
            enabled = false; // disabilita lo script
        }
    }

    // ──────────────────────────────────────────────────────────

    void CheckKeys(int current, int total)
    {
        if (isDisappearing) return;
        if (current < keysRequired) return;

        Debug.Log($"[KeyDoor] {keysRequired} chiavi raggiunte! Scomparsa attivata.");

        // Disabilita collider subito
        foreach (var c in colliders) c.enabled = false;

        if (animateDisappear)
        {
            isDisappearing = true;
        }
        else
        {
            // Sparisce istantaneamente
            foreach (var r in renderers) r.enabled = false;
            gameObject.SetActive(false);
        }
    }
}
