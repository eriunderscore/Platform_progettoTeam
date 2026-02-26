using UnityEngine;
using TMPro; // Usiamo TextMeshPro per un testo più nitido

public class ManagerMonete : MonoBehaviour
{
    public TextMeshProUGUI testoMonete; // Trascina qui il testo della UI
    private int moneteRaccolte = 0;

    // Funzione da chiamare quando prendi una moneta
    public void AggiungiMoneta(int valore)
    {
        moneteRaccolte += valore;
        AggiornaGrafica();
    }

    void AggiornaGrafica()
    {
        testoMonete.text = moneteRaccolte.ToString();
    }
}