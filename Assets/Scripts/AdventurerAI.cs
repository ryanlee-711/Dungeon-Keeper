using UnityEngine;
using System.Collections.Generic;

public class AdventurerAI : MonoBehaviour
{
    public static AdventurerAI Instance { get; private set; }

    [SerializeField] private DungeonGrid dungeonGrid;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int attackPower = 20;
    [SerializeField] private int visionRange = 3;

    [Header("D* Lite Settings")]
    [SerializeField] private float playerThreatWeight = 5f;
    [SerializeField] private float unexploredRoomCost = 3f;

    [Header("AI Difficulty & Exploitability")]
    [Tooltip("How often AI recalculates path (lower = more exploitable). 0 = never, 1 = always")]
    [SerializeField][Range(0f, 1f)] private float replanningChance = 0.7f;

    [Tooltip("How many turns AI commits to current path before reconsidering")]
    [SerializeField] private int pathCommitmentTurns = 3;

    [Tooltip("Health threshold where AI gets desperate and rushes goal")]
    [SerializeField] private int desperationHealthThreshold = 30;

    [Tooltip("How much AI overestimates/underestimates monster danger")]
    [SerializeField][Range(-20f, 20f)] private float monsterDangerBias = 0f;

    [Tooltip("Chance AI makes a 'greedy' choice toward goal instead of safe path")]
    [SerializeField][Range(0f, 0.5f)] private float greediness = 0.15f;

    [Tooltip("AI won't see traps until this close")]
    [SerializeField] private int trapDetectionRange = 2;

    private int currentHealth;
    private Vector2Int currentPosition;
    private Vector2Int lastPosition;
    private readonly HashSet<Vector2Int> exploredRooms = new HashSet<Vector2Int>();

    // D* Lite data structures
    private Dictionary<Vector2Int, float> g = new Dictionary<Vector2Int, float>();
    private Dictionary<Vector2Int, float> rhs = new Dictionary<Vector2Int, float>();
    private DStarPriorityQueue openSet = new DStarPriorityQueue();
    private float km = 0;

    // Player threat modeling
    private Dictionary<Vector2Int, float> playerThreatMap = new Dictionary<Vector2Int, float>();

    // AI personality/state
    private int turnsSinceLastReplan = 0;
    private bool isDesperateMode = false;
    private Vector2Int? committedTarget = null; // Room AI is "locked onto"
    private bool justFoughtCombat = false; // For pause after combat

    public Vector2Int GridPosition => currentPosition;
    public bool IsInDesperateMode => isDesperateMode;
    public bool JustFoughtCombat => justFoughtCombat;

    private bool hasReachedGoal = false;

    public bool HasReachedGoal => hasReachedGoal;

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
        lastPosition = currentPosition;
        SetOccupancy(currentPosition, true);

