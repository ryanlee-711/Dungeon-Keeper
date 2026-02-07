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
    [SerializeField] private RoomType roomType;
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    public RoomType Type => roomType;
    public Vector2Int GridPosition { get; private set; }
    public bool IsOccupiedByAdventurers { get; set; }
    public bool IsRevealed { get; set; }
    
    // For monster rooms
    public Monster Monster { get; private set; }
    
    // For trap rooms
    public Trap Trap { get; private set; }
    
    public bool CanBeSwapped()
    {
        // Can't swap start, goal, or rooms with adventurers
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
    
    public void SetMonster(Monster monster)
    {
        Monster = monster;
        roomType = RoomType.Monster;
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        // Update sprite based on room type and revealed status
        if (!IsRevealed)
        {
            spriteRenderer.sprite = GameAssets.Instance.GetFogSprite();
        }
        else
        {
            spriteRenderer.sprite = GameAssets.Instance.GetRoomSprite(roomType);
        }
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
