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
        if (Mouse.current == null) return;

        // Left click: select / swap
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (dungeonGrid == null || mainCamera == null) return;

            Vector3 worldPos = GetMouseWorldPosition();
            Vector2Int gridPos = dungeonGrid.WorldToGridPosition(worldPos);
            Room clickedRoom = dungeonGrid.GetRoom(gridPos.x, gridPos.y);

            if (clickedRoom == null) return;

            if (selectedRoom == null)
            {
                // First click = select
                if (clickedRoom.CanBeSwapped())
                    SelectRoom(clickedRoom, gridPos);
            }
            else
            {
                // Second click = swap attempt
                AttemptSwap(gridPos);
            }
        }

        // Right click: cancel selection
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelSelection();
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();

        // Correct Z distance so ScreenToWorldPoint hits the Z=0 plane
        float zDist = -mainCamera.transform.position.z;
        return mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDist));
    }

    private void SelectRoom(Room room, Vector2Int position)
    {
        selectedRoom = room;
        selectedPosition = position;

        // Visual feedback
        var sr = selectedRoom.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.yellow;
    }

    private void CancelSelection()
    {
        if (selectedRoom != null)
        {
            // Restore its normal color (based on type/fog/occupied state)
            selectedRoom.RefreshVisual();
            selectedRoom = null;
        }
    }

    public bool AttemptSwap(Vector2Int targetPosition)
    {
        if (dungeonGrid == null) { CancelSelection(); return false; }

        Room targetRoom = dungeonGrid.GetRoom(targetPosition.x, targetPosition.y);

        if (targetRoom == null || !targetRoom.CanBeSwapped())
        {
            CancelSelection();
            return false;
        }

        // Validate swap maintains paths (if validator exists)
        if (pathValidator != null && !pathValidator.ValidateSwap(selectedPosition, targetPosition))
        {
            Debug.Log("Swap would block required paths!");
            CancelSelection();
            return false;
        }

        // Perform swap
        dungeonGrid.SwapRooms(selectedPosition, targetPosition);

        // Re-plan AI if present
        if (AdventurerAI.Instance != null)
            AdventurerAI.Instance.RecalculatePath();

        CancelSelection();
        return true;
    }
}
