using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPausa : MonoBehaviour
{
    // Funzione per riprendere il gioco
    public void Riprendi()
    {
        // Disattiva questo specifico oggetto (l'immagine e i suoi figli)
        gameObject.SetActive(false);

        // Riporta il tempo alla normalità
        Time.timeScale = 1f;
    }

    public void VaiAiLivelli(string nomeScena)
    {
        Time.timeScale = 1f; // Importante resettare il tempo prima di cambiare scena
        SceneManager.LoadScene(nomeScena);
    }

    public void EsciDalGioco()
    {
        Application.Quit();
    }
}