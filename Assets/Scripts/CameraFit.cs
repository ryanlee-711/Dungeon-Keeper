using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAutoFit : MonoBehaviour
{
    [SerializeField] private DungeonGrid dungeonGrid;
    [SerializeField] private float padding = 1f;

    private void Start()
    {
        Fit();
    }

    public void Fit()
    {
        Camera cam = GetComponent<Camera>();
        if (!cam.orthographic || dungeonGrid == null) return;

        float worldWidth  = dungeonGrid.Width  * dungeonGrid.CellSize;
        float worldHeight = dungeonGrid.Height * dungeonGrid.CellSize;

        float sizeBasedOnHeight = worldHeight * 0.5f;
        float sizeBasedOnWidth  = (worldWidth * 0.5f) / cam.aspect;

        cam.orthographicSize = Mathf.Max(sizeBasedOnHeight, sizeBasedOnWidth) + padding;

        // center camera
        transform.position = new Vector3(worldWidth/2, worldHeight/2, transform.position.z);
    }
}
