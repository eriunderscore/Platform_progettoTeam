using UnityEngine;

public class RotazioneOggetto : MonoBehaviour
{
    public float velocitaRotazione = 100f;

    void Update()
    {
        // Fa ruotare l'oggetto sull'asse Y (verticale)
        transform.Rotate(Vector3.up * velocitaRotazione * Time.deltaTime);
    }
}