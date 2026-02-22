using UnityEngine;

public class Checkpoint3D : MonoBehaviour
{
    [Header("Spawn")]
    public Vector3 spawnOffset = new Vector3(0f, 1f, 0f);

    [Header("Colors")]
    public Color inactiveColor = Color.white;
    public Color activeColor = Color.green;

    public bool IsActivated { get; private set; }
    private MeshRenderer meshRenderer;
    private Material matInstance;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            // Create a brand new simple material so there are zero shader issues
            matInstance = new Material(Shader.Find("Standard"));
            matInstance.color = inactiveColor;
            meshRenderer.material = matInstance;
        }
    }

    public void Activate()
    {
        if (IsActivated) return;
        IsActivated = true;

        if (matInstance != null)
            matInstance.color = activeColor;

        Vector3 spawnPos = transform.position + spawnOffset;

        if (GameManager.Instance != null)
            GameManager.Instance.SetCheckpoint(spawnPos);

        Debug.Log($"[Checkpoint] Saved respawn at {spawnPos}");
    }

    public void ResetCheckpoint()
    {
        IsActivated = false;
        if (matInstance != null)
            matInstance.color = inactiveColor;
    }
}