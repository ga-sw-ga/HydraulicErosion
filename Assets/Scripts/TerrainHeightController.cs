using System;
using UnityEditor;
using UnityEngine;

public class TerrainHeightController : MonoBehaviour
{
    private Terrain _terrain;
    private TerrainData _terrainData;
    private int _heightmapWidth, _heightmapHeight;
    
    private float[,] _initialHeights;
    
    private void Awake()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        
        _terrain = GetComponent<Terrain>();
        _terrainData = _terrain.terrainData;
        _heightmapWidth = _terrainData.heightmapResolution;
        _heightmapHeight = _terrainData.heightmapResolution;
    }

    void Start()
    {
        SaveHeightmap();
        
        //AdjustTerrainHeight(targetHeight);
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // If the game exits play mode
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            LoadHeightmap();
        }
    }
    
    private Vector2Int WorldToHeightmapCoords(Vector3 worldPosition)
    {
        // Get terrain data and size
        TerrainData terrainData = _terrain.terrainData;
        Vector3 terrainPosition = _terrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        // Calculate relative position within the terrain (normalized to [0, 1] range)
        float relativeX = (worldPosition.x - terrainPosition.x) / terrainSize.x;
        float relativeZ = (worldPosition.z - terrainPosition.z) / terrainSize.z;

        // Clamp relative position to be within the heightmap bounds
        relativeX = Mathf.Clamp01(relativeX);
        relativeZ = Mathf.Clamp01(relativeZ);

        // Convert normalized position to heightmap indices
        int heightmapX = Mathf.FloorToInt(relativeX * (_terrainData.heightmapResolution - 1));
        int heightmapZ = Mathf.FloorToInt(relativeZ * (_terrainData.heightmapResolution - 1));

        return new Vector2Int(heightmapX, heightmapZ);
    }
    
    private Vector3 HeightmapToWorldCoords(Vector2Int heightmapCoords)
    {
        Vector3 terrainPosition = _terrain.transform.position;
        Vector3 terrainSize = _terrainData.size;

        // Get the relative position on the heightmap (0 to 1)
        float relativeX = (float)heightmapCoords.x / (_terrainData.heightmapResolution - 1);
        float relativeZ = (float)heightmapCoords.y / (_terrainData.heightmapResolution - 1);

        // Convert the relative position to world position
        float worldX = terrainPosition.x + relativeX * terrainSize.x;
        float worldZ = terrainPosition.z + relativeZ * terrainSize.z;

        // Get the height at the given heightmap coordinates
        float height = _terrainData.GetHeight(heightmapCoords.x, heightmapCoords.y);

        // Calculate the world position with the height
        Vector3 worldPosition = new Vector3(worldX, terrainPosition.y + height, worldZ);

        return worldPosition;
    }

    private float GetHeight(Vector3 worldPosition)
    {
        Vector2Int heightmapCoords = WorldToHeightmapCoords(worldPosition);
        return _terrainData.GetHeight(heightmapCoords.x, heightmapCoords.y);
    }
    
    private void SetTerrainHeight(Vector2Int heightmapCoords, float height)
    {
        // Get the current heights at the heightmap position
        float[,] heights = _terrainData.GetHeights(heightmapCoords.x, heightmapCoords.y, 1, 1);

        // Set the new height (normalized between 0 and 1)
        heights[0, 0] = height;

        // Apply the modified heights back to the terrain
        _terrainData.SetHeights(heightmapCoords.x, heightmapCoords.y, heights);
    }
    
    private void AddTerrainHeight(Vector2Int heightmapCoords, float addedHeight)
    {
        float[,] heights = _terrainData.GetHeights(heightmapCoords.x, heightmapCoords.y, 1, 1);
        heights[0, 0] += addedHeight;
        _terrainData.SetHeights(heightmapCoords.x, heightmapCoords.y, heights);
    }
    
    private void AddTerrainHeight(Vector3 worldPosition, float addedHeight)
    {
        AddTerrainHeight(WorldToHeightmapCoords(worldPosition), addedHeight);
    }

    private void SaveHeightmap()
    {
        _initialHeights = _terrainData.GetHeights(0, 0, _heightmapWidth, _heightmapHeight);
    }

    private void LoadHeightmap()
    {
        _terrainData.SetHeights(0, 0, _initialHeights);
    }
}