using UnityEngine;

public class RaccogliMoneta : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // ORA CERCA IL NOME NUOVO: ContenitoreMonete
            ContenitoreMonete contatore = Object.FindFirstObjectByType<ContenitoreMonete>(FindObjectsInactive.Include);

            if (contatore != null)
            {
                contatore.AggiungiMoneta(1);
                Debug.Log("Moneta inviata al Contenitore!");
            }
            else
            {
                // Questo errore apparirà se lo script sul Canvas non è stato ancora applicato
                Debug.LogError("ERRORE: Non ho trovato nessuno script chiamato ContenitoreMonete nella scena!");
            }

            Destroy(gameObject);
        }
    }
}