using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages the real-time dungeon defense game state
/// Handles win/lose conditions, game speed, and events
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private AdventurerAI adventurerAI;
    [SerializeField] private AdventurerMovementController movementController;

    [Header("Game State")]
    [SerializeField] private bool gameStarted = false;
    [SerializeField] private float gameTime = 0f;

    [Header("Speed Control")]
    [Tooltip("Base speed: time between AI moves (lower = faster game)")]
    [SerializeField] private float baseGameSpeed = 2f;

    [Tooltip("Should the game get faster over time?")]
    [SerializeField] private bool increaseDifficultyOverTime = true;

    [Tooltip("How much faster per minute (multiplier)")]
    [SerializeField] private float speedIncreasePerMinute = 0.1f;

    [Header("Events")]
    public UnityEvent OnGameStart;
    public UnityEvent OnPlayerWin;
    public UnityEvent OnPlayerLose;
    public UnityEvent<int> OnAdventurerHealthChanged;

    private bool gameOver = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (adventurerAI == null)
        {
            adventurerAI = AdventurerAI.Instance;
        }

        if (movementController == null)
        {
            movementController = FindObjectOfType<AdventurerMovementController>();
        }

        // Don't auto-start, wait for player input
        if (gameStarted)
        {
            StartGame();
        }
    }

    void Update()
    {
        if (!gameStarted || gameOver) return;

        gameTime += Time.deltaTime;

        // Gradually increase difficulty (speed up the game)
        if (increaseDifficultyOverTime && movementController != null)
        {
            float speedMultiplier = 1f + (gameTime / 60f) * speedIncreasePerMinute;
            float currentSpeed = baseGameSpeed / speedMultiplier;
            movementController.SetMoveSpeed(currentSpeed);
        }

        // Check for manual start/restart
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PauseGame();
        }
    }

    public void StartGame()
    {
        gameStarted = true;
        gameOver = false;
        gameTime = 0f;

        Debug.Log("Game Started! Defend your dungeon!");
        OnGameStart?.Invoke();
    }

    public void PlayerWins()
    {
        if (gameOver) return;

        gameOver = true;
        gameStarted = false;

        Debug.Log($"Player Wins! Adventurer defeated in {gameTime:F1} seconds!");
        OnPlayerWin?.Invoke();

        if (movementController != null)
        {
            movementController.PauseMovement(999f); // Stop movement
        }
    }

    public void PlayerLoses()
    {
        if (gameOver) return;

        gameOver = true;
        gameStarted = false;

        Debug.Log($"Player Loses! Adventurer reached the goal in {gameTime:F1} seconds!");
        OnPlayerLose?.Invoke();

        if (movementController != null)
        {
            movementController.PauseMovement(999f);
        }
    }

    public void RestartGame()
    {
        Debug.Log("Restarting game...");
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public void PauseGame()
    {
        Time.timeScale = Time.timeScale > 0 ? 0 : 1;
        Debug.Log(Time.timeScale > 0 ? "Game Resumed" : "Game Paused");
    }

    public void SetGameSpeed(float speed)
    {
        baseGameSpeed = Mathf.Max(0.1f, speed);
        if (movementController != null)
        {
            movementController.SetMoveSpeed(baseGameSpeed);
        }
    }

    // Called by AdventurerAI when health changes
    public void OnAdventurerTookDamage(int newHealth)
    {
        OnAdventurerHealthChanged?.Invoke(newHealth);

        if (newHealth <= 0)
        {
            PlayerWins();
        }
    }

    // Called by AdventurerAI when goal is reached
    public void OnAdventurerReachedGoal()
    {
        PlayerLoses();
    }

    // Public getters for UI
    public float GameTime => gameTime;
    public bool IsGameOver => gameOver;
    public bool IsGameStarted => gameStarted;
    public float CurrentGameSpeed => movementController != null ? movementController.TimeUntilNextMove : 0f;
}