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
    Healing,
    Blocked
}

public class Room : MonoBehaviour
{
    [SerializeField] public RoomType roomType = RoomType.Empty;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private bool drawOutline = true;
    [SerializeField] private float outlineWidth = 0.05f;

    [SerializeField] private Sprite blockedSprite;

    [SerializeField] private GameObject monsterVisualRoot; // drag MonsterVisual here
    [SerializeField] private SpriteRenderer monsterVisualRenderer; // drag its SpriteRenderer
    private Sprite normalSprite;

    private LineRenderer outline;

    public RoomType Type => roomType;
    public Vector2Int GridPosition { get; private set; }
    public bool IsOccupiedByAdventurers { get; private set; }
    public bool IsRevealed { get; private set; }

    public Monster Monster { get; set; }
    public Trap Trap { get; set; }

    // Store original monster for respawning
    private Monster originalMonster;

    private void CacheRenderer()
    {
        if (spriteRenderer != null) return;

        // Try same object first
        spriteRenderer = GetComponent<SpriteRenderer>();

        // If sprite is on a child (very common), grab it
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void Awake()
    {
        CacheRenderer();
    }

    private void OnValidate()
    {
        CacheRenderer();
    }

    private void Reset()
    {
        CacheRenderer();
    }

    public bool CanBeSwapped()
    {
        return roomType != RoomType.Start &&
               roomType != RoomType.Goal &&
               !IsOccupiedByAdventurers;
    }

    public void RefreshVisual() => UpdateVisuals();
    // private void OnValidate()
    // {
    //     if (spriteRenderer == null)
    //         spriteRenderer = GetComponent<SpriteRenderer>();
    // }

    public void Initialize(Vector2Int position, RoomType type)
    {
        CacheRenderer();
        if (spriteRenderer != null && normalSprite == null)
            normalSprite = spriteRenderer.sprite;

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


        // Store original monster data for respawning (only if not already set)
        if (originalMonster == null && monster != null)
        {
            originalMonster = new Monster
            {
                Name = monster.Name,
                Health = monster.Health,
                AttackPower = monster.AttackPower,
                Sprite = monster.Sprite
            };
        }
        UpdateVisuals();
    }

    public Monster GetOriginalMonster()
    {
        return originalMonster;
    }

    public void SetTrap(Trap trap)
    {
        Trap = trap;
        Monster = null;
        roomType = RoomType.Trap;
        UpdateVisuals();
    }

    // private void Reset()
    // {
    //     spriteRenderer = GetComponent<SpriteRenderer>();
    // }

    private void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        // Base color by room type
         if (roomType == RoomType.Blocked)
        {
            // Debug.Log($"[BLOCKED] Room {name} renderer={spriteRenderer.name} color={spriteRenderer.color} sprite={spriteRenderer.sprite}", this);
            if (blockedSprite != null) spriteRenderer.sprite = blockedSprite;
            spriteRenderer.color = new Color(0f, 0f, 0f, 1f);
            return;
        } else
        {
            if (normalSprite != null) spriteRenderer.sprite = normalSprite;
        }
        
        Color baseColor = roomType switch
        {
            RoomType.Start    => Color.cyan,
            RoomType.Goal     => Color.magenta,
            RoomType.Monster  => Color.red,
            RoomType.Trap     => new Color(1f, 0.5f, 0f, 1f),
            RoomType.Healing  => Color.green,
            RoomType.Treasure => Color.yellow,
            // RoomType.Blocked  => Color.black,
            _                 => Color.white
        };

        // Dim if monster is dead
        if (roomType == RoomType.Monster && Monster != null && Monster.Health <= 0)
            baseColor = Color.gray;

        // Occupied overrides (up to you whether this should override fog too)
        if (IsOccupiedByAdventurers)
            baseColor = Color.blue;

        // Fog-of-war: unrevealed rooms keep their type color but are dark/transparent
        if (!IsRevealed)
        {
            // Keep the type color, just make it see-through
            baseColor.a = 0.7f;

            // Optional: slightly wash toward gray so it reads as "hidden"
            // baseColor = Color.Lerp(baseColor, Color.gray, 0.35f);
        }
        else
        {
            baseColor.a = 1f;
        }

        spriteRenderer.color = baseColor;

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

    public void ShowMonster(GameObject monsterPrefab)
    {
        if (monsterVisualRoot == null || monsterVisualRenderer == null) return;

        // Pull the sprite from the prefab (or from a SpriteRenderer on it)
        var sr = monsterPrefab.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) monsterVisualRenderer.sprite = sr.sprite;

        monsterVisualRoot.SetActive(true);
    }

    public void HideMonster()
    {
        if (monsterVisualRoot != null)
            monsterVisualRoot.SetActive(false);
    }

}
