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

public class Room : MonoBehaviour
{
    [SerializeField] private RoomType roomType = RoomType.Empty;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public RoomType Type => roomType;
    public Vector2Int GridPosition { get; private set; }
    public bool IsOccupiedByAdventurers { get; private set; }
    public bool IsRevealed { get; private set; }

    public Monster Monster { get; private set; }
    public Trap Trap { get; private set; }

    public bool CanBeSwapped()
    {
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

    public void SetOccupiedByAdventurers(bool occupied)
    {
        IsOccupiedByAdventurers = occupied;
        UpdateVisuals();
    }

    public void SetRevealed(bool revealed)
    {
        IsRevealed = revealed;
        UpdateVisuals();
    }

    public void SetMonster(Monster monster)
    {
        Monster = monster;
        Trap = null;
        roomType = RoomType.Monster;
        UpdateVisuals();
    }

    public void SetTrap(Trap trap)
    {
        Trap = trap;
        Monster = null;
        roomType = RoomType.Trap;
        UpdateVisuals();
    }

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        // Fog-of-war: unrevealed rooms are dark
        if (!IsRevealed)
        {
            spriteRenderer.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            return;
        }

        // Simple color coding until you add sprite assets
        spriteRenderer.color = roomType switch
        {
            RoomType.Start => Color.cyan,
            RoomType.Goal => Color.magenta,
            RoomType.Monster => Color.red,
            RoomType.Trap => new Color(1f, 0.5f, 0f, 1f),
            RoomType.Healing => Color.green,
            RoomType.Treasure => Color.yellow,
            _ => Color.white
        };

        if (IsOccupiedByAdventurers)
            spriteRenderer.color = Color.blue;
    }
}
