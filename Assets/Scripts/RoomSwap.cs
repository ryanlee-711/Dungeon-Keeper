using UnityEngine;
using UnityEngine.InputSystem;

public class RoomManipulationSystem : MonoBehaviour
{
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
        // Left click
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));

            Vector2Int gridPos = dungeonGrid.WorldToGridPosition(worldPos);
            Room clickedRoom = dungeonGrid.GetRoom(gridPos.x, gridPos.y);

            if (clickedRoom != null)
            {
                if (selectedRoom == null)
                {
                    if (clickedRoom.CanBeSwapped())
                        SelectRoom(clickedRoom, gridPos);
                }
                else
                {
                    AttemptSwap(gridPos);
                }
            }
        }

        // Right click cancels
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelSelection();
        }
    }

    private void SelectRoom(Room room, Vector2Int position)
    {
        selectedRoom = room;
        selectedPosition = position;

        var sr = selectedRoom.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.yellow;
    }

    private void CancelSelection()
    {
        if (selectedRoom != null)
        {
            // Re-apply visuals (instead of forcing white)
            selectedRoom.SetRevealed(true); // keeps fog logic happy if you’re using it
            // If you want: selectedRoom.GetComponent<SpriteRenderer>().color = ...
            selectedRoom = null;
        }
    }

    public bool AttemptSwap(Vector2Int targetPosition)
    {
        Room targetRoom = dungeonGrid.GetRoom(targetPosition.x, targetPosition.y);

        if (targetRoom == null || !targetRoom.CanBeSwapped())
        {
            CancelSelection();
            return false;
        }

        if (pathValidator != null && !pathValidator.ValidateSwap(selectedPosition, targetPosition))
        {
            Debug.Log("Swap would block required paths!");
            CancelSelection();
            return false;
        }

        dungeonGrid.SwapRooms(selectedPosition, targetPosition);

        if (AdventurerAI.Instance != null)
            AdventurerAI.Instance.RecalculatePath();

        CancelSelection();
        return true;
    }
}
