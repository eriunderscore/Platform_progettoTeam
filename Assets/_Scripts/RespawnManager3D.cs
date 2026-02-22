using UnityEngine;

/// <summary>
/// Kept for compatibility with PlayerController3D.Die().
/// All logic now lives in GameManager.
/// </summary>
public class RespawnManager3D : MonoBehaviour
{
    public static RespawnManager3D Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void KillPlayer(GameObject player, Vector3 respawnPosition)
    {
        // GameManager handles lives, respawn position, and falling platform reset
        if (GameManager.Instance != null)
            GameManager.Instance.PlayerDied();
    }
}