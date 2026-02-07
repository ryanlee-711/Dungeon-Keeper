using UnityEngine;

public class DungeonGrid : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int width = 15;
    [SerializeField] private int height = 10;
    [SerializeField] private float cellSize = 1f;

    [Header("Room Generation")]
    [SerializeField] private Room roomPrefab;
    [SerializeField] private Transform roomParent;

    private Room[,] grid;
    private Vector2Int startPosition;
    private Vector2Int goalPosition;

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;

    public Vector2Int StartPosition => startPosition;
    public Vector2Int GoalPosition => goalPosition;

    // void Awake()
    // {
    //     grid = new Room[width, height];

    //     // Default start/goal (you can randomize later)
    //     startPosition = new Vector2Int(0, 0);
    //     goalPosition = new Vector2Int(width - 1, height - 1);

    //     InitializeGrid();
    // }

    private void Awake()
{
    grid = new Room[width, height];

    startPosition = new Vector2Int(0, 0);
    goalPosition  = new Vector2Int(width - 1, height - 1);

    // AUTO cell size from prefab sprite (world units)
    if (roomPrefab != null)
    {
        cellSize = GetRoomSize();
    }

    InitializeGrid();
}

    private void InitializeGrid()
    {
        if (roomParent == null) roomParent = transform;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                RoomType type = RoomType.Empty;
                if (x == startPosition.x && y == startPosition.y) type = RoomType.Start;
                if (x == goalPosition.x && y == goalPosition.y) type = RoomType.Goal;

                Room room;
                if (roomPrefab != null)
                {
                    room = Instantiate(roomPrefab, GridToWorldPosition(x, y), Quaternion.identity, roomParent);
 
                }
                else
                {
                    // If no prefab is assigned, create a simple GameObject
                    var go = new GameObject($"Room_{x}_{y}");
                    go.transform.SetParent(roomParent);
                    go.transform.position = GridToWorldPosition(x, y);
                    go.AddComponent<SpriteRenderer>();
                    room = go.AddComponent<Room>();
                }

                room.Initialize(new Vector2Int(x, y), type);

                if (type == RoomType.Goal) room.SetRevealed(true);

                // Start room is revealed by default
                if (type == RoomType.Start) room.SetRevealed(true);

                grid[x, y] = room;
            }
        }
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
        // return new Vector3(x * cellSize, y * cellSize, 0);
        return new Vector3((x + 0.5f) * cellSize, (y + 0.5f) * cellSize, 0);
    }

    // public Vector2Int WorldToGridPosition(Vector3 worldPos)
    // {
    //     float originX = -(width * cellSize) / 2f;
    //     float originY = -(height * cellSize) / 2f;

    //     float gx = (worldPos.x - originX) / cellSize - 0.5f;
    //     float gy = (worldPos.y - originY) / cellSize - 0.5f;

    //     int x = Mathf.RoundToInt(gx);
    //     int y = Mathf.RoundToInt(gy);

    //     x = Mathf.Clamp(x, 0, width - 1);
    //     y = Mathf.Clamp(y, 0, height - 1);

    //     return new Vector2Int(x, y);
    // }
    

    // public Vector2Int WorldToGridPosition(Vector3 worldPos)
    // {
    //     // Convert world position to grid coords using nearest cell center snapping
    //     float gx = worldPos.x / cellSize;
    //     float gy = worldPos.y / cellSize;

    //     int x = Mathf.RoundToInt(gx);
    //     int y = Mathf.RoundToInt(gy);

    //     // Clamp so clicks slightly outside don’t produce invalid indices
    //     x = Mathf.Clamp(x, 0, width - 1);
    //     y = Mathf.Clamp(y, 0, height - 1);

    //     return new Vector2Int(x, y);
    // }

    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        // Since rooms are centered at (x + 0.5) * cellSize,
        // the cell boundaries are at x * cellSize.
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int y = Mathf.FloorToInt(worldPos.y / cellSize);

        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);

        return new Vector2Int(x, y);
    }



    public void SwapRooms(Vector2Int a, Vector2Int b)
    {
        if (!IsValidPosition(a.x, a.y) || !IsValidPosition(b.x, b.y))
            return;

        Room ra = grid[a.x, a.y];
        Room rb = grid[b.x, b.y];
        if (ra == null || rb == null)
            return;

        // Swap in array
        grid[a.x, a.y] = rb;
        grid[b.x, b.y] = ra;

        // Swap positions in world
        Vector3 posA = GridToWorldPosition(a.x, a.y);
        Vector3 posB = GridToWorldPosition(b.x, b.y);

        rb.transform.position = posA;
        ra.transform.position = posB;

        // Update stored grid coordinates
        rb.SetGridPosition(a);
        ra.SetGridPosition(b);
    }

    private float GetRoomSize()
    {
        var sr = roomPrefab.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
            return 1f;

        return sr.sprite.bounds.size.x;
    }
}