        InitializeDStarLite();
        ComputeShortestPath();
    }

    private void SetOccupancy(Vector2Int pos, bool occupied)
    {
        Room r = dungeonGrid.GetRoom(pos.x, pos.y);
        if (r != null) r.SetOccupiedByAdventurers(occupied);
    }

    #region D* Lite Core Algorithm

    private void InitializeDStarLite()
    {
        g.Clear();
        rhs.Clear();
        openSet.Clear();
        km = 0;

        Vector2Int goal = dungeonGrid.GoalPosition;

        for (int x = 0; x < dungeonGrid.Width; x++)
        {
            for (int y = 0; y < dungeonGrid.Height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                g[pos] = float.MaxValue;
                rhs[pos] = float.MaxValue;
            }
        }

        rhs[goal] = 0;
        openSet.Enqueue(goal, CalculateKey(goal));
    }

    private Vector2 CalculateKey(Vector2Int pos)
    {
        float minG = Mathf.Min(g[pos], rhs[pos]);
        float h = Heuristic(currentPosition, pos);

        return new Vector2(
            minG + h + km,
            minG
        );
    }

    private void ComputeShortestPath()
    {
        int iterations = 0;
        int maxIterations = dungeonGrid.Width * dungeonGrid.Height * 2;

        while (openSet.Count > 0 && iterations < maxIterations)
        {
            Vector2 kOld = openSet.PeekKey();
            Vector2Int u = openSet.Dequeue();

            Vector2 kNew = CalculateKey(u);

            if (CompareKeys(CalculateKey(currentPosition), kOld) &&
                rhs[currentPosition] == g[currentPosition])
            {
                break;
            }

            if (CompareKeys(kOld, kNew))
            {
                openSet.Enqueue(u, kNew);
            }
            else if (g[u] > rhs[u])
            {
                g[u] = rhs[u];
                foreach (Vector2Int s in GetNeighbors(u))
                {
                    UpdateVertex(s);
                }
            }
            else
            {
                g[u] = float.MaxValue;
                UpdateVertex(u);
                foreach (Vector2Int s in GetNeighbors(u))
                {
                    UpdateVertex(s);
                }
            }

            iterations++;
        }

        if (iterations >= maxIterations)
        {
            Debug.LogWarning("D* Lite: Max iterations reached");
        }
    }

    private void UpdateVertex(Vector2Int pos)
    {
        Vector2Int goal = dungeonGrid.GoalPosition;

        if (pos != goal)
        {
            float minCost = float.MaxValue;

            foreach (Vector2Int neighbor in GetNeighbors(pos))
            {
                float moveCost = GetMoveCost(pos, neighbor);
                if (moveCost < float.MaxValue / 2f)
                {
                    float cost = moveCost + g[neighbor];
                    minCost = Mathf.Min(minCost, cost);
                }
            }

            rhs[pos] = minCost;
        }

        openSet.Remove(pos);

        if (!Mathf.Approximately(g[pos], rhs[pos]))
        {
            openSet.Enqueue(pos, CalculateKey(pos));
        }
    }

    private bool CompareKeys(Vector2 a, Vector2 b)
    {
        if (a.x < b.x - 0.0001f) return true;
        if (a.x > b.x + 0.0001f) return false;
        return a.y < b.y - 0.0001f;
    }

    #endregion

    #region Movement and Replanning

    public void MoveToNextRoom()
    {
        if (hasReachedGoal) return;

        // If we are already on the goal, stop here (and only fire once)
        if (currentPosition == dungeonGrid.GoalPosition)
        {
            hasReachedGoal = true;
            ReachGoal();
            return;
        }

        turnsSinceLastReplan++;
        justFoughtCombat = false;

        UpdateAIState();

        Vector2Int nextMove = ChooseNextMove();

        if (nextMove == currentPosition)
        {
            Debug.LogWarning("Adventurer is stuck - no valid path!");
            return;
        }

        // Reveal before moving (fine)
        RevealRoom(nextMove);

        // Move into the next cell
        SetOccupancy(currentPosition, false);
        lastPosition = currentPosition;
        currentPosition = nextMove;
        SetOccupancy(currentPosition, true);

        // If we stepped onto the goal, stop ON it and end immediately
        if (currentPosition == dungeonGrid.GoalPosition)
        {
            hasReachedGoal = true;
            ReachGoal();
            return;
        }

        Room nextRoom = dungeonGrid.GetRoom(nextMove.x, nextMove.y);
        if (nextRoom != null)
            HandleRoomInteraction(nextRoom);
    }


    private void UpdateAIState()
    {
        // Check if AI should enter desperate mode
        if (currentHealth <= desperationHealthThreshold && !isDesperateMode)
        {
            isDesperateMode = true;
            Debug.Log("Adventurer is desperate! Rushing to goal!");
        }
    }

    private Vector2Int ChooseNextMove()
    {
        // Desperate AI ignores danger and rushes goal
        if (isDesperateMode)
        {
            return ChooseDesperateMove();
        }

        // AI with committed target (exploitable - won't reconsider for a few turns)
        if (committedTarget.HasValue && turnsSinceLastReplan < pathCommitmentTurns)
        {
            return ChooseCommittedMove();
        }

        // Greedy AI sometimes takes risky shortcuts
        if (Random.value < greediness)
        {
            return ChooseGreedyMove();
        }

        // Normal D* Lite pathfinding
        return ChooseOptimalMove();
    }

    private Vector2Int ChooseOptimalMove()
    {
        Vector2Int bestNext = currentPosition;
        float bestCost = float.MaxValue;

        foreach (Vector2Int neighbor in GetNeighbors(currentPosition))
        {
            float moveCost = GetMoveCost(currentPosition, neighbor);
            if (moveCost >= float.MaxValue / 2f) continue;

            float totalCost = moveCost + g[neighbor];

            if (playerThreatMap.ContainsKey(neighbor))
            {
                totalCost += playerThreatMap[neighbor] * playerThreatWeight;
            }

            if (totalCost < bestCost)
            {
                bestCost = totalCost;
                bestNext = neighbor;
            }
        }

        return bestNext;
    }

    private Vector2Int ChooseGreedyMove()
    {
        // Pick move closest to goal (ignoring some dangers)
        Vector2Int bestNext = currentPosition;
        float bestDistance = float.MaxValue;

        foreach (Vector2Int neighbor in GetNeighbors(currentPosition))
        {
            float moveCost = GetMoveCost(currentPosition, neighbor);

            // Only avoid instant death
            if (moveCost >= 100f) continue;

            float distToGoal = Heuristic(neighbor, dungeonGrid.GoalPosition);

            if (distToGoal < bestDistance)
            {
                bestDistance = distToGoal;
                bestNext = neighbor;
            }
        }

        Debug.Log("Adventurer is being greedy!");
        return bestNext;
    }

    private Vector2Int ChooseDesperateMove()
    {
        // Move directly toward goal, only avoiding instant death
        Vector2Int bestNext = currentPosition;
        float bestDistance = float.MaxValue;

        foreach (Vector2Int neighbor in GetNeighbors(currentPosition))
        {
            Room room = dungeonGrid.GetRoom(neighbor.x, neighbor.y);
            if (room == null) continue;

            // Only check for instant death
            bool isInstantDeath = false;
            if (IsRoomRevealed(neighbor))
            {
                if (room.Type == RoomType.Monster && room.Monster != null)
                {
                    if (SimulateCombat(room.Monster) <= 0)
                        isInstantDeath = true;
                }
                else if (room.Type == RoomType.Trap && room.Trap != null)
                {
                    if (currentHealth - room.Trap.Damage <= 0)
                        isInstantDeath = true;
                }
            }

            if (isInstantDeath) continue;

            float distToGoal = Heuristic(neighbor, dungeonGrid.GoalPosition);
            if (distToGoal < bestDistance)
            {
                bestDistance = distToGoal;
                bestNext = neighbor;
            }
        }

        return bestNext;
    }

    private Vector2Int ChooseCommittedMove()
    {
        // AI is "locked on" to a target - won't reconsider unless blocked
        if (!committedTarget.HasValue)
            return ChooseOptimalMove();

        // Move toward committed target
        Vector2Int target = committedTarget.Value;
        Vector2Int bestNext = currentPosition;
        float bestDistance = float.MaxValue;

        foreach (Vector2Int neighbor in GetNeighbors(currentPosition))
        {
            float moveCost = GetMoveCost(currentPosition, neighbor);
            if (moveCost >= float.MaxValue / 2f) continue;

            float distToTarget = Heuristic(neighbor, target);
            if (distToTarget < bestDistance)
            {
                bestDistance = distToTarget;
                bestNext = neighbor;
            }
        }

        // If reached target, clear commitment
        if (currentPosition == target)
        {
            committedTarget = null;
            turnsSinceLastReplan = pathCommitmentTurns; // Force replan
        }

        return bestNext;
    }

    public void OnRoomChanged(Vector2Int changedPos)
    {
        // AI doesn't always notice changes (exploitable!)
        if (Random.value > replanningChance)
        {
            Debug.Log("Adventurer didn't notice the room change!");
            return;
        }

        // Clear commitment if player changes something
        committedTarget = null;
        turnsSinceLastReplan = 0;

        km += Heuristic(lastPosition, currentPosition);
        lastPosition = currentPosition;

        UpdateVertex(changedPos);

        foreach (Vector2Int neighbor in GetNeighbors(changedPos))
        {
            UpdateVertex(neighbor);
        }

        UpdatePlayerThreatMap(changedPos);
        ComputeShortestPath();

        Debug.Log("Adventurer noticed room change and replanned!");
    }

    public void RecalculatePath()
    {
        km += Heuristic(lastPosition, currentPosition);
        lastPosition = currentPosition;

        ComputeShortestPath();
    }

    #endregion

    #region Player Threat Modeling

    private void UpdatePlayerThreatMap(Vector2Int changedPos)
    {
        Room room = dungeonGrid.GetRoom(changedPos.x, changedPos.y);
        if (room == null) return;

        if (room.Type == RoomType.Monster || room.Type == RoomType.Trap)
        {
            float threatLevel = CalculatePositionalThreat(changedPos);
            playerThreatMap[changedPos] = threatLevel;

            foreach (Vector2Int neighbor in GetNeighbors(changedPos))
            {
                if (!playerThreatMap.ContainsKey(neighbor))
                    playerThreatMap[neighbor] = 0;

                playerThreatMap[neighbor] += threatLevel * 0.3f;
            }
        }
        else
        {
            if (playerThreatMap.ContainsKey(changedPos))
                playerThreatMap[changedPos] *= 0.5f;
        }
    }

    private float CalculatePositionalThreat(Vector2Int pos)
    {
        Vector2Int goal = dungeonGrid.GoalPosition;

        float totalDist = Vector2Int.Distance(currentPosition, goal);
        float distToPos = Vector2Int.Distance(currentPosition, pos);
        float distPosToGoal = Vector2Int.Distance(pos, goal);

        float deviation = Mathf.Abs(distToPos + distPosToGoal - totalDist);

        return Mathf.Max(0, 10f - deviation);
    }

    #endregion

    #region Cost Evaluation

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
                    // EXPLOITABLE: AI doesn't see traps until close!
                    float distToTrap = Vector2Int.Distance(currentPosition, to);
                    if (distToTrap > trapDetectionRange)
                    {
                        return baseCost; // Doesn't see the trap yet
                    }
                    return baseCost + EvaluateTrapDanger(room.Trap);

                case RoomType.Healing:
                    float healthDeficit = maxHealth - currentHealth;
                    float healingValue = healthDeficit / 10f;

                    // AI overvalues healing when desperate
                    if (isDesperateMode)
                        healingValue *= 2f;

                    // Create "bait" - AI attracted to healing
                    committedTarget = to;

                    return Mathf.Max(0.1f, baseCost - healingValue);

                case RoomType.Empty:
                case RoomType.Treasure:
                case RoomType.Start:
                case RoomType.Goal:
                    return baseCost;

                default:
                    return baseCost;
            }
        }

        return baseCost + unexploredRoomCost;
    }

    private float EvaluateMonsterDanger(Monster monster)
    {
        if (monster == null) return 0f;

        int resultingHealth = SimulateCombat(monster);

        // Apply bias (makes AI over/underestimate danger)
        float danger = 0f;

        if (resultingHealth <= 0) danger = 200f;
        else if (resultingHealth < 20) danger = 50f;
        else if (resultingHealth < 50) danger = 15f;
        else danger = 3f;

        return danger + monsterDangerBias;
    }

    private int SimulateCombat(Monster monster)
    {
        int adventurerHP = currentHealth;
        int monsterHP = monster.Health;

        while (adventurerHP > 0 && monsterHP > 0)
        {
            monsterHP -= attackPower;
            if (monsterHP > 0)
                adventurerHP -= monster.AttackPower;
        }

        return adventurerHP;
    }

    private float EvaluateTrapDanger(Trap trap)
    {
        if (trap == null) return 0f;

        int potentialDamage = trap.Damage;

        if (currentHealth - potentialDamage <= 0) return 100f;
        if (currentHealth - potentialDamage < 20) return 30f;
        return 10f;
    }

    #endregion

    #region Helper Methods

    private List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        Vector2Int[] dirs = {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

        List<Vector2Int> result = new List<Vector2Int>(4);
        foreach (var d in dirs)
        {
            Vector2Int neighbor = pos + d;
            if (dungeonGrid.IsValidPosition(neighbor.x, neighbor.y))
                result.Add(neighbor);
        }

        return result;
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

    #endregion

    #region Room Interaction

    private void RevealRoom(Vector2Int pos)
    {
        Room room = dungeonGrid.GetRoom(pos.x, pos.y);
        if (room != null)
        {
            room.SetRevealed(true);
            exploredRooms.Add(pos);

            foreach (Vector2Int neighbor in GetNeighbors(pos))
            {
                Room neighborRoom = dungeonGrid.GetRoom(neighbor.x, neighbor.y);
                if (neighborRoom != null)
                {
                    neighborRoom.SetRevealed(true);
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
                Monster targetMonster = room.Monster;
                Animator monsterAnim = targetMonster.gameObject.GetComponent<Animator>();
                monsterAnim.SetBool("is_fighting", true);
                monsterAnim.SetBool("is_fighting", false);
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

        justFoughtCombat = true; // Set flag for pause
        Debug.Log($"Combat started with {monster.Name}!");

        while (currentHealth > 0 && monster.Health > 0)
        {
            monster.TakeDamage(attackPower);
            if (monster.Health > 0)
                TakeDamage(monster.AttackPower);
        }

        if (currentHealth > 0)
        {
            Debug.Log($"Adventurer defeated {monster.Name}!");
        }
        else
        {
            Debug.Log("Adventurer was defeated! Player wins!");

            // Notify game manager of player victory
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerWins();
            }
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        Debug.Log($"Adventurer took {damage} damage. Health: {currentHealth}/{maxHealth}");

        // Notify game manager of health change
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnAdventurerTookDamage(currentHealth);
        }
    }

    private void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        Debug.Log($"Adventurer healed {amount}. Health: {currentHealth}/{maxHealth}");
    }

    private void TriggerTrap(Trap trap)
    {
        if (trap != null)
        {
            Debug.Log($"Triggered {trap.Type} trap!");
            trap.Trigger(this);
        }
    }

    private void ReachGoal()
    {
        Debug.Log("Adventurers reached the goal! Player loses!");

        // Notify game manager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnAdventurerReachedGoal();
        }
    }

    #endregion
}

