using System.Collections;
using UnityEngine;

public class FallingPlatform : MonoBehaviour
{
    [Header("Timing")]
    public float shakeDelay     = 1.2f;
    public float shakeIntensity = 0.05f;
    public float shakeSpeed     = 40f;

    [Header("Fall")]
    public float fallSpeed    = 12f;
    public float fallDistance = 30f;

    [Header("Respawn")]
    [Tooltip("Secondi prima che la piattaforma ritorni")]
    public float respawnDelay = 5f;

    // ── Private ───────────────────────────────────────────────
    private Vector3  startPosition;
    private bool     triggered    = false;
    private bool     isFalling    = false;
    private float    fallProgress = 0f;

    private Renderer[]  renderers;
    private Collider[]  colliders;

    private static System.Collections.Generic.List<FallingPlatform> all
        = new System.Collections.Generic.List<FallingPlatform>();

    // ──────────────────────────────────────────────────────────

    void Awake()
    {
        startPosition = transform.position;
        renderers     = GetComponentsInChildren<Renderer>();
        colliders     = GetComponentsInChildren<Collider>();
        all.Add(this);
    }

    void OnDestroy() { all.Remove(this); }

    void Update()
    {
        if (!isFalling) return;

        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        fallProgress       += fallSpeed * Time.deltaTime;

        if (fallProgress >= fallDistance)
        {
            isFalling = false;
            StartCoroutine(RespawnRoutine());
        }
    }

    // ──────────────────────────────────────────────────────────

    public void TriggerFall()
    {
        if (triggered) return;
        triggered = true;
        StartCoroutine(ShakeAndFall());
    }

    IEnumerator ShakeAndFall()
    {
        float elapsed = 0f;

        while (elapsed < shakeDelay)
        {
            elapsed += Time.deltaTime;
            float ox = Mathf.Sin(elapsed * shakeSpeed) * shakeIntensity;
            float oz = Mathf.Sin(elapsed * shakeSpeed * 0.7f) * shakeIntensity;
            transform.position = startPosition + new Vector3(ox, 0f, oz);
            yield return null;
        }

        transform.position = startPosition;
        isFalling = true;
    }

    IEnumerator RespawnRoutine()
    {
        // Rendi invisibile e disabilita collider — ma il GameObject rimane attivo
        // così la Coroutine continua a girare
        SetVisible(false);

        yield return new WaitForSeconds(respawnDelay);

        // Riporta alla posizione originale
        transform.position = startPosition;
        fallProgress       = 0f;
        triggered          = false;
        isFalling          = false;

        // Riappare con collider riattivato
        SetVisible(true);
    }

    void SetVisible(bool visible)
    {
        foreach (var r in renderers) r.enabled = visible;
        foreach (var c in colliders) c.enabled = visible;
    }

    // ──────────────────────────────────────────────────────────
    //  RESET ON PLAYER DEATH
    // ──────────────────────────────────────────────────────────
    public static void ResetAll()
    {
        foreach (var p in all)
        {
            if (p == null) continue;
            p.StopAllCoroutines();
            p.transform.position = p.startPosition;
            p.triggered          = false;
            p.isFalling          = false;
            p.fallProgress       = 0f;
            p.SetVisible(true);
        }
    }
}
