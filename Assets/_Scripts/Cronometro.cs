using UnityEngine;
using TMPro;
using System;

public class Cronometro : MonoBehaviour
{
    private TextMeshProUGUI testoTempo;
    private float tempoTrascorso = 0f; // Parte da zero
    private bool staCorrendo = true;

    void Awake()
    {
        testoTempo = GetComponent<TextMeshProUGUI>();

        if (testoTempo == null)
        {
            Debug.LogError("Non ho trovato TextMeshPro su questo oggetto!");
        }
    }

    void Start()
    {
        AggiornaGraficaTesto();
    }

    void Update()
    {
        if (staCorrendo)
        {
            // Conta in avanti
            tempoTrascorso += Time.deltaTime;
            AggiornaGraficaTesto();
        }
    }

    void AggiornaGraficaTesto()
    {
        if (testoTempo != null)
        {
            TimeSpan tempo = TimeSpan.FromSeconds(tempoTrascorso);

            // Formato 00:00 (Minuti : Secondi)
            testoTempo.text = string.Format("{0:00}:{1:00}",
                tempo.Minutes + (tempo.Hours * 60), // Include le ore nei minuti se il gioco dura molto
                tempo.Seconds);
        }
    }

    public void FermaTempo() => staCorrendo = false;
    public void RipartiTempo() => staCorrendo = true;

    // Funzione utile per resettare il tempo se il player muore
    public void ResettaTempo() => tempoTrascorso = 0f;
}