// ============================================================
//  TimeStopPowerUp.cs
//
//  SETUP:
//  1. Metti questo script sul GameObject del PowerUp
//  2. Aggiungi un Collider → Is Trigger ✅
//  3. Nel Canvas, crea un GameObject "IceOverlay":
//     - Aggiungi Image → assegna la tua texture del ghiaccio
//     - Anchor: stretch-stretch (copre tutto lo schermo)
//     - Lascialo ATTIVO nel Canvas (lo script lo nasconde a Start)
//  4. Trascina "IceOverlay" nel campo "Ice Overlay Image" di HUDManager
//  5. Il Player deve avere il Tag "Player"
// ============================================================

using UnityEngine;
using System.Collections;

public class TimeStopPowerUp : MonoBehaviour
{
    [Header("── Impostazioni Effetto ─────────────────────────")]
    [Tooltip("Quanti secondi rimane fermato il timer")]
    public float durataEffetto = 5f;

    // ══════════════════════════════════════════════════════════

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController3D player = other.GetComponent<PlayerController3D>();
        if (player == null) return;

        StartCoroutine(SequenzaFermo());

        // Nascondi visivamente il PowerUp subito
        MeshRenderer mr = GetComponent<MeshRenderer>();
        Collider col    = GetComponent<Collider>();
        if (mr)  mr.enabled  = false;
        if (col) col.enabled = false;
    }

    IEnumerator SequenzaFermo()
    {
        // 1. ATTIVAZIONE ──────────────────────────────────────
        HUDManager.Instance?.ShowIceOverlay();
        HUDManager.Instance?.PauseTimer();
        Debug.Log("[TimeStopPowerUp] Timer fermato!");

        // 2. ATTESA ───────────────────────────────────────────
        yield return new WaitForSeconds(durataEffetto);

        // 3. RIPRISTINO ───────────────────────────────────────
        HUDManager.Instance?.ResumeTimer();
        HUDManager.Instance?.HideIceOverlay();
        Debug.Log("[TimeStopPowerUp] Timer ripreso!");

        Destroy(gameObject);
    }
}
