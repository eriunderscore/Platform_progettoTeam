using UnityEngine;

/// <summary>
/// Attach to any block you want to be a beep block.
/// Set Type to A or B in the Inspector.
/// The block listens to BeepBlockManager for timing.
///
/// SETUP:
/// 1. Place ONE BeepBlockManager anywhere in the scene
/// 2. Add BeepBlock to any platform/cube
/// 3. Set Type = A or B
/// 4. Make sure the block has a MeshRenderer and a Collider
/// </summary>
public class BeepBlock : MonoBehaviour
{
    public enum BlockType { A, B }

    [Header("Block Type")]
    public BlockType type = BlockType.A;

    // ── Private ───────────────────────────────────────────────
    private MeshRenderer meshRenderer;
    private Collider[] colliders;
    private Material mat;

    // We use MaterialPropertyBlock to avoid creating new material instances
    private MaterialPropertyBlock propBlock;

    private static readonly int ColorProp = Shader.PropertyToID("_Color");

    // ──────────────────────────────────────────────────────────

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        colliders = GetComponents<Collider>();
        propBlock = new MaterialPropertyBlock();

        // Store original color from material
        if (meshRenderer != null)
            meshRenderer.GetPropertyBlock(propBlock);
    }

    void Update()
    {
        if (BeepBlockManager.Instance == null) return;

        bool shouldBeActive = (type == BlockType.A)
            ? BeepBlockManager.Instance.AIsActive
            : !BeepBlockManager.Instance.AIsActive;

        if (shouldBeActive)
            UpdateActiveBlock();
        else
            SetInactive();
    }

    // ──────────────────────────────────────────────────────────
    //  ACTIVE — solid, visible, with warning flicker near swap
    // ──────────────────────────────────────────────────────────
    void UpdateActiveBlock()
    {
        // Make sure physics is on
        SetColliders(true);
        if (meshRenderer != null) meshRenderer.enabled = true;

        float alpha = 1f;

        if (BeepBlockManager.Instance.InWarning)
        {
            float wp = BeepBlockManager.Instance.WarningProgress; // 0 → 1

            // Frequency increases as warning progresses
            float freq = Mathf.Lerp(
                BeepBlockManager.Instance.flickerFreqStart,
                BeepBlockManager.Instance.flickerFreqEnd,
                wp);

            // Sine wave oscillation
            float sine = (Mathf.Sin(Time.time * freq) + 1f) * 0.5f;  // 0 → 1

            // Flicker intensity increases — at wp=0 barely flickers, at wp=1 dips fully
            float minAlpha = Mathf.Lerp(0.85f, BeepBlockManager.Instance.flickerMinAlpha, wp);
            alpha = Mathf.Lerp(minAlpha, 1f, sine);
        }

        SetAlpha(alpha);
    }

    // ──────────────────────────────────────────────────────────
    //  INACTIVE — invisible and passable
    // ──────────────────────────────────────────────────────────
    void SetInactive()
    {
        SetColliders(false);
        if (meshRenderer != null) meshRenderer.enabled = false;
    }

    // ──────────────────────────────────────────────────────────
    //  HELPERS
    // ──────────────────────────────────────────────────────────
    void SetColliders(bool enabled)
    {
        foreach (var col in colliders)
            col.enabled = enabled;
    }

    void SetAlpha(float alpha)
    {
        if (meshRenderer == null) return;

        meshRenderer.GetPropertyBlock(propBlock);
        Color c = propBlock.GetColor(ColorProp);

        // If no color set yet, read from the actual material
        if (c == Color.clear)
            c = meshRenderer.sharedMaterial.color;

        c.a = alpha;
        propBlock.SetColor(ColorProp, c);
        meshRenderer.SetPropertyBlock(propBlock);
    }

    // Show outline in editor so you can tell A from B
    void OnDrawGizmosSelected()
    {
        Gizmos.color = type == BlockType.A
            ? new Color(1f, 0.4f, 0.1f, 0.4f)   // orange for A
            : new Color(0.1f, 0.4f, 1f, 0.4f);   // blue for B
        Gizmos.DrawCube(transform.position, transform.localScale * 1.05f);
    }
}