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
                    // First click - select room
                    if (clickedRoom.CanBeSwapped())
                    {
                        SelectRoom(clickedRoom, gridPos);
                    }
                }
                else
                {
                    // Second click - attempt swap
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
        // Visual feedback for selection
        selectedRoom.GetComponent<SpriteRenderer>().color = Color.yellow;
    }
    
    private void CancelSelection()
    {
        if (selectedRoom != null)
        {
            selectedRoom.GetComponent<SpriteRenderer>().color = Color.white;
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
        
        // Validate that swap maintains valid paths
        if (!pathValidator.ValidateSwap(selectedPosition, targetPosition))
        {
            Debug.Log("Swap would block required paths!");
            CancelSelection();
            return false;
        }
        
        // Perform the swap
        SwapRooms(selectedPosition, targetPosition);
        CancelSelection();
        return true;
    }
    
    private void SwapRooms(Vector2Int pos1, Vector2Int pos2)
    {
        dungeonGrid.SwapRooms(pos1, pos2);
        
        // Trigger recalculation of adventurer path
        AdventurerAI.Instance.RecalculatePath();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

