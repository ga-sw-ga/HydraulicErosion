using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class WaterDroplet : MonoBehaviour
{
    private const float DIR_STEP = 0.05f;
    
    // Droplet properties
    public float erosionRadius = 0.15f;
    public float inertia = 0.05f;
    public float sedimentCapacityFactor = 4f;
    public float minSedimentCapacity = 0.01f;
    public float erodeSpeed = 0.3f;
    public float depositSpeed = 0.3f;
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
        //if (Input.GetKey(KeyCode.Space))
        {
            if (lifetime > maxLifetime || waterVolume <= 0f)
            {
                Destroy(gameObject); // Stop simulation
                return;
            }

            lifetime++;

            // Convert droplet position to heightmap coordinates
            Vector2Int heightmapCoords = terrainController.WorldToHeightmapCoords(position);
            if (heightmapCoords.x < 0 || heightmapCoords.y < 0 ||
                heightmapCoords.x >= terrainController._heightmapWidth ||
                heightmapCoords.y >= terrainController._heightmapHeight)
            {
                Destroy(gameObject); // Droplet left the terrain
                return;
            }

            // Interpolate height and gradient at current position
            float currentHeight = terrainController.GetHeight(position);
            Vector2 gradient = terrainController.CalculateGradient(position);

            // Update direction using inertia
            Vector2 newDirection = (direction * inertia - gradient * (1 - inertia)).normalized * DIR_STEP;
            direction = newDirection;

            //Debug.DrawLine(position, position + new Vector3(direction.x, terrainController.GetHeight(position), direction.y));

            // Move the droplet
            position.x += direction.x;
            position.z += direction.y;
            position.y = terrainController.GetHeight(position);

            // Calculate new height and height difference
            float newHeight = terrainController.GetHeight(position);
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
                    : (sediment - sedimentCapacity) * depositSpeed;
                //terrainController.DepositSediment(position, depositAmount, erosionRadius);
                terrainController.AddSediment(position, depositAmount);
                sediment -= depositAmount;
            }
            else
            {
                // Erode sediment
                float erodeAmount = Mathf.Min((sedimentCapacity - sediment) * erodeSpeed, -deltaHeight);
                //terrainController.ErodeTerrain(position, erodeAmount, erosionRadius);
                terrainController.AddSediment(position, -erodeAmount);
                sediment += erodeAmount;
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
