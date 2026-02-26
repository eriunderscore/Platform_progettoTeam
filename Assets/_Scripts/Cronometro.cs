using UnityEngine;
using TMPro; // Fondamentale per TextMeshPro
using System;

public class Cronometro : MonoBehaviour
{
    private TextMeshProUGUI testoTempo; // Versione Pro del componente
    public float tempoRimanente = 120f;
    private bool staCorrendo = true;

    void Awake()
    {
        // Cerca il componente TextMeshPro sullo stesso oggetto
        testoTempo = GetComponent<TextMeshProUGUI>();

        if (testoTempo == null)
        {
            Debug.LogError("Non ho trovato TextMeshPro su questo oggetto! Controlla di averlo messo sull'oggetto giusto.");
        }
    }

    void Start()
    {
        AggiornaGraficaTesto();
    }

    void Update()
    {
        if (staCorrendo && tempoRimanente > 0)
        {
            tempoRimanente -= Time.deltaTime;
            if (tempoRimanente < 0) tempoRimanente = 0;

            AggiornaGraficaTesto();

            if (tempoRimanente <= 0)
            {
                Debug.Log("TEMPO SCADUTO!");
                // Qui puoi chiamare la morte del player se vuoi
            }
        }
    }

    void AggiornaGraficaTesto()
    {
        if (testoTempo != null)
        {
            TimeSpan tempo = TimeSpan.FromSeconds(tempoRimanente);
            // Formato 00:00:00
            testoTempo.text = string.Format("{0:00}:{1:00}:{2:00}",
                tempo.Minutes, tempo.Seconds, tempo.Milliseconds / 10);
        }
    }

    public void FermaTempo() => staCorrendo = false;
    public void RipartiTempo() => staCorrendo = true;
}