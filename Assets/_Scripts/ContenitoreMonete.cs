using UnityEngine;
using TMPro;

public class ContenitoreMonete : MonoBehaviour
{
    public TextMeshProUGUI testoMonete;
    public GameObject elementoGraficoMonete;
    private int totaleMonete = 0;
    private float timerVisibilita = 0f;
    private bool inventarioAperto = false; // Aggiunto per gestire la R

    void Start()
    {
        if (elementoGraficoMonete != null) elementoGraficoMonete.SetActive(false);
    }

    void Update()
    {
        // AGGIUNTO: Anche le monete ora ascoltano il tasto R
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

    public void AggiungiMoneta(int valore)
    {
        totaleMonete += valore;
        if (testoMonete != null) testoMonete.text = totaleMonete.ToString();
        timerVisibilita = 3f;
        AggiornaStato();
    }

    void AggiornaStato()
    {
        bool deveEssereVisibile = inventarioAperto || timerVisibilita > 0;
        if (elementoGraficoMonete != null) elementoGraficoMonete.SetActive(deveEssereVisibile);
    }
}