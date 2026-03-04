using UnityEngine;
using TMPro;

public class ManagerMonete : MonoBehaviour
{
    public TextMeshProUGUI testoMonete;
    private int moneteRaccolte = 0;

    public void AggiungiMoneta(int valore)
    {
        moneteRaccolte += valore;
        AggiornaGrafica();
    }

    void AggiornaGrafica()
    {
        testoMonete.text = moneteRaccolte.ToString("D3");
    }
}