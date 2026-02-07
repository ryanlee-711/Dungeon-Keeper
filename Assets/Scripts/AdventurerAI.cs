using UnityEngine;

public class AdventurerAI : MonoBehaviour
{
    public static AdventurerAI Instance { get; private set; }
    
    [SerializeField] private DungeonGrid dungeonGrid;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int attackPower = 20;
    [SerializeField] private int visionRange = 3;
    
    private int currentHealth;
    private Vector2Int currentPosition;
    private HashSet<Vector2Int> exploredRooms = new HashSet<Vector2Int>();
    private List<Vector2Int> currentPath;
    private AIState currentState;
    
    private enum AIState
    {
        Exploring,
        Fighting,
        Fleeing,
        Healing,
        ReachingGoal
    }
    
    void Awake()
    {
        Instance = this;
        currentHealth = maxHealth;
    }
    
    void Start()
    {
        currentPosition = dungeonGrid.StartPosition;
        RecalculatePath();
    }
    
    public void RecalculatePath()
    {
        currentPath = CalculateOptimalPath();
    }
    
    private List<Vector2Int> CalculateOptimalPath()
    {
        // A* pathfinding with danger assessment
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
            {
                return ReconstructPath(cameFrom, current);
            }
            
            closedSet.Add(current);
            
            foreach (Vector2Int neighbor in GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor))
                    continue;
                
                float moveCost = GetMoveCost(current, neighbor);
                float tentativeGScore = gScore[current] + moveCost;
                
                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, goal);
                    
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                    }
                }
            }
        }
        
        return null; // No path found
    }
    
    private float GetMoveCost(Vector2Int from, Vector2Int to)
    {
        float baseCost = 1f;
        Room room = dungeonGrid.GetRoom(to.x, to.y);
        
        if (room == null)
            return float.MaxValue;
        
        // Add danger cost based on room type and revealed status
        if (IsRoomRevealed(to))
        {
            switch (room.Type)
            {
                case RoomType.Monster:
                    return baseCost + EvaluateMonsterDanger(room.Monster);
                case RoomType.Trap:
                    return baseCost + 5f; // Avoid traps
                case RoomType.Healing:
                    return baseCost - 2f; // Prefer healing rooms if injured
                default:
                    return baseCost;
            }
        }
        else
        {
            // Unknown rooms have moderate danger cost
            return baseCost + 2f;
        }
    }
    
    private float EvaluateMonsterDanger(Monster monster)
    {
        if (monster == null)
            return 0f;
        
        // Calculate win probability
        float healthRatio = (float)currentHealth / maxHealth;
        float powerRatio = (float)attackPower / monster.AttackPower;
        float healthDifference = currentHealth - monster.Health;
        
        if (healthDifference < -30)
        {
            // Monster is much stronger - avoid at high cost
            return 50f;
        }
        else if (healthDifference > 30)
        {
            // Easy kill - low cost
            return 0.5f;
        }
        else
        {
            // Moderate fight
            return 10f;
        }
    }
    
    private bool IsRoomRevealed(Vector2Int pos)
    {
        // Check if room is within vision range or has been explored
        if (exploredRooms.Contains(pos))
            return true;
        
        float distance = Vector2Int.Distance(currentPosition, pos);
        return distance <= visionRange;
    }
    
    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); // Manhattan distance
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
        
        // Reveal room
        RevealRoom(nextPos);
        
        // Move to room
        Room nextRoom = dungeonGrid.GetRoom(nextPos.x, nextPos.y);
        currentPosition = nextPos;
        
        // Handle room interaction
        HandleRoomInteraction(nextRoom);
    }
    
    private void RevealRoom(Vector2Int pos)
    {
        Room room = dungeonGrid.GetRoom(pos.x, pos.y);
        if (room != null)
        {
            room.IsRevealed = true;
            exploredRooms.Add(pos);
            
            // Reveal adjacent rooms
            foreach (Vector2Int neighbor in GetNeighbors(pos))
            {
                Room neighborRoom = dungeonGrid.GetRoom(neighbor.x, neighbor.y);
                if (neighborRoom != null)
                {
                    neighborRoom.IsRevealed = true;
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
        // Simple combat resolution
        while (currentHealth > 0 && monster.Health > 0)
        {
            monster.TakeDamage(attackPower);
            if (monster.Health > 0)
            {
                TakeDamage(monster.AttackPower);
            }
        }
        
        if (currentHealth > 0)
        {
            Debug.Log("Adventurer defeated monster!");
        }
        else
        {
            Debug.Log("Adventurer was defeated!");
            // Game over for adventurers
        }
    }
    
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0)
            currentHealth = 0;
    }
    
    private void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
    }
    
    private void TriggerTrap(Trap trap)
    {
        trap.Trigger(this);
    }
    
    private void ReachGoal()
    {
        Debug.Log("Adventurers reached the goal! Player loses!");
        // Trigger game over
    }
}