#region D* Lite Priority Queue

public class DStarPriorityQueue
{
    private List<(Vector2Int pos, Vector2 key)> elements = new List<(Vector2Int, Vector2)>();

    public int Count => elements.Count;

    public void Enqueue(Vector2Int pos, Vector2 key)
    {
        Remove(pos);

        elements.Add((pos, key));

        elements.Sort((a, b) =>
        {
            if (a.key.x < b.key.x - 0.0001f) return -1;
            if (a.key.x > b.key.x + 0.0001f) return 1;
            if (a.key.y < b.key.y - 0.0001f) return -1;
            if (a.key.y > b.key.y + 0.0001f) return 1;
            return 0;
        });
    }

    public Vector2Int Dequeue()
    {
        if (elements.Count == 0)
            throw new System.InvalidOperationException("Queue is empty");

        Vector2Int pos = elements[0].pos;
        elements.RemoveAt(0);
        return pos;
    }

    public Vector2 PeekKey()
    {
        if (elements.Count == 0)
            return new Vector2(float.MaxValue, float.MaxValue);

        return elements[0].key;
    }

    public void Remove(Vector2Int pos)
    {
        elements.RemoveAll(e => e.pos == pos);
    }

    public bool Contains(Vector2Int pos)
    {
        return elements.Exists(e => e.pos == pos);
    }

    public void Clear()
    {
        elements.Clear();
    }
}

#endregion