using UnityEngine;

public class DeathZone3D : MonoBehaviour
{
    [Header("Visual")]
    public bool showInGame = true;
    public Color zoneColor = new Color(1f, 0f, 0f, 0.4f);

    void Awake()
    {
        // Se non vogliamo vederla, disattiviamo il renderer e usciamo
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (!showInGame)
        {
            if (mr != null) mr.enabled = false;
            return;
        }

        // Creazione materiale rosso (il tuo vecchio codice)
        if (mr != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
            mat.color = zoneColor;
            mr.material = mat;
        }
    }

    // --- QUESTA È LA PARTE CHE MANCAVA ---
    private void OnTriggerEnter(Collider other)
    {
        // Controlla se l'oggetto che entra ha il tag "Player"
        if (other.CompareTag("Player"))
        {
            PlayerController3D player = other.GetComponent<PlayerController3D>();
            if (player != null)
            {
                player.Die(); // Chiama la funzione morte che toglie la vita e respawna
            }
        }
    }
}