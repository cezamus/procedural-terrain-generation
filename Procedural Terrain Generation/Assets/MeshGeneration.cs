using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
public class MeshGeneration : MonoBehaviour
{
    // Mesh setup
    Mesh mesh;
    MeshCollider meshCollider;
    Vector3[] vertices;
    int[] triangles;
    float[] exponentials;
    float height;

    // Player setup
    [SerializeField] Transform player;
    Transform terrain;
    public int extensionStep = 256;
    float xMargeBot;
    float xMargeTop;
    float zMargeBot;
    float zMargeTop;
    bool firstFrame = true;

    // Presets setup
    public enum Preset
    {
        None,
        MountainRocks,
        MountainRocks2,
        MountainRanges,
        SandyDesert,
        StoneDesert
    }
    public Preset preset = Preset.None;

    // Choice of algorithm
    public enum Algorithm
    {
        Random,
        PerlinNoise,
        FractionalBrownianMotion,
        Multifractal,
        RidgedMultifractal
    }
    public Algorithm algorithm = Algorithm.RidgedMultifractal;
    Algorithm privAlgorithm;

    // Terrain properties
    public bool eachTimeRandom = false;
    public int xSize = 1024;
    public int zSize = 1024;
    public int xOffset;
    public int zOffset;
    public float verticalScale = 80f; 
    public float xScale = 0.5f;
    public float zScale = 0.5f;
    public float H = 0.8f;
    public float lacunarity = 2f;
    public float octaves = 18f;

    float offset = 1.05f;
    float gain = 2f;

    public float xHeightDependence = 0f;
    public float zHeightDependence = 0f;
    public float xHDependence = 0f;
    public float zHDependence = 0f;

    // additional variables for changes detection
    int privxSize;
    int privzSize;
    int privxOffset;
    int privzOffset;
    float privverticalScale;
    float privxScale;
    float privzScale;
    float privH;
    float privlacunarity;
    float privoctaves;

    float privxHeightDependence;
    float privzHeightDependence;
    float privxHDependence;
    float privzHDependence;

    // Colors setup
    Color[] colors;
    float minHeight;
    float maxHeight;
    public Gradient gradient;


    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        meshCollider = GetComponent<MeshCollider>();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        terrain = GetComponent<Transform>();

        UsePreset();


