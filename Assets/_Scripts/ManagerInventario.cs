using UnityEngine;
using UnityEngine.UI;

public class ManagerInventario : MonoBehaviour
{
    public GameObject pannelloInventario; // Il pannello che contiene tutto
    public Image[] iconeChiavi;           // Trascina qui le 3 immagini delle chiavi
    private int chiaviPrese = 0;

    private float timerVisibilita = 0f;
    private bool inventarioApertoManualmente = false;

    void Start()
    {
        pannelloInventario.SetActive(false);
    }

    void Update()
    {
        // Tasto R per aprire/chiudere l'inventario
        if (Input.GetKeyDown(KeyCode.R))
        {
            inventarioApertoManualmente = !inventarioApertoManualmente;
            AggiornaStatoUI();
        }

        // Gestione timer per la scomparsa automatica
        if (timerVisibilita > 0 && !inventarioApertoManualmente)
        {
            timerVisibilita -= Time.deltaTime;
            if (timerVisibilita <= 0)
            {
                pannelloInventario.SetActive(false);
            }
        }
    }

    public void AggiungiChiave()
    {
        if (chiaviPrese < iconeChiavi.Length)
        {
            // Colora l'icona della chiave presa (la fa diventare bianca/colorata)
            iconeChiavi[chiaviPrese].color = Color.white;
            chiaviPrese++;

            // Mostra temporaneamente l'inventario
            MostraTemporaneamente();
        }
    }

    void MostraTemporaneamente()
    {
        timerVisibilita = 3f; // Resta visibile per 3 secondi
        pannelloInventario.SetActive(true);
    }

    void AggiornaStatoUI()
    {
        // Se l'inventario è aperto con R, resta acceso. Altrimenti segue il timer.
        pannelloInventario.SetActive(inventarioApertoManualmente || timerVisibilita > 0);
    }
}