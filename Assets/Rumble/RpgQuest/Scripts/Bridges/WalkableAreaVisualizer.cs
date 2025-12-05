using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class WalkableAreaVisualizer : MonoBehaviour
{
    [Header("Visual Settings")]
    public Material walkableMaterial;
    public Material nonWalkableMaterial;
    public float checkInterval = 0.5f;
    // Add to WalkableAreaVisualizer script

    private NavMeshPath path;
    private float timer;
    private Texture2D walkableMap;
    private Terrain terrain;
// Add to WalkableAreaVisualizer script
public GameObject edgeMarkerPrefab;
private List<GameObject> edgeMarkers = new List<GameObject>();

void CreateEdgeMarkers()
{
    ClearMarkers();
    
    Vector3 terrainSize = terrain.terrainData.size;
    int markerCount = 20; // Adjust based on terrain size
    
    for (int i = 0; i < markerCount; i++)
    {
        Vector3 pos = new Vector3(
            Random.Range(0, terrainSize.x),
            0,
            Random.Range(0, terrainSize.z)
        );
        
        pos.y = terrain.SampleHeight(pos);
        
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas))
        {
            GameObject marker = Instantiate(edgeMarkerPrefab, pos, Quaternion.identity);
            edgeMarkers.Add(marker);
        }
    }
}

void ClearMarkers()
{
    foreach (var marker in edgeMarkers)
    {
        Destroy(marker);
    }
    edgeMarkers.Clear();
}
    void Start()
    {
        terrain = Terrain.activeTerrain;
        path = new NavMeshPath();
        
        // Create a semi-transparent overlay texture
        CreateWalkableMap();
        
        // Apply to terrain
        UpdateTerrainMaterials();
    }

    void CreateWalkableMap()
    {
        int textureSize = 256; // Lower for performance, higher for precision
        walkableMap = new Texture2D(textureSize, textureSize);

        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 terrainPos = terrain.transform.position;

        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                Vector3 worldPos = new Vector3(
                    terrainPos.x + (x / (float)textureSize) * terrainSize.x,
                    terrainPos.y,
                    terrainPos.z + (y / (float)textureSize) * terrainSize.z
                );

                // Sample height at this position
                worldPos.y = terrain.SampleHeight(worldPos) + terrainPos.y;

                // Check if position is walkable
                NavMeshHit hit;
                bool walkable = NavMesh.SamplePosition(worldPos, out hit, 1.0f, NavMesh.AllAreas);

                // Color the pixel (green for walkable, red for non-walkable)
                walkableMap.SetPixel(x, y, walkable ? Color.green : Color.red);
            }
        }

        walkableMap.Apply();
       walkableMap.filterMode = FilterMode.Point;
walkableMap.wrapMode = TextureWrapMode.Clamp; 
    }

     

void UpdateTerrainMaterials()
{
    if (terrain != null)
    {
        var terrainMaterial = new Material(Shader.Find("Custom/KidFriendlyTerrain"));
        terrainMaterial.CopyPropertiesFromMaterial(terrain.materialTemplate);
        terrainMaterial.SetTexture("_WalkableMap", walkableMap);
        terrain.materialTemplate = terrainMaterial;
    }
}




    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            UpdateWalkableVisuals();
            timer = 0;
        }
    }

    void UpdateWalkableVisuals()
    {
        // This can be expanded to show path to current objective
    }
}