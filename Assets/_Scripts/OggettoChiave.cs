using UnityEngine;

public class OggettoChiave : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 1. IL RAPANELLO DEVE AVERE IL TAG "Player" (Scritto così!)
        if (other.CompareTag("Player"))
        {
            // 2. Cerca il nuovo script delle chiavi
            ContenitoreChiavi scriptChiavi = Object.FindAnyObjectByType<ContenitoreChiavi>();

            if (scriptChiavi != null)
            {
                scriptChiavi.AggiungiChiave();
                Destroy(gameObject); // ORA DEVE SPARIRE
            }
            else
            {
                Debug.LogError("Non trovo ContenitoreChiavi sul Canvas!");
            }
        }
    }
}