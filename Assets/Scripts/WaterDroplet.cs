using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class WaterDroplet : MonoBehaviour
{
    private const float DIR_STEP = 0.025f;
    
    // Droplet properties
    public float erosionRadius = 3f;
    public float inertia = 0.05f;
    public float sedimentCapacityFactor = 4f;
    public float minSedimentCapacity = 0.01f;
    public float erodeSpeed = 0.03f;
    public float depositSpeed = 0.03f;
    public float evaporateSpeed = 0.01f;
    public float gravity = 4f;
    public int maxLifetime = 30;
    public float initialWaterVolume = 1f;
    public float initialSpeed = 1f;

    // Droplet simulation state
    private Vector3 position;
    private Vector2 direction;
    private float speed;
    private float waterVolume;
    private float sediment;

    // Reference to terrain
    public TerrainHeightController terrainController;

    // Internal state
    private int lifetime;

    private void Awake()
    {
        terrainController = GameObject.FindWithTag("Terrain").GetComponent<TerrainHeightController>();
    }

    private void Start()
    {
        // Initialize droplet properties
        position = transform.position;
        direction = Random.insideUnitCircle.normalized;
        speed = initialSpeed;
        waterVolume = initialWaterVolume;
        sediment = 0f;
        lifetime = 0;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (lifetime > maxLifetime || waterVolume <= 0f)
            {
                Destroy(gameObject); // Stop simulation
                return;
            }

            lifetime++;

            // Interpolate height and gradient at current position
            float currentHeight = terrainController.GetHeightOnHeightmap(position);
            Vector2 gradient = terrainController.GetGradient(position);

            // Update direction using inertia
            Vector2 newDirection = (direction * inertia - gradient * (1 - inertia)).normalized * DIR_STEP;
            direction = newDirection;

            //Debug.DrawLine(position, position + new Vector3(direction.x, terrainController.GetHeight(position), direction.y));

            // Move the droplet
            position.x += direction.x;
            position.z += direction.y;
            position.y = currentHeight;
            //position.y = currentHeight;
            
            // Convert droplet position to heightmap coordinates
            Vector2Int heightmapCoords = terrainController.WorldToHeightmapCoords(position);
            if (heightmapCoords.x < 0 || heightmapCoords.y < 0 ||
                heightmapCoords.x + 1 >= terrainController._heightmapWidth ||
                heightmapCoords.y + 1 >= terrainController._heightmapHeight)
            {
                Destroy(gameObject); // Droplet left the terrain
                return;
            }

            // Calculate new height and height difference
            float newHeight = terrainController.GetHeightOnHeightmap(position);
            float deltaHeight = newHeight - currentHeight;

            // Update sediment capacity
            float sedimentCapacity = Mathf.Max(-deltaHeight * speed * waterVolume * sedimentCapacityFactor,
                minSedimentCapacity);

            // Deposit or erode sediment
            if (sediment > sedimentCapacity || deltaHeight > 0)
            {
                // Deposit sediment
                float depositAmount = deltaHeight > 0
                    ? Mathf.Min(sediment, deltaHeight)
                    : (sediment - sedimentCapacity) * depositSpeed / terrainController.GetTerrainSize().y;
                //terrainController.DepositSediment(position, depositAmount, erosionRadius);
                terrainController.AddSedimentToHeightmap(position, depositAmount);
                sediment -= depositAmount;
                Debug.Log("Deposit Amount: " + depositAmount);
            }
            else
            {
                // Erode sediment
                float erodeAmount = Mathf.Min((sedimentCapacity - sediment) * erodeSpeed, -deltaHeight) / terrainController.GetTerrainSize().y;
                //terrainController.ErodeTerrain(position, erodeAmount, erosionRadius);
                terrainController.AddSedimentToHeightmap(position, -erodeAmount);
                sediment += erodeAmount;
                Debug.Log("Erode Amount: " + erodeAmount);
            }

            // Update droplet speed and water volume
            speed = Mathf.Sqrt(speed * speed + deltaHeight * gravity);
            waterVolume *= (1 - evaporateSpeed);

            // Check if the droplet should stop
            if (speed <= 0.01f || waterVolume <= 0f)
            {
                Destroy(gameObject);
            }

            transform.position = position;
        }
    }
}
