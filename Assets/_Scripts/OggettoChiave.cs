using UnityEngine;

public class OggettoChiave : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Qui puoi aggiungere un messaggio tipo "Hai preso la chiave!"
            Debug.Log("Chiave raccolta!");

            // Se hai un Manager per le chiavi, chiamalo qui
            // Altrimenti, per ora, falla solo sparire
            Destroy(gameObject);
        }
    }
}