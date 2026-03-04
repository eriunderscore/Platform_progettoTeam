using UnityEngine;
using TMPro;

public class ContenitoreMonete : MonoBehaviour
{
    [Header("CONFIGURAZIONE UI")]
    public GameObject pannelloMonete;
    public TextMeshProUGUI testoMonete;

    private int moneteTotali = 0;
    private float timerVisibilita = 0f;
    private bool inventarioAperto = false;

    void Start()
    {
        if (pannelloMonete != null) pannelloMonete.SetActive(false);
        AggiornaUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            inventarioAperto = !inventarioAperto;
            Debug.Log("Inventario Monete: " + (inventarioAperto ? "APERTO" : "CHIUSO"));
            AggiornaUI();
        }

        if (timerVisibilita > 0)
        {
            timerVisibilita -= Time.deltaTime;
            if (timerVisibilita <= 0 && !inventarioAperto)
            {
                AggiornaUI();
            }
        }
    }

    public void AggiungiMoneta(int quantita)
    {
        moneteTotali += quantita;
        timerVisibilita = 3f;
        AggiornaUI();
        Debug.Log("Moneta presa! Totale: " + moneteTotali);
    }

    void AggiornaUI()
    {
        if (testoMonete != null)
            testoMonete.text = moneteTotali.ToString("D3");

        if (pannelloMonete != null)
        {
            bool deveEssereVisibile = inventarioAperto || timerVisibilita > 0;
            pannelloMonete.SetActive(deveEssereVisibile);
        }
    }
}