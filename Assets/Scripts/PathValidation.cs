using UnityEngine;

public class PathValidator : MonoBehaviour
{
    private DungeonGrid dungeonGrid;
    
    public bool ValidateSwap(Vector2Int pos1, Vector2Int pos2)
    {
        // Temporarily perform swap
        dungeonGrid.SwapRooms(pos1, pos2);
        
        bool isValid = true;
        
        // Check if path from start to goal exists
        if (!PathExists(dungeonGrid.StartPosition, dungeonGrid.GoalPosition))
        {
            isValid = false;
        }
        
        // Check if path from adventurers to start/goal exists
        if (isValid)
        {
            Vector2Int adventurerPos = AdventurerController.Instance.GridPosition;
            if (!PathExists(adventurerPos, dungeonGrid.GoalPosition))
            {
                isValid = false;
            }
        }
        
        // Swap back
        dungeonGrid.SwapRooms(pos1, pos2);
        
        return isValid;
    }
    
    private bool PathExists(Vector2Int start, Vector2Int goal)
    {
        // BFS to check if path exists
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        
        queue.Enqueue(start);
        visited.Add(start);
        
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            
            if (current == goal)
                return true;
            
            foreach (Vector2Int neighbor in GetNeighbors(current))
            {
                if (!visited.Contains(neighbor) && IsWalkable(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        
        return false;
    }
    
    private List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] directions = {
            new Vector2Int(0, 1),  // Up
            new Vector2Int(1, 0),  // Right
            new Vector2Int(0, -1), // Down
            new Vector2Int(-1, 0)  // Left
        };
        
        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighbor = pos + dir;
            if (dungeonGrid.IsValidPosition(neighbor.x, neighbor.y))
            {
                neighbors.Add(neighbor);
            }
        }
        
        return neighbors;
    }
    
    private bool IsWalkable(Vector2Int pos)
    {
        Room room = dungeonGrid.GetRoom(pos.x, pos.y);
        return room != null; // All rooms are walkable (monsters don't block path)
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

