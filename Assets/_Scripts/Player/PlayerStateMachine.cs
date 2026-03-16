// ============================================================
//  PlayerStateMachine.cs
//  Definisce gli stati del giocatore e li rende visibili
//  nell'Inspector di Unity durante il Play Mode.
//
//  COME FUNZIONA:
//  Ogni stato è un valore dell'enum PlayerState.
//  Lo script PlayerController3D chiama ChangeState() per
//  passare da uno stato all'altro.
//  Nell'Inspector vedrai una sezione "── DEBUG STATE ──"
//  con un checkbox per ogni stato: quello selezionato è
//  lo stato attivo in tempo reale.
// ============================================================

using UnityEngine;

// ── Enum degli stati ──────────────────────────────────────────
public enum PlayerState
{
    Idle,
    Run,
    Jump,
    Fall,
    Dash,
    Climbing
}

public class PlayerStateMachine : MonoBehaviour
{
    // ── Stato corrente (usato dal controller) ─────────────────
    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;

    // ── DEBUG VISUALE NELL'EDITOR ─────────────────────────────
    // Questi bool vengono aggiornati ogni frame e mostrano
    // visivamente quale stato è attivo nell'Inspector.
    // Non modificarli a mano: sono di sola lettura durante il play.
    [Header("── DEBUG STATE (read-only in Play Mode) ──")]
    [Tooltip("Il personaggio è fermo")]
    public bool stateIdle;

    [Tooltip("Il personaggio si sta muovendo a terra")]
    public bool stateRun;

    [Tooltip("Il personaggio sta saltando (Y positiva)")]
    public bool stateJump;

    [Tooltip("Il personaggio sta cadendo (Y negativa, non a terra)")]
    public bool stateFall;

    [Tooltip("Il personaggio sta eseguendo un dash")]
    public bool stateDash;

    [Tooltip("Il personaggio è agganciato a una parete")]
    public bool stateClimbing;

    // ── API pubblica ──────────────────────────────────────────

    /// <summary>
    /// Cambia lo stato corrente. Se lo stato è già quello richiesto,
    /// non fa nulla (evita rientri inutili).
    /// </summary>
    public void ChangeState(PlayerState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        RefreshDebugFlags();
    }

    // ── Aggiornamento dei flag debug ──────────────────────────
    void RefreshDebugFlags()
    {
        stateIdle     = CurrentState == PlayerState.Idle;
        stateRun      = CurrentState == PlayerState.Run;
        stateJump     = CurrentState == PlayerState.Jump;
        stateFall     = CurrentState == PlayerState.Fall;
        stateDash     = CurrentState == PlayerState.Dash;
        stateClimbing = CurrentState == PlayerState.Climbing;
    }

    // ── Aggiorna ogni frame per sicurezza ─────────────────────
    // (nel caso il CurrentState venga cambiato senza passare da ChangeState)
    void LateUpdate() => RefreshDebugFlags();
}
