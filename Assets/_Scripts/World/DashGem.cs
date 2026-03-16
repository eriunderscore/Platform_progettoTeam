using System.Collections;
using UnityEngine;

public class DashGem : MonoBehaviour
{
    [Header("─── References ───")]
    public string playerTag = "Player";

    [Header("─── Respawn ───")]
    [Tooltip("Secondi prima che la gem riappaia dopo essere raccolta")]
    public float respawnTime = 10f;

    private bool _collected = false;
    private MeshRenderer _mesh;
    private Collider _col;

    void Awake()
    {
        _mesh = GetComponent<MeshRenderer>();
        _col  = GetComponent<Collider>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        if (!other.CompareTag(playerTag)) return;

        _collected = true;

        PlayerController3D player = other.GetComponent<PlayerController3D>();
        if (player != null)
        {
            player.RefillDash();
            player.RefillStamina();
        }
        else
            Debug.LogWarning("DashGem: PlayerController3D non trovato sul Player!");

        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        // Nasconde la gem
        if (_mesh) _mesh.enabled = false;
        if (_col)  _col.enabled  = false;

        yield return new WaitForSeconds(respawnTime);

        // Riappare
        if (_mesh) _mesh.enabled = true;
        if (_col)  _col.enabled  = true;
        _collected = false;
    }
}
