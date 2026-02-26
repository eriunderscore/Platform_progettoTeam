using UnityEngine;

public class DeathZone3D : MonoBehaviour
{
    [Header("Respawn Settings")]
    public Transform puntoDiPartenza; // Trascina qui l'oggetto del punto iniziale (es. lo SpawnPoint)

    private void OnTriggerEnter(Collider other)
    {
        // Controlla se l'oggetto che entra ha il tag "Player"
        if (other.CompareTag("Player"))
        {
            // Cerchiamo lo script del giocatore
            PlayerController3D player = other.GetComponent<PlayerController3D>();

            if (player != null)
            {
                // 1. Chiamiamo la funzione morte (toglie vita/aggiorna UI)
                player.Die();

                // 2. Teletrasporto al punto di partenza
                if (puntoDiPartenza != null)
                {
                    // Se usi un CharacterController, dobbiamo spegnerlo un attimo per "teletrasportare"
                    CharacterController cc = other.GetComponent<CharacterController>();

                    if (cc != null) cc.enabled = false;

                    other.transform.position = puntoDiPartenza.position;

                    if (cc != null) cc.enabled = true;

                    Debug.Log("Il Rapanello è caduto! Respawn effettuato.");
                }
                else
                {
                    Debug.LogError("ERRORE: Manca il 'Punto Di Partenza' nell'Inspector della DeathZone!");
                }
            }
        }
    }
}