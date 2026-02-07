using UnityEngine;
using UnityEngine.InputSystem;

public class RoomManipulationSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DungeonGrid dungeonGrid;
    [SerializeField] private PathValidator pathValidator;
    [SerializeField] private Camera mainCamera;

    [Header("Mana")]
    public int maxMana = 100;
    public float currentMana = 100f;
    public int swapManaCost = 10;

    public float manaRegenPerSecond = 2f;
    public float regenDelayAfterSpend = 1.0f;

    private float regenTimer;

    [Header("Optional: Override costs here (leave false to use PlayerManager's costs)")]
    // [SerializeField] private bool useLocalSwapCost = false;
    // [SerializeField] private int localSwapManaCost = 10;

    private Room selectedRoom;
    private Vector2Int selectedPosition;

    void Update() {
        HandleInput();
        RegenerateMana();
    }

    private void HandleInput()
    {
        if (Mouse.current == null) return;
        if (dungeonGrid == null || mainCamera == null) return;

        // Left click: select / swap
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 worldPos = GetMouseWorldPosition();
            Vector2Int gridPos = dungeonGrid.WorldToGridPosition(worldPos);
            Room clickedRoom = dungeonGrid.GetRoom(gridPos.x, gridPos.y);

            if (clickedRoom == null) return;

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

        // Right click: cancel selection OR (optional) respawn monster like PlayerController did
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (selectedRoom != null)
            {
                CancelSelection();
            }
            else
            {
                // Optional: respawn monster at clicked room
                Vector3 worldPos = GetMouseWorldPosition();
                Vector2Int gridPos = dungeonGrid.WorldToGridPosition(worldPos);
                Room clickedRoom = dungeonGrid.GetRoom(gridPos.x, gridPos.y);

                if (clickedRoom != null)
                    AttemptRespawnMonster(clickedRoom, gridPos);
            }
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        float zDist = -mainCamera.transform.position.z;
        return mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDist));
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
            selectedRoom.RefreshVisual(); // restores based on room type/fog/etc
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

        if (!TrySpendMana(swapManaCost))
        {
            Debug.Log("Not enough mana to swap");
            CancelSelection();
            return false;
        }

        dungeonGrid.SwapRooms(selectedPosition, targetPosition);

        // Re-plan AI if present
        if (AdventurerAI.Instance != null)
            AdventurerAI.Instance.RecalculatePath();

        CancelSelection();
        return true;
    }

    private void AttemptRespawnMonster(Room room, Vector2Int position)
    {
        // Copy of PlayerController logic (optional)
        if (room.Type != RoomType.Monster)
            return;

        if (room.Monster != null && room.Monster.Health > 0)
            return;

        Monster originalMonster = room.GetOriginalMonster();
        if (originalMonster == null)
            return;

        // if (PlayerManager.Instance == null)
        //     return;

        // if (!PlayerManager.Instance.CanRespawnMonster(originalMonster))
        //     return;

        // if (PlayerManager.Instance.TryRespawnMonster(originalMonster))
        // {
        //     Monster newMonster = new Monster
        //     {
        //         Name = originalMonster.Name,
        //         Health = Mathf.RoundToInt(originalMonster.Health * PlayerManager.Instance.GetMonsterPowerMultiplier()),
        //         AttackPower = Mathf.RoundToInt(originalMonster.AttackPower * PlayerManager.Instance.GetMonsterPowerMultiplier()),
        //         Sprite = originalMonster.Sprite
        //     };

        //     room.SetMonster(newMonster);

        //     if (AdventurerAI.Instance != null)
        //         AdventurerAI.Instance.OnRoomChanged(position);
        // }
    }

    public bool TrySpendMana(int amount)
    {
        if (currentMana < amount)
            return false;

        currentMana -= amount;
        regenTimer = regenDelayAfterSpend;
        return true;
    }

    private void RegenerateMana()
    {
        if (currentMana >= maxMana)
            return;

         if (regenTimer > 0f)
        {
            regenTimer -= Time.deltaTime;
            return;
        }

        currentMana += manaRegenPerSecond * Time.deltaTime;
        currentMana = Mathf.Min(currentMana, maxMana);
    }
}
