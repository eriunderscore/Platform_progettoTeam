// ============================================================
//  MusicManager.cs
//
//  SETUP:
//  1. Crea un GameObject vuoto chiamato "MusicManager"
//  2. Aggiungi questo script
//  3. Aggiungi un componente AudioSource sullo stesso GameObject
//  4. Assegna la tua traccia audio nel campo "musicClip"
//  5. L'AudioSource si configura automaticamente — non serve
//     impostare nulla su di esso manualmente
// ============================================================

using UnityEngine;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR
    // ══════════════════════════════════════════════════════════

    [Header("── Musica ───────────────────────────────────────")]
    [Tooltip("La traccia audio da riprodurre")]
    public AudioClip musicClip;

    [Header("── Volume ───────────────────────────────────────")]
    [Tooltip("Volume normale della musica (0-1)")]
    [Range(0f, 1f)]
    public float normalVolume = 0.8f;

    [Tooltip("Volume ridotto durante l'interazione NPC (0-1)")]
    [Range(0f, 1f)]
    public float npcVolume = 0.2f;

    [Header("── Fade ─────────────────────────────────────────")]
    [Tooltip("Secondi del fade in all'avvio")]
    public float fadeInDuration = 2f;

    [Tooltip("Secondi del fade out alla fine della traccia")]
    public float fadeOutDuration = 2f;

    [Tooltip("Secondi di silenzio tra una ripetizione e l'altra")]
    public float silenceBetweenLoops = 1.5f;

    [Tooltip("Secondi del fade quando si abbassa per l'NPC")]
    public float npcFadeDuration = 0.5f;

    // ══════════════════════════════════════════════════════════
    //  PRIVATE
    // ══════════════════════════════════════════════════════════

    private AudioSource audioSource;
    private float       targetVolume;
    private float       currentFadeSpeed;
    private bool        isFading      = false;
    private bool        isNPCActive   = false;
    private Coroutine   loopCoroutine;
    private Coroutine   fadeCoroutine;

    // ══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Configura AudioSource
        audioSource.loop        = false;
        audioSource.playOnAwake = false;
        audioSource.volume      = 0f;
    }

    void Start()
    {
        if (musicClip != null)
            loopCoroutine = StartCoroutine(MusicLoopRoutine());
        else
            Debug.LogWarning("[MusicManager] Nessun clip assegnato!");
    }

    // ══════════════════════════════════════════════════════════
    //  LOOP PRINCIPALE
    // ══════════════════════════════════════════════════════════

    IEnumerator MusicLoopRoutine()
    {
        while (true)
        {
            // Imposta volume a 0 e avvia
            audioSource.volume = 0f;
            audioSource.clip   = musicClip;
            audioSource.Play();

            float vol = isNPCActive ? npcVolume : normalVolume;

            // Fade IN
            yield return StartCoroutine(FadeVolume(0f, vol, fadeInDuration));

            // Aspetta che la traccia stia per finire (lascia spazio al fade out)
            float waitTime = musicClip.length - fadeInDuration - fadeOutDuration;
            if (waitTime > 0f)
                yield return new WaitForSeconds(waitTime);

            // Fade OUT
            yield return StartCoroutine(FadeVolume(audioSource.volume, 0f, fadeOutDuration));

            // Silenzio tra i loop
            yield return new WaitForSeconds(silenceBetweenLoops);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  FADE
    // ══════════════════════════════════════════════════════════

    IEnumerator FadeVolume(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        audioSource.volume = to;
    }

    IEnumerator FadeToVolume(float targetVol, float duration)
    {
        float from    = audioSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(from, targetVol, elapsed / duration);
            yield return null;
        }
        audioSource.volume = targetVol;
    }

    // ══════════════════════════════════════════════════════════
    //  API PUBBLICA — NPC
    // ══════════════════════════════════════════════════════════

    /// <summary>Abbassa il volume durante l'interazione NPC.</summary>
    public void OnNPCInteractionStart()
    {
        if (isNPCActive) return;
        isNPCActive = true;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeToVolume(npcVolume, npcFadeDuration));
    }

    /// <summary>Riporta il volume normale alla fine dell'interazione NPC.</summary>
    public void OnNPCInteractionEnd()
    {
        if (!isNPCActive) return;
        isNPCActive = false;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeToVolume(normalVolume, npcFadeDuration));
    }

    // ══════════════════════════════════════════════════════════
    //  API PUBBLICA — Generale
    // ══════════════════════════════════════════════════════════

    public void SetNormalVolume(float vol)
    {
        normalVolume = Mathf.Clamp01(vol);
        if (!isNPCActive)
            audioSource.volume = normalVolume;
    }

    public void SetNPCVolume(float vol)
    {
        npcVolume = Mathf.Clamp01(vol);
        if (isNPCActive)
            audioSource.volume = npcVolume;
    }
}
