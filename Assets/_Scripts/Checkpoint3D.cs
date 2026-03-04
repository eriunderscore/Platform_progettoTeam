using System.Collections;
using UnityEngine;

public class Checkpoint3D : MonoBehaviour
{
    private bool isActivated = false;
    public bool IsActivated => isActivated;

    [Header("─── Color Settings ───")]
    [Tooltip("Color of the checkpoint before activation")]
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f); // grey
    [Tooltip("Color of the checkpoint after activation")]
    public Color activeColor = new Color(1f, 0.8f, 0.1f);     // gold
    [Tooltip("How long the fade transition takes in seconds")]
    public float fadeDuration = 0.8f;

    // ── Private ──
    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();

        // Set starting color without creating a new material instance
        SetColor(inactiveColor);
    }

    public void Activate() => isActivated = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController3D player = other.GetComponent<PlayerController3D>();
            if (player != null)
            {
                player.SetRespawnPoint(transform.position);

                if (!isActivated)
                {
                    isActivated = true;
                    Debug.Log("Checkpoint salvato in questa posizione!");
                    StartCoroutine(FadeToColor(inactiveColor, activeColor));
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────
    //  COLOR
    // ──────────────────────────────────────────────────────────

    /// <summary>Instantly sets the material color using a PropertyBlock.
    /// PropertyBlocks avoid creating duplicate materials in memory.</summary>
    private void SetColor(Color color)
    {
        if (_renderer == null) return;
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_Color", color);
        _renderer.SetPropertyBlock(_propBlock);
    }

    /// <summary>Smoothly fades from one color to another over fadeDuration seconds.</summary>
    private IEnumerator FadeToColor(Color from, Color to)
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            // SmoothStep makes the fade ease in and out instead of being linear
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            SetColor(Color.Lerp(from, to, smoothT));

            yield return null;
        }

        // Snap to exact final color to avoid floating point drift
        SetColor(to);
    }
}