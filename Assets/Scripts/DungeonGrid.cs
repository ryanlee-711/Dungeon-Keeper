using UnityEngine;


public class DungeonGrid : MonoBehaviour
{
    [SerializeField] private int width = 15;
    [SerializeField] private int height = 10;
    [SerializeField] private float cellSize = 1f;
    
    private Room[,] grid;
    private Vector2Int startPosition;
    private Vector2Int goalPosition;
    
    void Awake()
    {
        grid = new Room[width, height];
        InitializeGrid();
    }
    
    public Room GetRoom(int x, int y)
    {
        if (IsValidPosition(x, y))
            return grid[x, y];
        return null;
    }
    
    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
    
    public Vector3 GridToWorldPosition(int x, int y)
    {
        return new Vector3(x * cellSize, y * cellSize, 0);
    }
    
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / cellSize),
            Mathf.FloorToInt(worldPos.y / cellSize)
        );
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