using UnityEngine;
using System.Collections;

public class TImeStopPowerUp : MonoBehaviour
{
    [Header("Interfaccia UI")]
    public GameObject schermataGhiaccio; // Trascina qui l'immagine del ghiaccio dal Canvas

    [Header("Impostazioni Effetto")]
    public float durataEffetto = 5f;

    private void OnTriggerEnter(Collider other)
    {
        // Verifica se l'oggetto che tocca il PowerUp ha il Tag "Player"
        if (other.CompareTag("Player"))
        {
            // Cerchiamo i componenti necessari nella scena
            PlayerController3D player = other.GetComponent<PlayerController3D>();
            Cronometro crono = Object.FindAnyObjectByType<Cronometro>();

            if (player != null)
            {
                // Avviamo la sequenza del fermatempo
                StartCoroutine(SequenzaFermo(player, crono));

                // Disattiviamo la grafica del PowerUp (il cubetto) per non riprenderlo
                if (GetComponent<MeshRenderer>()) GetComponent<MeshRenderer>().enabled = false;
                if (GetComponent<Collider>()) GetComponent<Collider>().enabled = false;
            }
        }
    }

    IEnumerator SequenzaFermo(PlayerController3D p, Cronometro c)
    {
        // 1. ATTIVAZIONE: Mostra il ghiaccio e ferma il countdown
        if (schermataGhiaccio != null)
            schermataGhiaccio.SetActive(true);

        if (c != null)
            c.FermaTempo();

        // 2. PAUSA: Il gioco continua, tu ti muovi, ma il tempo è bloccato
        yield return new WaitForSeconds(durataEffetto);

        // 3. RIPRISTINO: Il tempo riparte e il ghiaccio sparisce
        if (c != null)
            c.RipartiTempo();

        if (schermataGhiaccio != null)
            schermataGhiaccio.SetActive(false);

        // Distrugge l'oggetto PowerUp definitivamente
        Destroy(gameObject);
    }
}