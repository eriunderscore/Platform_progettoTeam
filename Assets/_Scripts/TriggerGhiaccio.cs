using UnityEngine;

public class TriggerGhiaccio : MonoBehaviour
{
    [Header("Riferimenti")]
    // Qui trascinerai l'oggetto "Caduta ghiaccio"
    public Animator animatorGhiaccio;

    private AudioSource musicaZZZ;
    private bool giaAttivato = false;

    void Start()
    {
        // Prende l'AudioSource che metterai sul Cubo
        musicaZZZ = GetComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        // Controlla se il giocatore si chiama "Player" (Tag)
        if (other.CompareTag("Player") && !giaAttivato)
        {
            giaAttivato = true;

            // Avvia l'animazione fatta in Blender
            if (animatorGhiaccio != null)
            {
                animatorGhiaccio.Play("Ghiaccio_Cade");
            }

            // Avvia la musica di Zenless Zone Zero
            if (musicaZZZ != null)
            {
                musicaZZZ.Play();
            }

            Debug.Log("Ghiaccio attivato con successo!");
        }
    }
}