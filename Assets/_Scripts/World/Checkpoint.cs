using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("── Impostazioni ─────────────────────────────────")]
    [Tooltip("Offset Y del punto di respawn rispetto al checkpoint")]
    public float respawnYOffset = 1f;

    [Header("── Visuale ──────────────────────────────────────")]
    [Tooltip("Colore quando il checkpoint è inattivo")]
    public Color inactiveColor = Color.gray;
    [Tooltip("Colore quando il checkpoint è attivo")]
    public Color activeColor   = new Color(0f, 1f, 0.4f, 1f);

    // ── Static — traccia il checkpoint attivo globalmente ─────
    public static Checkpoint ActiveCheckpoint { get; private set; }

    private Renderer[] renderers;
    private bool       isActive = false;

    // ══════════════════════════════════════════════════════════

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        SetVisual(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (isActive) return; // già attivo, ignora

        Activate(other.GetComponent<PlayerController3D>());
    }

    // ──────────────────────────────────────────────────────────

    void Activate(PlayerController3D player)
    {
        // Disattiva il checkpoint precedente
        if (ActiveCheckpoint != null && ActiveCheckpoint != this)
            ActiveCheckpoint.Deactivate();

        // Attiva questo
        isActive        = true;
        ActiveCheckpoint = this;

        // Imposta il respawn point nel PlayerController3D
        Vector3 respawnPos = transform.position + Vector3.up * respawnYOffset;
        player?.SetRespawnPoint(respawnPos);

        SetVisual(true);
        Debug.Log($"[Checkpoint] Attivato: {gameObject.name} — Respawn: {respawnPos}");
    }

    public void Deactivate()
    {
        isActive = false;
        SetVisual(false);
    }

    void SetVisual(bool active)
    {
        Color col = active ? activeColor : inactiveColor;
        foreach (var r in renderers)
        {
            if (r.material != null)
                r.material.color = col;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = isActive ? activeColor : inactiveColor;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * respawnYOffset, 0.3f);
    }
#endif
}
