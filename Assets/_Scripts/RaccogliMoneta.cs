using UnityEngine;

public class RaccogliMoneta : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Ora cerchiamo il componente con il nuovo nome
            ContatoreMonete contatore = Object.FindAnyObjectByType<ContatoreMonete>();

            if (contatore != null)
            {
                contatore.AggiungiMoneta(1);
            }

            Destroy(gameObject);
        }
    }
}