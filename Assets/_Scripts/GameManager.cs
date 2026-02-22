using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Lives")]
    public int maxLives = 3;

    [Header("Game Over")]
    public float gameOverDelay = 2f;

    public int CurrentLives { get; private set; }
    public bool IsGameOver { get; private set; }

    private Vector3 respawnPosition;
    private GameObject playerObject;
    private bool dying = false;

    // ──────────────────────────────────────────────────────────

    void Awake()
    {
        // Destroy duplicate — do NOT use DontDestroyOnLoad
        // A fresh GameManager spawns each scene load
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        CurrentLives = maxLives;
    }

    void Start()
    {
        FindPlayer();
    }

    void FindPlayer()
    {
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            respawnPosition = playerObject.transform.position;
    }

    // ──────────────────────────────────────────────────────────
    //  CHECKPOINT
    // ──────────────────────────────────────────────────────────
    public void SetCheckpoint(Vector3 position)
    {
        respawnPosition = position;
    }

    // ──────────────────────────────────────────────────────────
    //  DEATH
    // ──────────────────────────────────────────────────────────
    public void PlayerDied()
    {
        if (IsGameOver || dying) return;
        dying = true;

        CurrentLives--;

        // Notify lives UI
        if (LivesUI.Instance != null)
            LivesUI.Instance.OnPlayerDied(CurrentLives);

        if (CurrentLives <= 0)
        {
            IsGameOver = true;
            StartCoroutine(GameOver());
        }
        else
        {
            StartCoroutine(Respawn());
        }
    }

    // ──────────────────────────────────────────────────────────
    //  RESPAWN
    // ──────────────────────────────────────────────────────────
    IEnumerator Respawn()
    {
        // Re-find player in case reference went stale
        if (playerObject == null) FindPlayer();
        if (playerObject == null) { dying = false; yield break; }

        playerObject.SetActive(false);
        yield return null;

        FallingPlatform.ResetAll();
        if (BeepBlockManager.Instance != null)
            BeepBlockManager.Instance.ResetCycle();

        CharacterController cc = playerObject.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        playerObject.transform.position = respawnPosition;
        if (cc != null) cc.enabled = true;

        playerObject.SetActive(true);

        // Critical — reset CollisionChecker so player can die again
        CollisionChecker checker = playerObject.GetComponent<CollisionChecker>();
        if (checker != null) checker.ResetDead();

        dying = false;
    }

    // ──────────────────────────────────────────────────────────
    //  GAME OVER
    // ──────────────────────────────────────────────────────────
    IEnumerator GameOver()
    {
        if (playerObject != null)
            playerObject.SetActive(false);

        yield return new WaitForSeconds(gameOverDelay);

        // Reset lives BEFORE reloading so the new scene starts fresh
        CurrentLives = maxLives;
        IsGameOver = false;
        dying = false;
        Instance = null;   // clear singleton so new scene's GameManager takes over

        if (LivesUI.Instance != null)
            LivesUI.Instance.OnGameReset(maxLives);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}