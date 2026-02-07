using UnityEngine;

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
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
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

        if (Input.GetMouseButtonDown(1))
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
            var sr = selectedRoom.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.white;

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

        if (!pathValidator.ValidateSwap(selectedPosition, targetPosition))
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
