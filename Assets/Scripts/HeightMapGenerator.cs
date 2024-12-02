using UnityEngine;

public class HeightMapGenerator : MonoBehaviour {
    public static int seed;
    public static bool randomizeSeed;

    public static int numOctaves = 4;
    public static float persistence = .5f;
    public static float lacunarity = 2;
    public static float initialScale = 2;

    public static float[,] Generate (int mapSize) {
        var map = new float[mapSize, mapSize];
        seed = (randomizeSeed) ? Random.Range (-10000, 10000) : seed;
        var prng = new System.Random (seed);

        Vector2[] offsets = new Vector2[numOctaves];
        for (int i = 0; i < numOctaves; i++) {
            offsets[i] = new Vector2 (prng.Next (-1000, 1000), prng.Next (-1000, 1000));
        }

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int y = 0; y < mapSize; y++) {
            for (int x = 0; x < mapSize; x++) {
                float noiseValue = 0;
                float scale = initialScale;
                float weight = 1;
                for (int i = 0; i < numOctaves; i++) {
                    Vector2 p = offsets[i] + new Vector2 (x / (float) mapSize, y / (float) mapSize) * scale;
                    noiseValue += Mathf.PerlinNoise (p.x, p.y) * weight;
                    weight *= persistence;
                    scale *= lacunarity;
                }
                map[y, x] = noiseValue;
                minValue = Mathf.Min (noiseValue, minValue);
                maxValue = Mathf.Max (noiseValue, maxValue);
            }
        }

        // Normalize
        if (!Mathf.Approximately(maxValue, minValue)) {
            for (int i = 0; i < mapSize; i++) {
                for (int j = 0; j < mapSize; j++)
                {
                    map[i, j] = (map[i, j] - minValue) / (maxValue - minValue);
                }
            }
        }

        return map;
    }
}