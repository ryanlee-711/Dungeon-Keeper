using UnityEngine;

/// <summary>
/// Simplified player controls - just swap rooms and respawn monsters
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DungeonGrid dungeonGrid;
    [SerializeField] private PathValidator pathValidator;
    [SerializeField] private Camera mainCamera;

    private Room selectedRoom;
    private Vector2Int selectedPosition;

    void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // Left click - select first room, click second to swap
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPos = dungeonGrid.WorldToGridPosition(worldPos);
            Room clickedRoom = dungeonGrid.GetRoom(gridPos.x, gridPos.y);

            if (clickedRoom != null)
            {
                if (selectedRoom == null)
                {
                    // First click - select room
                    if (clickedRoom.CanBeSwapped())
                    {
                        SelectRoom(clickedRoom, gridPos);
                    }
                }
                else
                {
                    // Second click - try to swap
                    AttemptSwap(gridPos);
                }
            }
        }

        // Right click - cancel selection OR respawn monster
        if (Input.GetMouseButtonDown(1))
        {
            if (selectedRoom != null)
            {
                CancelSelection();
            }
            else
            {
                // Try to respawn monster at clicked room
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                Vector2Int gridPos = dungeonGrid.WorldToGridPosition(worldPos);
                Room clickedRoom = dungeonGrid.GetRoom(gridPos.x, gridPos.y);

                if (clickedRoom != null)
                {
                    AttemptRespawnMonster(clickedRoom, gridPos);
                }
            }
        }
    }

    private void SelectRoom(Room room, Vector2Int position)
    {
        selectedRoom = room;
        selectedPosition = position;

        var sr = selectedRoom.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.yellow;

        Debug.Log($"Selected room at {position}");
    }

    private void CancelSelection()
    {
        if (selectedRoom != null)
        {
            var sr = selectedRoom.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.white;

            selectedRoom = null;
            Debug.Log("Selection cancelled");
        }
    }

    private void AttemptSwap(Vector2Int targetPosition)
    {
        Room targetRoom = dungeonGrid.GetRoom(targetPosition.x, targetPosition.y);

        if (targetRoom == null || !targetRoom.CanBeSwapped())
        {
            Debug.Log("Cannot swap with that room!");
            CancelSelection();
            return;
        }

        // Check if player has enough mana
        if (!PlayerManager.Instance.CanSwapRoom())
        {
            CancelSelection();
            return;
        }

        // Validate path won't be blocked
        if (!pathValidator.ValidateSwap(selectedPosition, targetPosition))
        {
            Debug.Log("Swap would block required paths!");
            CancelSelection();
            return;
        }

        // Spend mana and perform swap
        if (PlayerManager.Instance.TrySwapRoom())
        {
            dungeonGrid.SwapRooms(selectedPosition, targetPosition);

            // Notify AI
            if (AdventurerAI.Instance != null)
            {
                AdventurerAI.Instance.OnRoomChanged(selectedPosition);
                AdventurerAI.Instance.OnRoomChanged(targetPosition);
            }

            Debug.Log($"Swapped {selectedPosition} with {targetPosition}");
        }

        CancelSelection();
    }

    private void AttemptRespawnMonster(Room room, Vector2Int position)
    {
        // Can only respawn in monster rooms where monster is dead
        if (room.Type != RoomType.Monster)
        {
            Debug.Log("Can only respawn monsters in monster rooms!");
            return;
        }

        if (room.Monster != null && room.Monster.Health > 0)
        {
            Debug.Log("Monster is still alive!");
            return;
        }

        // Get original monster data (you'll need to store this on the room)
        Monster originalMonster = room.GetOriginalMonster();
        if (originalMonster == null)
        {
            Debug.Log("No monster to respawn!");
            return;
        }

        // Check if player can afford
        if (!PlayerManager.Instance.CanRespawnMonster(originalMonster))
        {
            return;
        }

        // Spend mana and respawn
        if (PlayerManager.Instance.TryRespawnMonster(originalMonster))
        {
            // Create fresh monster with upgrades applied
            Monster newMonster = new Monster
            {
                Name = originalMonster.Name,
                Health = Mathf.RoundToInt(originalMonster.Health * PlayerManager.Instance.GetMonsterPowerMultiplier()),
                AttackPower = Mathf.RoundToInt(originalMonster.AttackPower * PlayerManager.Instance.GetMonsterPowerMultiplier()),
                Sprite = originalMonster.Sprite
            };

            room.SetMonster(newMonster);

            // Notify AI
            if (AdventurerAI.Instance != null)
            {
                AdventurerAI.Instance.OnRoomChanged(position);
            }

            Debug.Log($"Respawned {newMonster.Name} at {position} with {newMonster.Health} HP");
        }
    }
}
