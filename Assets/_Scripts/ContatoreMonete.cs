using UnityEngine;
using TMPro; // Serve per il testo TextMeshPro

public class ContatoreMonete : MonoBehaviour
{
    public TextMeshProUGUI testoMonete;
    private int totaleMonete = 0;

    // Funzione chiamata dalle monete quando vengono raccolte
    public void AggiungiMoneta(int quantita)
    {
        totaleMonete += quantita;
        AggiornaGrafica();
    }

    void AggiornaGrafica()
    {
        if (testoMonete != null)
        {
            testoMonete.text = totaleMonete.ToString();
        }
    }
}