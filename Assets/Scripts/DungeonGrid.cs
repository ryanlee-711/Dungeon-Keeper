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

    [Header("Room Type Counts (excluding Start/Goal)")]
    [SerializeField] private int monsterRooms = 5;
    [SerializeField] private int trapRooms = 4;
    [SerializeField] private int healRooms = 1;
    [SerializeField] private int treasureRooms = 0;

    private Room[,] grid;
    private Vector2Int startPosition;
    private Vector2Int goalPosition;

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;

    public Vector2Int StartPosition => startPosition;
    public Vector2Int GoalPosition => goalPosition;

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

        var roomTypes = GenerateRoomTypes();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                RoomType type = roomTypes[x, y];
                // if (x == startPosition.x && y == startPosition.y) type = RoomType.Start;
                // if (x == goalPosition.x && y == goalPosition.y) type = RoomType.Goal;

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
        return new Vector3((x + 0.5f) * cellSize, (y + 0.5f) * cellSize, 0);
    }


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

    private RoomType[,] GenerateRoomTypes()
    {
        var types = new RoomType[width, height];

        // Default everything to Empty
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                types[x, y] = RoomType.Empty;

        // Force Start/Goal
        types[startPosition.x, startPosition.y] = RoomType.Start;
        types[goalPosition.x, goalPosition.y] = RoomType.Goal;

        // Collect all available cells (exclude start/goal)
        var candidates = new System.Collections.Generic.List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var p = new Vector2Int(x, y);
                if (p == startPosition || p == goalPosition) continue;
                candidates.Add(p);
            }
        }

        // Shuffle candidates
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        int needed = monsterRooms + trapRooms + healRooms + treasureRooms;
        if (needed > candidates.Count)
        {
            Debug.LogWarning($"Requested {needed} special rooms, but only {candidates.Count} cells available. Clamping.");
            // clamp by reducing extras (simple approach)
            needed = candidates.Count;
        }

        int idx = 0;

        void Place(RoomType t, int count)
        {
            for (int k = 0; k < count && idx < candidates.Count; k++)
            {
                var p = candidates[idx++];
                types[p.x, p.y] = t;
                Room r = new Room();
                if(t == RoomType.Monster)
                {
                    Monster m = new Monster();
                    if (k % 2 == 0)
                    {
                        m.Name = "Vampire";
                        m.Health = 30;
                        m.AttackPower = 10;
                    }
                    else
                    {
                        m.Name = "Skeleton";
                        m.Health = 20;
                        m.AttackPower = 15;
                    }
                    r.SetMonster(m);

                    grid[p.x, p.y] = r;
                }
                if (t == RoomType.Trap)
                {
                    Trap tr = new Trap();                  
                    tr.Type = Trap.TrapType.Damage;
                    tr.Damage = 20;
                    r.SetTrap(tr);

                    grid[p.x, p.y] = r;
                }

            }
        }

        // Place in whatever priority order you want
        Place(RoomType.Healing, healRooms);
        Place(RoomType.Monster, monsterRooms);
        Place(RoomType.Trap, trapRooms);
        Place(RoomType.Treasure, treasureRooms);

        return types;
    }

}
