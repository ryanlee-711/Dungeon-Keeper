using UnityEngine;

/// <summary>
/// Controls real-time movement for the adventurer AI
/// The AI moves continuously through the dungeon while the player frantically rearranges rooms
/// </summary>
public class AdventurerMovementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AdventurerAI adventurerAI;
    [SerializeField] private Transform adventurerVisual; // The sprite/model that moves smoothly

    [Header("Movement Settings")]
    [Tooltip("Time in seconds between each room move")]
    [SerializeField] private float moveInterval = 2f;

    [Tooltip("Time it takes to smoothly move between rooms visually")]
    [SerializeField] private float moveDuration = 0.5f;

    [Tooltip("Should the AI pause briefly after combat?")]
    [SerializeField] private bool pauseAfterCombat = true;

    [Tooltip("How long to pause after combat (seconds)")]
    [SerializeField] private float combatPauseTime = 1f;

    private float moveTimer = 0f;
    private bool isMoving = false;
    private bool isPaused = false;
    private float pauseTimer = 0f;

    // For smooth visual movement
    private Vector3 moveStartPos;
    private Vector3 moveTargetPos;
    private float moveProgress = 0f;

    void Start()
    {
        if (adventurerAI == null)
        {
            adventurerAI = AdventurerAI.Instance;
        }

        if (adventurerVisual == null && adventurerAI != null)
        {
            adventurerVisual = adventurerAI.transform;
        }

        // Start at the adventurer's initial position
        if (adventurerVisual != null && adventurerAI != null)
        {
            Vector2Int gridPos = adventurerAI.GridPosition;
            Vector3 worldPos = GetWorldPosition(gridPos);
            adventurerVisual.position = worldPos;
        }
    }

    void Update()
    {
        if (adventurerAI == null || adventurerVisual == null) return;

        // Handle pause (after combat, etc.)
        if (isPaused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0)
            {
                isPaused = false;
            }
            return;
        }

        // Handle smooth visual movement
        if (isMoving)
        {
            moveProgress += Time.deltaTime / moveDuration;
            adventurerVisual.position = Vector3.Lerp(moveStartPos, moveTargetPos, moveProgress);

            if (moveProgress >= 1f)
            {
                isMoving = false;
                adventurerVisual.position = moveTargetPos;
            }
            return;
        }

        // Handle movement timer
        moveTimer += Time.deltaTime;

        if (moveTimer >= moveInterval)
        {
            moveTimer = 0f;
            MoveAdventurer();
        }
    }

    private void MoveAdventurer()
    {
        Vector2Int previousPos = adventurerAI.GridPosition;

        // Tell AI to calculate and move to next room
        adventurerAI.MoveToNextRoom();

        Vector2Int newPos = adventurerAI.GridPosition;

        // If position changed, animate the movement
        if (previousPos != newPos)
        {
            StartSmoothMove(previousPos, newPos);

            if (adventurerAI.HasReachedGoal)
            {
                isMoving = false;
                adventurerVisual.position = GetWorldPosition(newPos);
            }
        }

        // Check if we should pause (e.g., after combat)
        if (pauseAfterCombat && adventurerAI.JustFoughtCombat)
        {
            PauseMovement(combatPauseTime);
        }
    }

    private void StartSmoothMove(Vector2Int from, Vector2Int to)
    {
        moveStartPos = GetWorldPosition(from);
        moveTargetPos = GetWorldPosition(to);
        moveProgress = 0f;
        isMoving = true;
    }

    private Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        // Get the dungeon grid reference
        var dungeonGrid = FindObjectOfType<DungeonGrid>();
        if (dungeonGrid != null)
        {
            return dungeonGrid.GridToWorldPosition(gridPos.x, gridPos.y);
        }

        // Fallback if DungeonGrid not found
        return new Vector3(gridPos.x, gridPos.y, 0);
    }

    public void PauseMovement(float duration)
    {
        isPaused = true;
        pauseTimer = duration;
    }

    public void ResumeMovement()
    {
        isPaused = false;
        pauseTimer = 0f;
    }

    public void SetMoveSpeed(float newInterval)
    {
        moveInterval = Mathf.Max(0.1f, newInterval);
    }

    // Public getters for UI/debugging
    public float TimeUntilNextMove => Mathf.Max(0, moveInterval - moveTimer);
    public bool IsPaused => isPaused;
    public bool IsMoving => isMoving;
}