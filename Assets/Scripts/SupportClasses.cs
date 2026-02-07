using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Simple priority queue for A*
public class PriorityQueue<T>
{
    private readonly List<(T item, float priority)> elements = new List<(T, float)>();

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
        return elements.Any(e => EqualityComparer<T>.Default.Equals(e.item, item));
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
        if (Health < 0) Health = 0;
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
            // TODO: implement Slow / Poison later
        }
    }
}
