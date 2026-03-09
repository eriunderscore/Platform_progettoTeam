using UnityEngine;

public class DeathZone3D : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Uncheck to make the death zone invisible in the final game")]
    public bool showInGame = true;
    public Color zoneColor = new Color(1f, 0f, 0f, 0.4f);

    void Awake()
    {
        if (!showInGame) return;

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null) return;

        // Build a transparent red material at runtime
        Material mat = new Material(Shader.Find("Standard"));

        // Set Standard shader to Transparent mode
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