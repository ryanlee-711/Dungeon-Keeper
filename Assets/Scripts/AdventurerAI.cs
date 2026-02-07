using UnityEngine;
using System.Collections.Generic;

public class AdventurerAI : MonoBehaviour
{
    public static AdventurerAI Instance { get; private set; }

    [SerializeField] private DungeonGrid dungeonGrid;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int attackPower = 20;
    [SerializeField] private int visionRange = 3;

    private int currentHealth;
    private Vector2Int currentPosition;
    private readonly HashSet<Vector2Int> exploredRooms = new HashSet<Vector2Int>();
    private List<Vector2Int> currentPath;

    public Vector2Int GridPosition => currentPosition;

    void Awake()
    {
        Instance = this;
        currentHealth = maxHealth;
    }

    void Start()
    {
        if (dungeonGrid == null)
        {
            Debug.LogError("AdventurerAI: dungeonGrid not assigned.");
            enabled = false;
            return;
        }

        currentPosition = dungeonGrid.StartPosition;
        SetOccupancy(currentPosition, true);
        RecalculatePath();
    }

    private void SetOccupancy(Vector2Int pos, bool occupied)
    {
        Room r = dungeonGrid.GetRoom(pos.x, pos.y);
        if (r != null) r.SetOccupiedByAdventurers(occupied);
    }

    public void RecalculatePath()
    {
        currentPath = CalculateOptimalPath();

        // If the path includes our current position as first node, remove it
        if (currentPath != null && currentPath.Count > 0 && currentPath[0] == currentPosition)
            currentPath.RemoveAt(0);
    }

    private List<Vector2Int> CalculateOptimalPath()
    {
        return AStarWithDanger(currentPosition, dungeonGrid.GoalPosition);
    }

    private List<Vector2Int> AStarWithDanger(Vector2Int start, Vector2Int goal)
    {
        Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
        Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        PriorityQueue<Vector2Int> openSet = new PriorityQueue<Vector2Int>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        gScore[start] = 0;
        fScore[start] = Heuristic(start, goal);
        openSet.Enqueue(start, fScore[start]);

        while (openSet.Count > 0)
        {
            Vector2Int current = openSet.Dequeue();

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            closedSet.Add(current);

            foreach (Vector2Int neighbor in GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor))
                    continue;

                float moveCost = GetMoveCost(current, neighbor);
                if (moveCost >= float.MaxValue / 2f) continue;

                float tentativeGScore = gScore[current] + moveCost;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor))
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }

        return null;
    }

    private List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        Vector2Int[] dirs =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

        List<Vector2Int> result = new List<Vector2Int>(4);
        foreach (var d in dirs)
        {
            var n = pos + d;
            if (dungeonGrid.IsValidPosition(n.x, n.y))
                result.Add(n);
        }

        return result;
    }

    private float GetMoveCost(Vector2Int from, Vector2Int to)
    {
        float baseCost = 1f;
        Room room = dungeonGrid.GetRoom(to.x, to.y);
        if (room == null) return float.MaxValue;

        if (IsRoomRevealed(to))
        {
            switch (room.Type)
            {
                case RoomType.Monster:
                    return baseCost + EvaluateMonsterDanger(room.Monster);
                case RoomType.Trap:
                    return baseCost + 5f;
                case RoomType.Healing:
                    return baseCost - 2f;
                default:
                    return baseCost;
            }
        }

        return baseCost + 2f;
    }

    private float EvaluateMonsterDanger(Monster monster)
    {
        if (monster == null) return 0f;

        int healthDifference = currentHealth - monster.Health;

        if (healthDifference < -30) return 50f;   // avoid
        if (healthDifference > 30) return 0.5f;   // easy
        return 10f;                               // moderate
    }

    private bool IsRoomRevealed(Vector2Int pos)
    {
        if (exploredRooms.Contains(pos)) return true;
        float distance = Vector2Int.Distance(currentPosition, pos);
        return distance <= visionRange;
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2Int> path = new List<Vector2Int> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }

        return path;
    }

    public void MoveToNextRoom()
    {
        if (currentPath == null || currentPath.Count == 0)
        {
            RecalculatePath();
            return;
        }

        Vector2Int nextPos = currentPath[0];
        currentPath.RemoveAt(0);

        RevealRoom(nextPos);

        // move occupancy flag
        SetOccupancy(currentPosition, false);
        currentPosition = nextPos;
        SetOccupancy(currentPosition, true);

        Room nextRoom = dungeonGrid.GetRoom(nextPos.x, nextPos.y);
        if (nextRoom != null)
            HandleRoomInteraction(nextRoom);
    }

    private void RevealRoom(Vector2Int pos)
    {
        Room room = dungeonGrid.GetRoom(pos.x, pos.y);
        if (room != null)
        {
            room.SetRevealed(true);
            exploredRooms.Add(pos);

            foreach (Vector2Int neighbor in GetNeighbors(pos))
            {
                Room nr = dungeonGrid.GetRoom(neighbor.x, neighbor.y);
                if (nr != null)
                {
                    nr.SetRevealed(true);
                    exploredRooms.Add(neighbor);
                }
            }
        }
    }

    private void HandleRoomInteraction(Room room)
    {
        switch (room.Type)
        {
            case RoomType.Monster:
                StartCombat(room.Monster);
                break;
            case RoomType.Trap:
                TriggerTrap(room.Trap);
                break;
            case RoomType.Healing:
                Heal(30);
                break;
            case RoomType.Goal:
                ReachGoal();
                break;
        }
    }

    private void StartCombat(Monster monster)
    {
        if (monster == null) return;

        while (currentHealth > 0 && monster.Health > 0)
        {
            monster.TakeDamage(attackPower);
            if (monster.Health > 0)
                TakeDamage(monster.AttackPower);
        }

        if (currentHealth > 0)
            Debug.Log("Adventurer defeated monster!");
        else
            Debug.Log("Adventurer was defeated!");
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
    }

    private void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
    }

    private void TriggerTrap(Trap trap)
    {
        if (trap != null) trap.Trigger(this);
    }

    private void ReachGoal()
    {
        Debug.Log("Adventurers reached the goal! Player loses!");
    }
}
