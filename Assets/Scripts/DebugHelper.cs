using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple debug UI to display game state during testing
/// Shows: AI health, game time, next move timer, AI state
/// </summary>
public class DebugUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AdventurerAI adventurerAI;
    [SerializeField] private AdventurerMovementController movementController;
    [SerializeField] private GameManager gameManager;

    [Header("UI Text")]
    [SerializeField] private Text statusText;

    void Start()
    {
        if (adventurerAI == null)
            adventurerAI = AdventurerAI.Instance;

        if (movementController == null)
            movementController = FindObjectOfType<AdventurerMovementController>();

        if (gameManager == null)
            gameManager = GameManager.Instance;
    }

    void Update()
    {
        if (statusText == null) return;

        string status = "";

        // Game state
        if (gameManager != null)
        {
            status += $"Game Time: {gameManager.GameTime:F1}s\n";
            status += $"Game Status: {(gameManager.IsGameOver ? "GAME OVER" : "PLAYING")}\n";
            status += "\n";
        }

        // AI health and state
        if (adventurerAI != null)
        {
            // Use reflection to get private health field for debug display
            var healthField = adventurerAI.GetType().GetField("currentHealth",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (healthField != null)
            {
                int health = (int)healthField.GetValue(adventurerAI);
                var maxHealthField = adventurerAI.GetType().GetField("maxHealth",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                int maxHealth = maxHealthField != null ? (int)maxHealthField.GetValue(adventurerAI) : 100;

                status += $"Adventurer Health: {health}/{maxHealth}\n";
            }

            status += $"Position: {adventurerAI.GridPosition}\n";
            status += $"Desperate Mode: {(adventurerAI.IsInDesperateMode ? "YES" : "NO")}\n";
            status += "\n";
        }

        // Movement state
        if (movementController != null)
        {
            status += $"Next Move In: {movementController.TimeUntilNextMove:F1}s\n";
            status += $"Is Moving: {movementController.IsMoving}\n";
            status += $"Is Paused: {movementController.IsPaused}\n";
            status += "\n";
        }

        // Controls
        status += "=== CONTROLS ===\n";
        status += "Left Click: Select/Swap Rooms\n";
        status += "Right Click: Cancel Selection\n";
        status += "R: Restart Game\n";
        status += "ESC: Pause Game\n";

        statusText.text = status;
    }

    void OnGUI()
    {
        // Fallback if no Text component assigned
        if (statusText != null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Box("DUNGEON DEFENSE - DEBUG");

        if (gameManager != null)
        {
            GUILayout.Label($"Time: {gameManager.GameTime:F1}s");
            GUILayout.Label($"Status: {(gameManager.IsGameOver ? "GAME OVER" : "PLAYING")}");
        }

        if (adventurerAI != null)
        {
            var healthField = adventurerAI.GetType().GetField("currentHealth",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (healthField != null)
            {
                int health = (int)healthField.GetValue(adventurerAI);
                GUILayout.Label($"AI Health: {health}");
            }

            GUILayout.Label($"Position: {adventurerAI.GridPosition}");
            GUILayout.Label($"Desperate: {adventurerAI.IsInDesperateMode}");
        }

        if (movementController != null)
        {
            GUILayout.Label($"Next Move: {movementController.TimeUntilNextMove:F1}s");
        }

        GUILayout.EndArea();
    }
}
