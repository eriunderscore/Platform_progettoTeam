using UnityEngine;

public class Chiave : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ContenitoreChiavi manager = ContenitoreChiavi.Instance;
            if (manager != null)
                manager.AggiungiChiave();

            Debug.Log("Raccolta chiave (+1)");
            Destroy(gameObject);
        }
    }
}
