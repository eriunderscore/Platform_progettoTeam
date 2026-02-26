using UnityEngine;
using UnityEngine.UI;

public class ContenitoreChiavi : MonoBehaviour
{
    public GameObject pannelloChiavi;
    public Image[] iconeChiavi;
    private int chiaviPrese = 0;
    private float timerVisibilita = 0f;
    private bool inventarioAperto = false;

    void Start()
    {
        if (pannelloChiavi != null) pannelloChiavi.SetActive(false);

        // Reset iniziale di tutte le icone
        foreach (Image img in iconeChiavi)
        {
            if (img != null) img.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            inventarioAperto = !inventarioAperto;
            AggiornaStato();
        }

        if (timerVisibilita > 0 && !inventarioAperto)
        {
            timerVisibilita -= Time.deltaTime;
            if (timerVisibilita <= 0) AggiornaStato();
        }
    }

    public void AggiungiChiave()
    {
        // Controllo fondamentale: se abbiamo ancora icone disponibili
        if (chiaviPrese < iconeChiavi.Length)
        {
            // Accendiamo l'icona corrispondente al numero di chiavi prese
            if (iconeChiavi[chiaviPrese] != null)
            {
                iconeChiavi[chiaviPrese].color = Color.white;
            }

            chiaviPrese++; // Aumentiamo il conteggio DOPO aver acceso l'icona
            timerVisibilita = 3f;
            AggiornaStato();
        }
    }

    void AggiornaStato()
    {
        bool deveEssereVisibile = inventarioAperto || timerVisibilita > 0;
        if (pannelloChiavi != null) pannelloChiavi.SetActive(deveEssereVisibile);
    }
}