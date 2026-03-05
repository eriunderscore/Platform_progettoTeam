using UnityEngine;

public class Moneta : MonoBehaviour
{
    public enum TipoMoneta { Bronze, Silver, Gold }

    [Header("Tipo Moneta")]
    public TipoMoneta tipo = TipoMoneta.Bronze;

    // Valore automatico in base al tipo
    private int Valore()
    {
        switch (tipo)
        {
            case TipoMoneta.Gold: return 10;
            case TipoMoneta.Silver: return 5;
            case TipoMoneta.Bronze: return 1;
            default: return 1;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Cerca il ContenitoreMonete nella scena
            ContenitoreMonete manager = FindObjectOfType<ContenitoreMonete>();
            if (manager != null)
                manager.AggiungiMoneta(Valore());

            Debug.Log($"Raccolta moneta {tipo} (+{Valore()})");
            Destroy(gameObject);
        }
    }
}