using UnityEngine;

public class DungeonBackground : MonoBehaviour
{
    [SerializeField] private DungeonGrid grid;
    [SerializeField] private float padding = 0.5f;

    private void Start()
    {
        Resize();
    }

    public void Resize()
    {
        if (grid == null)
        {
            Debug.LogError("DungeonBackground: Grid not assigned.");
            return;
        }

        float worldWidth  = grid.Width  * grid.CellSize + padding * 2f;
        float worldHeight = grid.Height * grid.CellSize + padding * 2f;

        transform.localScale = new Vector3(worldWidth, worldHeight, 1f);
        transform.position  = Vector3.zero; // grid is centered at origin
    }
}
