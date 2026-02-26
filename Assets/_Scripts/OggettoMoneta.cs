using UnityEngine;

public class OggettoMoneta : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Se il giocatore tocca la moneta
        if (other.CompareTag("Player"))
        {
            // Cerca il manager nel Canvas
            ManagerMonete manager = Object.FindAnyObjectByType<ManagerMonete>();

            if (manager != null)
            {
                manager.AggiungiMoneta(1);
            }

            // Distrugge la moneta raccolta
            Destroy(gameObject);
        }
    }
}