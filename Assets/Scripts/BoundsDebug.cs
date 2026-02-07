using UnityEngine;

public class BoundsPrint : MonoBehaviour
{
    void Start()
    {
        var rends = GetComponentsInChildren<Renderer>();
        Bounds b = new Bounds(transform.position, Vector3.zero);
        foreach (var r in rends) b.Encapsulate(r.bounds);
        Debug.Log($"ROOM WORLD SIZE: {b.size}");
    }
}