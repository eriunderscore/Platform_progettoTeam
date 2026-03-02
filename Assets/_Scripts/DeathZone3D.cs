using UnityEngine;

public class DeathZone3D : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Controlla se l'oggetto che entra ha il tag "Player"
        if (other.CompareTag("Player"))
        {
            PlayerController3D player = other.GetComponent<PlayerController3D>();

            if (player != null)
            {
                // Chiama la morte: gestirà vite, UI e respawn internamente
                player.Die();
            }
        }
    }
}