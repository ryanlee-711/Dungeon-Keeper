using System.Diagnostics.Tracing;
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

    [SerializeField] private bool drawOutline = true;
    [SerializeField] private float outlineWidth = 0.05f;

    private LineRenderer outline;

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

    public void RefreshVisual() => UpdateVisuals();

    public void Initialize(Vector2Int position, RoomType type)
    {
        GridPosition = position;
        roomType = type;
        EnsureOutline();
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

        EnsureOutline();
    }

    private void EnsureOutline()
    {
        if (!drawOutline) return;

        if (outline == null)
        {
            outline = GetComponent<LineRenderer>();
            if (outline == null) outline = gameObject.AddComponent<LineRenderer>();

            outline.useWorldSpace = false;
            outline.loop = true;
            outline.positionCount = 5;
            outline.startWidth = outlineWidth;
            outline.endWidth = outlineWidth;

            // Default material is fine in URP/2D; if it’s pink/missing, assign a simple sprite/default material
            outline.material = new Material(Shader.Find("Sprites/Default"));
        }

        // Square border around sprite (assuming pivot centered and 1x1 scale)
        // If your sprite is scaled, this still works because it’s in local space.
        outline.SetPosition(0, new Vector3(-0.5f, -0.5f, -0.01f));
        outline.SetPosition(1, new Vector3(-0.5f,  0.5f, -0.01f));
        outline.SetPosition(2, new Vector3( 0.5f,  0.5f, -0.01f));
        outline.SetPosition(3, new Vector3( 0.5f, -0.5f, -0.01f));
        outline.SetPosition(4, new Vector3(-0.5f, -0.5f, -0.01f));

        outline.startColor = Color.black;
        outline.endColor = Color.black;
    }

}
