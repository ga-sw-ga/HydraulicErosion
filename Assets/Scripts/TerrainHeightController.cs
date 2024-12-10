using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class TerrainHeightController : MonoBehaviour
{
    public int iterationNum, currentIteration = 0, iterationChunkSize = 50;
    [HideInInspector] public Terrain _terrain;
    [HideInInspector] public TerrainData _terrainData;
    [HideInInspector] public int _heightmapWidth, _heightmapHeight;
    [HideInInspector] public Vector3 _terrainSize;
    [HideInInspector] public Vector3 _terrainPosition;

    public GameObject waterDropletGameObject;
    
    private float[,] _initialHeights, _heightMap;

    private Dictionary<int, (int, int, float)[]> brushRadiiOffsets = new Dictionary<int, (int, int, float)[]>;


    private void Awake()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        
        _terrain = GetComponent<Terrain>();
        _terrainData = _terrain.terrainData;
        _heightmapWidth = _terrainData.heightmapResolution;
        _heightmapHeight = _terrainData.heightmapResolution;
        _terrainSize = _terrainData.size;
        _terrainPosition = _terrain.transform.position;
    }

    void Start()
    {
        SaveInitialHeightmap();
        _heightMap = CopyHeightmap(_initialHeights);
        //_heightMap = HeightMapGenerator.Generate(_initialHeights.GetLength(0));
        Instantiate(waterDropletGameObject, new Vector3(21f, 17f, 34f), Quaternion.identity);
        //AdjustTerrainHeight(targetHeight);
    }

    private void Update()
    {
        if (currentIteration <= iterationNum)
        {
            for (int i = 0; i < iterationChunkSize; i++)
            {
                Instantiate(waterDropletGameObject, new Vector3(Random.Range(0f, 50f), 0f, Random.Range(0f, 50f)), Quaternion.identity);
                currentIteration++;
            }
        }

        if (Input.GetKey(KeyCode.Space))
        {
            //Instantiate(waterDropletGameObject, new Vector3(Random.Range(0f, 50f), 0f, Random.Range(0f, 50f)), Quaternion.identity);
            //Instantiate(waterDropletGameObject, new Vector3(12f, 15f, 33f), Quaternion.identity);
            //Instantiate(waterDropletGameObject, new Vector3(21.5f, 5f, 36f), Quaternion.identity);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            LoadHeightmap(_heightMap);
        }
        
        // Check for mouse click
        /*if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            // Create a ray from the camera through the mouse position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Raycast to see if it hits the terrain
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Check if the ray hit the terrain
                if (hit.collider.gameObject == gameObject)
                {
                    // Convert world position to terrain coordinates
                    Vector3 terrainPosition = hit.point;

                    // Normalize terrain coordinates (0 to 1)
                    Vector3 normalizedPosition = new Vector3(
                        (terrainPosition.x - _terrain.transform.position.x) / _terrainData.size.x,
                        (terrainPosition.y - _terrain.transform.position.y) / _terrainData.size.y,
                        (terrainPosition.z - _terrain.transform.position.z) / _terrainData.size.z
                    );

                    // Convert normalized coordinates to heightmap coordinates
                    int x = Mathf.RoundToInt(normalizedPosition.x * (_terrainData.heightmapResolution - 1));
                    int z = Mathf.RoundToInt(normalizedPosition.z * (_terrainData.heightmapResolution - 1));
                    
                    // Set the height at the clicked point
                    _heightMap[z, x] = 1.0f;

                    // Apply the updated heightmap
                    LoadHeightmap(_heightMap);
                }
            }
        }*/
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // If the game exits play mode
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            LoadHeightmap(_initialHeights);
        }
    }
    
    public Vector2Int WorldToHeightmapCoords(Vector3 worldPosition)
    {
        // Calculate relative position within the terrain (normalized to [0, 1] range)
        float relativeX = (worldPosition.x - _terrainPosition.x) / _terrainSize.x;
        float relativeZ = (worldPosition.z - _terrainPosition.z) / _terrainSize.z;

        // Clamp relative position to be within the heightmap bounds
        // relativeX = Mathf.Clamp01(relativeX);
        // relativeZ = Mathf.Clamp01(relativeZ);

        // Convert normalized position to heightmap indices
        int heightmapX = Mathf.FloorToInt(relativeX * (_terrainData.heightmapResolution - 1));
        int heightmapZ = Mathf.FloorToInt(relativeZ * (_terrainData.heightmapResolution - 1));

        return new Vector2Int(heightmapX, heightmapZ);
    }
    
    public Vector3 HeightmapToWorldCoords(Vector2Int heightmapCoords)
    {
        // Get the relative position on the heightmap (0 to 1)
        float relativeX = (float)heightmapCoords.x / (_terrainData.heightmapResolution - 1);
        float relativeZ = (float)heightmapCoords.y / (_terrainData.heightmapResolution - 1);

        // Convert the relative position to world position
        float worldX = _terrainPosition.x + relativeX * _terrainSize.x;
        float worldZ = _terrainPosition.z + relativeZ * _terrainSize.z;

        // Get the height at the given heightmap coordinates
        float height = _terrainData.GetHeight(heightmapCoords.x, heightmapCoords.y);

        // Calculate the world position with the height
        Vector3 worldPosition = new Vector3(worldX, _terrainPosition.y + height, worldZ);

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
        float relativeX = (worldPosition.x - _terrainPosition.x) / _terrainSize.x * (_heightmapWidth - 1);
        float relativeZ = (worldPosition.z - _terrainPosition.z) / _terrainSize.z * (_heightmapHeight - 1);
        
        int baseX = Mathf.Clamp(Mathf.FloorToInt(relativeX), 0, _heightmapWidth - 2);
        int baseZ = Mathf.Clamp(Mathf.FloorToInt(relativeZ), 0, _heightmapHeight - 2);
        
        float fracX = relativeX - baseX;
        float fracZ = relativeZ - baseZ;
        
        AddTerrainHeight(new Vector2Int(baseX, baseZ), amount * (1f - fracX) * (1f - fracZ));
        AddTerrainHeight(new Vector2Int(baseX, baseZ + 1), amount * (1f - fracX) * fracZ);
        AddTerrainHeight(new Vector2Int(baseX + 1, baseZ), amount * fracX * (1f - fracZ));
        AddTerrainHeight(new Vector2Int(baseX + 1, baseZ + 1), amount * fracX * fracZ);
    }

    public void AddSedimentToHeightmap(Vector3 worldPosition, float amount)
    {
        Vector2Int heightmapCoords = WorldToHeightmapCoords(worldPosition);
        
        float relativeX = (worldPosition.x - _terrainPosition.x) / _terrainSize.x * (_heightmapWidth - 1);
        float relativeZ = (worldPosition.z - _terrainPosition.z) / _terrainSize.z * (_heightmapHeight - 1);
        
        int baseX = Mathf.Clamp(Mathf.FloorToInt(relativeX), 0, _heightmapWidth - 2);
        int baseZ = Mathf.Clamp(Mathf.FloorToInt(relativeZ), 0, _heightmapHeight - 2);

        float fracX = relativeX - baseX;
        float fracZ = relativeZ - baseZ;
        
        _heightMap[heightmapCoords.y, heightmapCoords.x] += amount * (1f - fracX) * (1f - fracZ);
        _heightMap[heightmapCoords.y + 1, heightmapCoords.x] += amount * (1f - fracX) * fracZ;
        _heightMap[heightmapCoords.y, heightmapCoords.x + 1] += amount * fracX * (1f - fracZ);
        _heightMap[heightmapCoords.y + 1, heightmapCoords.x + 1] += amount * fracX * fracZ;
    }
    
    public Vector2 CalculateCellOffset(Vector3 worldPosition)
    {
        // Calculate normalized position on the heightmap
        float relativeX = (worldPosition.x - _terrainPosition.x) / _terrainSize.x * (_heightmapWidth - 1);
        float relativeZ = (worldPosition.z - _terrainPosition.z) / _terrainSize.z * (_heightmapHeight - 1);

        // Get the fractional part to determine offset within the cell
        float xOffset = relativeX - Mathf.Floor(relativeX);
        float zOffset = relativeZ - Mathf.Floor(relativeZ);

        return new Vector2(xOffset, zOffset);
    }
    
    public Vector2 CalculateGradient(Vector3 worldPosition)
    {
        // Get normalized position within the heightmap
        float relativeX = (worldPosition.x - _terrainPosition.x) / _terrainSize.x * (_heightmapWidth - 1);
        float relativeZ = (worldPosition.z - _terrainPosition.z) / _terrainSize.z * (_heightmapHeight - 1);

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
    
    public Vector2 GetGradient(Vector3 worldPosition)
    {
        // Get normalized position within the heightmap
        float relativeX = (worldPosition.x - _terrainPosition.x) / _terrainSize.x * (_heightmapWidth - 1);
        float relativeZ = (worldPosition.z - _terrainPosition.z) / _terrainSize.z * (_heightmapHeight - 1);

        int baseX = Mathf.Clamp(Mathf.FloorToInt(relativeX), 0, _heightmapWidth - 2);
        int baseZ = Mathf.Clamp(Mathf.FloorToInt(relativeZ), 0, _heightmapHeight - 2);

        // Fetch surrounding heights
        float h00 = _heightMap[baseZ, baseX];
        float h10 = _heightMap[baseZ, baseX + 1];
        float h01 = _heightMap[baseZ + 1, baseX];
        float h11 = _heightMap[baseZ + 1, baseX + 1];
        
        // Compute the fractional offsets
        float fracX = relativeX - baseX;
        float fracZ = relativeZ - baseZ;

        // Calculate gradients in the X and Z directions
        float gradientX = (h10 - h00) * (1 - fracZ) + 
                          (h11 - h01) * fracZ;

        float gradientZ = (h01 - h00) * (1 - fracX) + 
                          (h11 - h10) * fracX;

        // Return the gradient vector in world coordinates
        return new Vector2(gradientX, gradientZ);
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

        // Get normalized position within the cell
        float relativeX = (worldPosition.x - _terrainPosition.x) / _terrainSize.x * (_heightmapWidth - 1);
        float relativeZ = (worldPosition.z - _terrainPosition.z) / _terrainSize.z * (_heightmapHeight - 1);

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
    
    public float GetHeightOnHeightmap(Vector3 worldPosition)
    {
        // Get normalized position within the heightmap
        float relativeX = (worldPosition.x - _terrainPosition.x) / _terrainSize.x * (_heightmapWidth - 1);
        float relativeZ = (worldPosition.z - _terrainPosition.z) / _terrainSize.z * (_heightmapHeight - 1);

        int baseX = Mathf.Clamp(Mathf.FloorToInt(relativeX), 0, _heightmapWidth - 2);
        int baseZ = Mathf.Clamp(Mathf.FloorToInt(relativeZ), 0, _heightmapHeight - 2);

        // Fetch surrounding heights
        float h00 = _heightMap[baseZ, baseX];
        float h10 = _heightMap[baseZ, baseX + 1];
        float h01 = _heightMap[baseZ + 1, baseX];
        float h11 = _heightMap[baseZ + 1, baseX + 1];
        
        // Compute the fractional offsets
        float fracX = relativeX - baseX;
        float fracZ = relativeZ - baseZ;

        // Bilinear interpolation
        float interpolatedHeight = h00 * (1f - fracX) * (1f - fracZ)
                                   + h10 * fracX * (1f - fracZ)
                                   + h01 * (1f - fracX) * fracZ
                                   + h11 * fracX * fracZ;

        return interpolatedHeight;
    }

    public float GetHeightOnHeightmap(int x, int y)
    {
        return _heightMap[x, y];
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

    private void SaveInitialHeightmap()
    {
        _initialHeights = _terrainData.GetHeights(0, 0, _heightmapWidth, _heightmapHeight);
    }

    private void LoadHeightmap(float [,] heightmap)
    {
        _terrainData.SetHeights(0, 0, heightmap);
    }
    
    private float[,] CopyHeightmap(float[,] heightmap)
    {
        int rows = heightmap.GetLength(0);
        int cols = heightmap.GetLength(1);

        float[,] copy = new float[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                copy[i, j] = heightmap[i, j];
            }
        }

        return copy;
    }

    private void Temp()
    {
        int rows = _heightMap.GetLength(0);

        for (int i = 0; i < rows; i++)
        {
            Debug.Log(_terrainData.GetHeight(i, i) / _heightMap[i, i]);
        }
        
        Debug.Log("_terrainData.size.y: " + _terrainData.size.y);
        Debug.Log("_terrainData.alphamapHeight: " + _terrainData.alphamapHeight);
        Debug.Log("_terrainData.detailHeight: " + _terrainData.detailHeight);
        Debug.Log("_terrainData.heightmapScale.y: " + _terrainData.heightmapScale.y);
    }
    
    

}