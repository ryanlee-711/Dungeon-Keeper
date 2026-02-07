using UnityEngine;

using UnityEngine;

public enum RoomType
{
    Empty,
    Monster,
    Trap,
    Start,
    Goal,
    Treasure,
    Healing
}

[RequireComponent(typeof(SpriteRenderer))]
public class Room : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private RoomType roomType = RoomType.Empty;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Contents")]
    [SerializeField] private Monster monster; // optional for Monster rooms
    [SerializeField] private Trap trap;       // optional for Trap rooms

    // Public API expected by the rest of your code
    public RoomType Type => roomType;
    public Vector2Int GridPosition { get; private set; }
    public bool IsOccupiedByAdventurers { get; set; }
    public bool IsRevealed { get; set; }

    // For monster / trap access in AI
    public Monster Monster => monster;
    public Trap Trap => trap;

    void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisuals();
    }

    public bool CanBeSwapped()
    {
        // Can't swap start, goal, or rooms with adventurers inside
        return roomType != RoomType.Start &&
               roomType != RoomType.Goal &&
               !IsOccupiedByAdventurers;
    }

    public void Initialize(Vector2Int position, RoomType type)
    {
        GridPosition = position;
        roomType = type;
        UpdateVisuals();
    }

    public void SetGridPosition(Vector2Int position)
    {
        GridPosition = position;
    }

    public void SetMonster(Monster newMonster)
    {
        monster = newMonster;
        roomType = RoomType.Monster;
        UpdateVisuals();
    }

    public void SetTrap(Trap newTrap)
    {
        trap = newTrap;
        roomType = RoomType.Trap;
        UpdateVisuals();
    }

    public void ClearContents()
    {
        monster = null;
        trap = null;
        if (roomType == RoomType.Monster || roomType == RoomType.Trap)
            roomType = RoomType.Empty;
        UpdateVisuals();
    }

    public void Reveal(bool revealed)
    {
        IsRevealed = revealed;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (!spriteRenderer) return;

        if (!IsRevealed)
        {
            spriteRenderer.sprite = GameAssets.Instance
                ? GameAssets.Instance.GetFogSprite()
                : null;
        }
        else
        {
            spriteRenderer.sprite = GameAssets.Instance
                ? GameAssets.Instance.GetRoomSprite(roomType)
                : null;
        }
    }
}