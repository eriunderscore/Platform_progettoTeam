using UnityEngine;

public class VoloOggetto : MonoBehaviour
{
    public float ampiezza = 0.5f; // Quanto va in alto/basso
    public float velocita = 2f;   // Quanto velocemente si muove
    public float velocitaRotazione = 50f; // Velocità di rotazione

    private Vector3 posizioneIniziale;

    void Start()
    {
        posizioneIniziale = transform.position;
    }

    void Update()
    {
        // Movimento su e giù (sinusoide)
        float nuovaY = posizioneIniziale.y + Mathf.Sin(Time.time * velocita) * ampiezza;
        transform.position = new Vector3(transform.position.x, nuovaY, transform.position.z);

        // Rotazione continua
        transform.Rotate(Vector3.up, velocitaRotazione * Time.deltaTime);
    }
}