using UnityEngine;

// Simple priority queue for A*
public class PriorityQueue<T>
{
    private List<(T item, float priority)> elements = new List<(T, float)>();
    
    public int Count => elements.Count;
    
    public void Enqueue(T item, float priority)
    {
        elements.Add((item, priority));
        elements.Sort((a, b) => a.priority.CompareTo(b.priority));
    }
    
    public T Dequeue()
    {
        T item = elements[0].item;
        elements.RemoveAt(0);
        return item;
    }
    
    public bool Contains(T item)
    {
        return elements.Any(e => e.item.Equals(item));
    }
}

// Monster data
[System.Serializable]
public class Monster
{
    public string Name;
    public int Health;
    public int AttackPower;
    public Sprite Sprite;
    
    public void TakeDamage(int damage)
    {
        Health -= damage;
    }
}

// Trap data
[System.Serializable]
public class Trap
{
    public enum TrapType
    {
        Damage,
        Slow,
        Poison
    }
    
    public TrapType Type;
    public int Damage;
    
    public void Trigger(AdventurerAI adventurer)
    {
        switch (Type)
        {
            case TrapType.Damage:
                adventurer.TakeDamage(Damage);
                break;
            // Add other trap effects
        }
    }
}

