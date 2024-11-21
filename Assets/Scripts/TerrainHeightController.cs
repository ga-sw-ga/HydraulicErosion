using System;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class TerrainHeightController : MonoBehaviour
{
    [HideInInspector] public Terrain _terrain;
    [HideInInspector] public TerrainData _terrainData;
    [HideInInspector] public int _heightmapWidth, _heightmapHeight;

    public GameObject waterDropletGameObject;
    
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

        //Debug.Log("Terrain Size: " + GetTerrainSize());
        //Debug.Log("Terrain World Pos Index 0, 0: " + HeightmapToWorldCoords(new Vector2Int(0, 0)));
        //Debug.Log("Terrain World Pos Index 0, MAX: " + HeightmapToWorldCoords(new Vector2Int(0, _heightmapHeight)));
        //Debug.Log("Terrain World Pos Index MAX, 0: " + HeightmapToWorldCoords(new Vector2Int(_heightmapWidth, 0)));
        //Debug.Log("Terrain World Pos Index MAX, MAX: " + HeightmapToWorldCoords(new Vector2Int(_heightmapWidth, _heightmapHeight)));
        //Instantiate(waterDropletGameObject, new Vector3(12, 15, 33), Quaternion.identity);
        //AdjustTerrainHeight(targetHeight);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Instantiate(waterDropletGameObject, new Vector3(Random.Range(0f, 50f), 0f, Random.Range(0f, 50f)), Quaternion.identity);
        }
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // If the game exits play mode
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            LoadHeightmap();
        }
    }
    
    public Vector2Int WorldToHeightmapCoords(Vector3 worldPosition)
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
    
    public Vector3 HeightmapToWorldCoords(Vector2Int heightmapCoords)
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
    
    public void DepositSediment(Vector3 worldPosition, float amount, float radius)
    {
        Vector2Int centerCoords = WorldToHeightmapCoords(worldPosition);
        int depositRadius = Mathf.CeilToInt(radius);
        int minX = Mathf.Max(0, centerCoords.x - depositRadius);
        int maxX = Mathf.Min(_heightmapWidth - 1, centerCoords.x + depositRadius);
        int minY = Mathf.Max(0, centerCoords.y - depositRadius);
        int maxY = Mathf.Min(_heightmapHeight - 1, centerCoords.y + depositRadius);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                float distance = Vector2.Distance(new Vector2(centerCoords.x, centerCoords.y), new Vector2(x, y));
                if (distance < depositRadius)
                {
                    float weight = Mathf.Max(0, 1 - distance / depositRadius); // Weight based on distance
                    float adjustedAmount = amount * weight;

                    AddTerrainHeight(new Vector2Int(x, y), adjustedAmount);
                }
            }
        }
    }

    public void ErodeTerrain(Vector3 worldPosition, float amount, float radius)
    {
        Vector2Int centerCoords = WorldToHeightmapCoords(worldPosition);
        int erosionRadius = Mathf.CeilToInt(radius);
        int minX = Mathf.Max(0, centerCoords.x - erosionRadius);
        int maxX = Mathf.Min(_heightmapWidth - 1, centerCoords.x + erosionRadius);
        int minY = Mathf.Max(0, centerCoords.y - erosionRadius);
        int maxY = Mathf.Min(_heightmapHeight - 1, centerCoords.y + erosionRadius);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                float distance = Vector2.Distance(new Vector2(centerCoords.x, centerCoords.y), new Vector2(x, y));
                if (distance < erosionRadius)
                {
                    float weight = Mathf.Max(0, 1 - distance / erosionRadius); // Weight based on distance
                    float adjustedAmount = amount * weight;

                    AddTerrainHeight(new Vector2Int(x, y), -adjustedAmount);
                }
            }
        }
    }
    
    public void AddSediment(Vector3 worldPosition, float amount)
    {
        Vector3 terrainPosition = GetTerrainPosition();
        Vector3 terrainSize = GetTerrainSize();
        
        float relativeX = (worldPosition.x - terrainPosition.x) / terrainSize.x * (_heightmapWidth - 1);
        float relativeZ = (worldPosition.z - terrainPosition.z) / terrainSize.z * (_heightmapHeight - 1);
        
        int baseX = Mathf.Clamp(Mathf.FloorToInt(relativeX), 0, _heightmapWidth - 2);
        int baseZ = Mathf.Clamp(Mathf.FloorToInt(relativeZ), 0, _heightmapHeight - 2);
        
        float fracX = relativeX - baseX;
        float fracZ = relativeZ - baseZ;
        
        AddTerrainHeight(new Vector2Int(baseX, baseZ), amount * (1f - fracX) * (1f - fracZ));
        AddTerrainHeight(new Vector2Int(baseX, baseZ + 1), amount * (1f - fracX) * fracZ);
        AddTerrainHeight(new Vector2Int(baseX + 1, baseZ), amount * fracX * (1f - fracZ));
        AddTerrainHeight(new Vector2Int(baseX + 1, baseZ + 1), amount * fracX * fracZ);
    }
    
    public Vector2 CalculateCellOffset(Vector3 worldPosition)
    {
        Vector3 terrainPosition = GetTerrainPosition();
        Vector3 terrainSize = GetTerrainSize();

        // Calculate normalized position on the heightmap
        float relativeX = (worldPosition.x - terrainPosition.x) / terrainSize.x * (_heightmapWidth - 1);
        float relativeZ = (worldPosition.z - terrainPosition.z) / terrainSize.z * (_heightmapHeight - 1);

        // Get the fractional part to determine offset within the cell
        float xOffset = relativeX - Mathf.Floor(relativeX);
        float zOffset = relativeZ - Mathf.Floor(relativeZ);

        return new Vector2(xOffset, zOffset);
    }
    
    public Vector2 CalculateGradient(Vector3 worldPosition)
    {
        Vector3 terrainPosition = GetTerrainPosition();
        Vector3 terrainSize = GetTerrainSize();

        // Get normalized position within the heightmap
        float relativeX = (worldPosition.x - terrainPosition.x) / terrainSize.x * (_heightmapWidth - 1);
        float relativeZ = (worldPosition.z - terrainPosition.z) / terrainSize.z * (_heightmapHeight - 1);

        int baseX = Mathf.Clamp(Mathf.FloorToInt(relativeX), 0, _heightmapWidth - 2);
        int baseZ = Mathf.Clamp(Mathf.FloorToInt(relativeZ), 0, _heightmapHeight - 2);

        // Fetch surrounding heights
        float h00 = _terrainData.GetHeight(baseX, baseZ);
        float h10 = _terrainData.GetHeight(baseX + 1, baseZ);
        float h01 = _terrainData.GetHeight(baseX, baseZ + 1);
        float h11 = _terrainData.GetHeight(baseX + 1, baseZ + 1);
        
        // Compute the fractional offsets
        float fracX = relativeX - baseX;
        float fracZ = relativeZ - baseZ;

        // Calculate gradients in the X and Z directions
        float gradientX = (h10 - h00) * (1 - fracZ) + 
                           (h11 - h01) * fracZ;

        float gradientZ = (h01 - h00) * (1 - fracX) + 
                           (h11 - h10) * fracX;

        // Return the gradient vector in world coordinates
        return new Vector2(gradientX, gradientZ).normalized;
    }

    public Vector3 GetTerrainSize()
    {
        return _terrainData.size;
    }
    
    public Vector3 GetTerrainPosition()
    {
        return _terrain.transform.position;
    }

    public float GetHeight(Vector3 worldPosition)
    {
        Vector2Int heightmapCoords = WorldToHeightmapCoords(worldPosition);
        Vector3 terrainPosition = GetTerrainPosition();
        Vector3 terrainSize = GetTerrainSize();

        // Get normalized position within the cell
        float relativeX = (worldPosition.x - terrainPosition.x) / terrainSize.x * (_heightmapWidth - 1);
        float relativeZ = (worldPosition.z - terrainPosition.z) / terrainSize.z * (_heightmapHeight - 1);

        float fracX = relativeX - Mathf.Floor(relativeX);
        float fracZ = relativeZ - Mathf.Floor(relativeZ);

        // Fetch the four surrounding heights
        float h00 = _terrainData.GetHeight(heightmapCoords.x, heightmapCoords.y);
        float h10 = _terrainData.GetHeight(heightmapCoords.x + 1, heightmapCoords.y);
        float h01 = _terrainData.GetHeight(heightmapCoords.x, heightmapCoords.y + 1);
        float h11 = _terrainData.GetHeight(heightmapCoords.x + 1, heightmapCoords.y + 1);

        // Bilinear interpolation
        float interpolatedHeight = h00 * (1f - fracX) * (1f - fracZ)
                                   + h10 * fracX * (1f - fracZ)
                                   + h01 * (1f - fracX) * fracZ
                                   + h11 * fracX * fracZ;

        return interpolatedHeight;
    }

    public float GetHeight(int x, int y)
    {
        return _terrainData.GetHeight(x, y);
    }
    
    /// <summary>
    /// Gets the steepest downhill direction from a given heightmap coordinate.
    /// </summary>
    /// <param name="x">The X index on the heightmap.</param>
    /// <param name="y">The Y index on the heightmap.</param>
    /// <returns>A Vector2 pointing to the steepest downhill direction. The vector is normalized.</returns>
    public Vector2 GetSteepestDownhillDirection(int x, int y)
    {
        // Ensure the point is within bounds
        if (x <= 0 || x >= _heightmapWidth - 1 || y <= 0 || y >= _heightmapHeight - 1)
            return Vector2.zero; // No downhill if on the edge

        // Get the height of the current point
        float currentHeight = GetHeight(x, y);

        // Initialize variables to track the steepest slope
        Vector2 steepestDirection = Vector2.zero;
        float steepestSlope = 0f;

        // Check the heights of the 8 neighbors
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                // Skip the current point itself
                if (offsetX == 0 && offsetY == 0)
                    continue;

                // Get the height of the neighboring point
                float neighborHeight = GetHeight(x + offsetX, y + offsetY);

                // Calculate the height difference and the distance
                float heightDifference = currentHeight - neighborHeight;
                float distance = Mathf.Sqrt(offsetX * offsetX + offsetY * offsetY);

                // Calculate the slope (gradient)
                float slope = heightDifference / distance;

                // Update if this slope is the steepest so far
                if (slope > steepestSlope)
                {
                    steepestSlope = slope;
                    steepestDirection = new Vector2(offsetX, offsetY);
                }
            }
        }

        // Normalize the direction vector to ensure it's unit-length
        return steepestDirection.normalized;
    }
    
    /// <summary>
    /// Calculates the steepest downhill direction at a point using the gradient of the heightmap.
    /// </summary>
    /// <param name="x">The X index on the heightmap.</param>
    /// <param name="y">The Y index on the heightmap.</param>
    /// <returns>A Vector2 pointing to the steepest downhill direction. The vector is normalized.</returns>
    public Vector2 CalculateGradientDirection(int x, int y)
    {
        // Ensure the point is within bounds to avoid accessing out-of-range indices
        if (x <= 0 || x >= _heightmapWidth - 1 || y <= 0 || y >= _heightmapHeight - 1)
            return Vector2.zero; // No gradient on the edge

        // Compute partial derivatives (central differences)
        float dHeight_dx = (GetHeight(x + 1, y) - GetHeight(x - 1, y)) / 2f; // Partial derivative w.r.t. x
        float dHeight_dz = (GetHeight(x, y + 1) - GetHeight(x, y - 1)) / 2f; // Partial derivative w.r.t. z

        // Gradient vector
        Vector2 gradient = new Vector2(dHeight_dx, dHeight_dz);

        // The steepest downhill direction is the negative gradient
        Vector2 downhillDirection = -gradient.normalized;

        return downhillDirection;
    }
    
    public void SetTerrainHeight(Vector2Int heightmapCoords, float height)
    {
        // Get the current heights at the heightmap position
        float[,] heights = _terrainData.GetHeights(heightmapCoords.x, heightmapCoords.y, 1, 1);

        // Set the new height (normalized between 0 and 1)
        heights[0, 0] = height;

        // Apply the modified heights back to the terrain
        _terrainData.SetHeights(heightmapCoords.x, heightmapCoords.y, heights);
    }
    
    public void AddTerrainHeight(Vector2Int heightmapCoords, float addedHeight)
    {
        float[,] heights = _terrainData.GetHeights(heightmapCoords.x, heightmapCoords.y, 1, 1);
        heights[0, 0] += addedHeight;
        _terrainData.SetHeights(heightmapCoords.x, heightmapCoords.y, heights);
    }
    
    public void AddTerrainHeight(Vector3 worldPosition, float addedHeight)
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