        CalculateExponentials();
        UpdateMesh();
        SetPrivs();
    }

    void SetPrivs()
    {
        privAlgorithm = algorithm;
        privxSize = xSize;
        privzSize = zSize;
        privxOffset = xOffset;
        privzOffset = zOffset;
        privverticalScale = verticalScale;
        privxScale = xScale;
        privzScale = zScale;
        privH = H;
        privlacunarity = lacunarity;
        privoctaves = octaves;

        privxHeightDependence = xHeightDependence;
        privzHeightDependence = zHeightDependence;
        privxHDependence = xHDependence;
        privzHDependence = zHDependence;
    }

    void UsePreset()
    {
        switch (preset)
        {
            case Preset.MountainRocks:
                algorithm = Algorithm.Multifractal;
                eachTimeRandom = false;
                xSize = zSize = 1024;
                xOffset = zOffset = 10000;
                verticalScale = 150f;
                xScale = zScale = 0.25f;
                H = 0.15f;
                lacunarity = 2f;
                octaves = 18f;
                xHeightDependence = 0f;
                zHeightDependence = 0f;
                xHDependence = 0f;
                zHDependence = 0f;
                return;case Preset.MountainRocks2:
                algorithm = Algorithm.Multifractal;
                eachTimeRandom = false;
                xSize = zSize = 1024;
                xOffset = zOffset = 10000;
                verticalScale = 200f;
                xScale = zScale = 0.4f;
                H = 0.15f;
                lacunarity = 2.4f;
                octaves = 18f;
                xHeightDependence = 0f;
                zHeightDependence = 0f;
                xHDependence = 0f;
                zHDependence = 0f;
                return;
            case Preset.MountainRanges:
                algorithm = Algorithm.RidgedMultifractal;
                eachTimeRandom = false;
                xSize = zSize = 1024;
                xOffset = zOffset = 30000;
                verticalScale = 100f;
                xScale = zScale = 0.5f;
                H = 0.8f;
                lacunarity = 2f;
                octaves = 18f;
                xHeightDependence = 0f;
                zHeightDependence = 0f;
                xHDependence = 0f;
                zHDependence = 0f;
                return;
            case Preset.SandyDesert:
                algorithm = Algorithm.RidgedMultifractal;
                eachTimeRandom = false;
                xSize = zSize = 1024;
                xOffset = zOffset = 10000;
                verticalScale = 40f;
                xScale = 0.25f;
                zScale = 0.45f;
                H = 1f;
                lacunarity = 6f;
                octaves = 3f;
                xHeightDependence = 0f;
                zHeightDependence = 0f;
                xHDependence = 0f;
                zHDependence = 0f;
                return;
            case Preset.StoneDesert:
                algorithm = Algorithm.FractionalBrownianMotion;
                eachTimeRandom = false;
                xSize = zSize = 1024;
                xOffset = zOffset = 10000;
                verticalScale = 50f;
                xScale = zScale = 0.25f;
                H = 0.9f;
                lacunarity = 2f;
                octaves = 18f;
                xHeightDependence = 0f;
                zHeightDependence = 0f;
                xHDependence = 0f;
                zHDependence = 0f; xHeightDependence = 0f;
                zHeightDependence = 0f;
                xHDependence = 0f;
                zHDependence = 0f;
                return;
            default:
                if (eachTimeRandom)
                {
                    xOffset = Random.Range(1000, 100000);
                    zOffset = Random.Range(1000, 100000);
                }
                else
                {
                    xOffset += 10000;
                    zOffset += 10000;
                }
                return;
        }
    }

    void CalculateExponentials()
    {
        exponentials = new float[(int)octaves];
        float frequency = 1f;
        for (int i = 0; i < (int)octaves; ++i)
        {
            exponentials[i] = Mathf.Pow(frequency, -H);
            frequency *= lacunarity;
        }
    }


    void Update()
    {
        UpdateTerrainPosition();
        CheckPrivs();
    }

    void CheckPrivs()
    {
        if( privAlgorithm != algorithm ||
            privxSize != xSize ||
            privzSize != zSize ||
            privxOffset != xOffset ||
            privzOffset != zOffset ||
            privverticalScale != verticalScale ||
            privxScale != xScale ||
            privzScale != zScale ||
            privH != H ||
            privlacunarity != lacunarity ||
            privoctaves != octaves ||

            privxHeightDependence != xHeightDependence ||
            privzHeightDependence != zHeightDependence ||
            privxHDependence != xHDependence ||
            privzHDependence != zHDependence)
        {
            CalculateExponentials();
            SetPrivs();
            UpdateMesh();
        }
    }

    void UpdateTerrainPosition()
    {
        if(firstFrame)
        {
            terrain.position = player.position - new Vector3(xSize / 2, verticalScale * 2, zSize / 2);
            firstFrame = false;
            return;
        }
        else if (player.position.x < xMargeBot)
        {
            terrain.position -= new Vector3(extensionStep, 0, 0);
            xOffset -= extensionStep;
            if(player.position.z - zMargeBot >= zMargeTop - player.position.z)
            {
                terrain.position += new Vector3(0, 0, extensionStep);
                zOffset += extensionStep;
            }
            else
            {
                terrain.position -= new Vector3(0, 0, extensionStep);
                zOffset -= extensionStep;
            }
        }
        else if (player.position.x > xMargeTop)
        {
            terrain.position += new Vector3(extensionStep, 0, 0);
            xOffset += extensionStep;
            if (player.position.z - zMargeBot >= zMargeTop - player.position.z)
            {
                terrain.position += new Vector3(0, 0, extensionStep);
                zOffset += extensionStep;
            }
            else
            {
                terrain.position -= new Vector3(0, 0, extensionStep);
                zOffset -= extensionStep;
            }
        }
        else if (player.position.z < zMargeBot)
        {
            terrain.position -= new Vector3(0, 0, extensionStep);
            zOffset -= extensionStep;
            if (player.position.x - xMargeBot >= xMargeTop - player.position.x)
            {
                terrain.position += new Vector3(extensionStep, 0, 0);
                xOffset += extensionStep;
            }
            else
            {
                terrain.position -= new Vector3(extensionStep, 0,0);
                xOffset -= extensionStep;
            }
        }
        else if (player.position.z > zMargeTop)
        {
            terrain.position += new Vector3(0, 0, extensionStep);
            zOffset += extensionStep;
            if (player.position.x - xMargeBot >= xMargeTop - player.position.x)
            {
                terrain.position += new Vector3(extensionStep, 0, 0);
                xOffset += extensionStep;
            }
            else
            {
                terrain.position -= new Vector3(extensionStep, 0, 0);
                xOffset -= extensionStep;
            }
        }
        else return;

        UpdateMesh();
        return;
    }

    void UpdateMesh()
    {
        CreateHeightmap();
        CreateTriangles();
        SetColor();
        CalculateMarges();

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        meshCollider.sharedMesh = mesh;
    }

    void CalculateMarges()
    {
        xMargeBot = terrain.position.x + extensionStep;
        xMargeTop = terrain.position.x + xSize - extensionStep;
        zMargeBot = terrain.position.z + extensionStep;
        zMargeTop = terrain.position.z + zSize - extensionStep;
    }

    void CreateHeightmap()
    {
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];
        for(int i = 0, z = 0; z < zSize + 1; ++z)
        {
            for(int x = 0; x < xSize + 1; ++x)
            {
                switch (algorithm)
                {
                    case Algorithm.Random:
                        height = RandomTerrain();
                        break;
                    case Algorithm.PerlinNoise:
                        height = PerlinNoise(x, z);
                        break;
                    case Algorithm.FractionalBrownianMotion:
                        height = FractionalBrownianMotion(x, z);
                        break;
                    case Algorithm.Multifractal:
                        height = Multifractal(x, z);
                        break;
                    case Algorithm.RidgedMultifractal:
                        height = RidgedMultifractal(x, z);
                        break;
                    default:
                        height = RidgedMultifractal(x, z);
                        break;
                }
                
                if (height > maxHeight) maxHeight = height;
                if (height < minHeight) minHeight = height;
                vertices[i] = new Vector3(x, height, z);
                ++i;
            }
        }
    }
    
    void CreateTriangles()
    {
        triangles = new int[xSize * zSize * 6];
        for (int currentVerticle = 0, currentSquare = 0, z = 0; z < zSize; ++z)
        {
            for (int x = 0; x < xSize; ++x)
            {
                triangles[currentSquare] = currentVerticle;
                triangles[currentSquare + 1] = currentVerticle + xSize + 1;
                triangles[currentSquare + 2] = currentVerticle + 1;
                triangles[currentSquare + 3] = currentVerticle + 1;
                triangles[currentSquare + 4] = currentVerticle + xSize + 1;
                triangles[currentSquare + 5] = currentVerticle + xSize + 2;

                ++currentVerticle;
                currentSquare += 6;
            }
            ++currentVerticle;
        }
    }

    void SetColor()
    {
        colors = new Color[vertices.Length];
        for (int i = 0, z = 0; z < zSize + 1; ++z)
        {
            for (int x = 0; x < xSize + 1; ++x)
            {
                colors[i] = gradient.Evaluate(Mathf.InverseLerp(minHeight, maxHeight, vertices[i].y));
                ++i;
            }
        }
    }
    
    // Terrain generation algorithms
    float RandomTerrain()
    {
        float height = Mathf.InverseLerp(0, 1000000, Random.Range(0, 1000000));
        return height * verticalScale;
    }

    float PerlinNoise(int x, int z)
    {
        float xCoord = ((float)x + xOffset) / xSize / xScale;
        float zCoord = ((float)z + zOffset) / zSize / zScale;

        float xHeightModifier = (terrain.position.x + x) * xHeightDependence * 0.01f;
        float zHeightModifier = (terrain.position.z + z) * zHeightDependence * 0.01f;

        return Mathf.PerlinNoise(xCoord, zCoord) * verticalScale * (1 + xHeightModifier + zHeightModifier);
    }

    float FractionalBrownianMotion(int x, int z)
    {
        float value = 0f;

        float xCoord = ((float)x + xOffset) / xSize / xScale;
        float zCoord = ((float)z + zOffset) / zSize / zScale;

        // modifiers are adjusted with small numbers for user convenience 
        float xHeightModifier = (terrain.position.x + x) * xHeightDependence * 0.01f;
        float zHeightModifier = (terrain.position.z + z) * zHeightDependence * 0.01f;
        float xHModifier = (terrain.position.x + x) * xHDependence * 0.001f;
        float zHModifier = (terrain.position.z + z) * zHDependence * 0.001f;

        // inner loop of fractal generation 
        for (int i = 0; i < octaves; i++)
        {
            value += Mathf.PerlinNoise(xCoord, zCoord) * Mathf.Pow(lacunarity, (-H + xHModifier + zHModifier) * i);
            xCoord *= lacunarity;
            zCoord *= lacunarity;
        }

        return value * verticalScale * (1 + xHeightModifier + zHeightModifier);
    }

    float Multifractal(int x, int z)
    {
        float result, signal, weight;

        float xCoord = ((float)x + xOffset) / xSize / xScale;
        float zCoord = ((float)z + zOffset) / zSize / zScale;

        // modifiers are adjusted with small numbers for user convenience */
        float xHeightModifier = (terrain.position.x + x) * xHeightDependence * 0.01f;
        float zHeightModifier = (terrain.position.z + z) * zHeightDependence * 0.01f;
        float xHModifier = (terrain.position.x + x) * xHDependence * 0.001f;
        float zHModifier = (terrain.position.z + z) * zHDependence * 0.001f;

        // computing the first octave 
        result = Mathf.PerlinNoise(xCoord, zCoord) * (exponentials[0] + xHModifier + zHModifier);
        weight = result;

        // increasing frequency
        xCoord *= lacunarity;
        zCoord *= lacunarity;

        // inner loop of fractal generation
        for (int i = 1; i < octaves; i++)
        {
            // preventing divergence 
            if (weight > 1f) weight = 1f;
            // getting next frequency 
            signal = Mathf.PerlinNoise(xCoord, zCoord) * (exponentials[i] + xHModifier + zHModifier);
            // adding it, weighted by local value of previous frequency  
            result += weight * signal;
            weight *= signal;
            // increasing frequency 
            xCoord *= lacunarity;
            zCoord *= lacunarity;
        } 

        return result * verticalScale * (1 + xHeightModifier + zHeightModifier);
    }

    float RidgedMultifractal(int x, int z)
    {
        float result, signal, weight;
        
        float xCoord = ((float)x + xOffset) / xSize / xScale;
        float zCoord = ((float)z + zOffset) / zSize / zScale;

        // modifiers are adjusted with small numbers for user convenience 
        float xHeightModifier = (terrain.position.x + x) * xHeightDependence * 0.01f;
        float zHeightModifier = (terrain.position.z + z) * zHeightDependence * 0.01f;
        float xHModifier = (terrain.position.x + x) * xHDependence * 0.001f;
        float zHModifier = (terrain.position.z + z) * zHDependence * 0.001f;

        // computing the first octave with operation on the noise which generates the ridges 
        signal = Mathf.PerlinNoise(xCoord, zCoord) * 2 - 1;
        if (signal < 0.0) signal = -signal;
        signal = offset - signal;

        // squaring the signal, to make the ridges more visible 
        signal *= signal;
        result = signal;
        for (int i = 1; i < octaves; i++)
        {
            // increase the frequency 
            xCoord *= lacunarity;
            zCoord *= lacunarity;
            // weight successive contributions by previous signal 
            weight = signal * gain;
            if (weight > 1f) weight = 1f;
            if (weight < 0f) weight = 0f;
            signal = Mathf.PerlinNoise(xCoord, zCoord);
            if (signal < 0f) signal = -signal;
            signal = offset - signal;
            signal *= signal;
            signal *= weight;
            result += signal * (exponentials[i] + xHModifier + zHModifier);
        }
        return result * verticalScale * (1 + xHeightModifier + zHeightModifier);
    }
}
