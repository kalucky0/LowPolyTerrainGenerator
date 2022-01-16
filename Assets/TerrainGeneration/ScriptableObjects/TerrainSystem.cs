using UnityEngine;

[CreateAssetMenu(fileName = "TerrainSystem", menuName = "Terrain System", order = 3)]
public class TerrainSystem : ScriptableObject
{

    [HideInInspector]
    public TerrainGenerator generator;
    [HideInInspector]
    public Transform terrain;

    public Vector2 size = new Vector2(500f, 500f);
    public Vector2i resolution = new Vector2i(100, 100);

    public Mesh[] meshes;
    public Vertex[] vertices;

    public Vector2 vertexDistance;
    public Vector3[] vertexData;
    public int[] triangleData;
    public Vector2[] uvData;
    public float[] heightMap;
    public Color[] colorMap;
    [HideInInspector]
    public Material material;
    [HideInInspector]
    public Texture2D texture;

    [Range(0f, 1f)]
    public float interpolation = 0.5f;
    public FilterMode filterMode = FilterMode.Trilinear;
    public Biome[] biomes = new Biome[0];

    public int chunkSize = 6000;

    [HideInInspector]
    public bool isInitialized = false;

    public TerrainSystem Initialise(TerrainGenerator genertor)
    {
        isInitialized = true;
        generator = genertor;
        terrain = new GameObject("Terrain").transform;
        terrain.SetParent(generator.transform);
        CreateTerrain();

        return this;
    }

    public void Reinitialise()
    {
        CreateTerrain();
    }

    public void SetSize(Vector2 _size)
    {
        if (size != _size)
        {
            size = _size;
            for (int y = 0; y < resolution.y; y++)
            {
                for (int x = 0; x < resolution.x; x++)
                {
                    Vector2 position = GridToWorld(x, y);
                    vertices[GridToArray(x, y)].SetPosition(position.x, position.y, this);
                }
            }
            for (int i = 0; i < meshes.Length; i++)
            {
                meshes[i].vertices = vertexData.RangeSubset(i * chunkSize, Mathf.Min(chunkSize, vertexData.Length - i * chunkSize));
                meshes[i].RecalculateNormals();
                //Terrain.GetChild(i).GetComponent<MeshCollider>().sharedMesh = Meshes[i];
            }
        }
    }

    public void SetResolution(Vector2i _resolution)
    {
        resolution.x = Mathf.Max(resolution.x, 2);
        resolution.y = Mathf.Max(resolution.y, 2);
        if (resolution.x != _resolution.x || resolution.y != _resolution.y)
        {
            resolution = _resolution;
            CreateTerrain();
        }
    }

    public void Update()
    {
        terrain.localPosition = Vector3.zero;
        terrain.localRotation = Quaternion.identity;
        for (int i = 0; i < terrain.childCount; i++)
        {
            terrain.GetChild(i).localPosition = Vector3.zero;
            terrain.GetChild(i).localRotation = Quaternion.identity;
        }
    }

    public void Record()
    {
        //TODO
    }

    public void Undo()
    {
        //TODO
    }

    public void Redo()
    {
        //TODO
    }

    public void ModifyTerrain(Vector2 world, float size, float strength, ToolType tool)
    {
        bool[] meshUpdates = new bool[meshes.Length];

        float sqrSize = size * size;
        for (float y = world.y - size; y <= world.y + size; y += vertexDistance.y)
        {
            for (float x = world.x - size; x <= world.x + size; x += vertexDistance.x)
            {
                float sqrDist = (world.x - x) * (world.x - x) + (world.y - y) * (world.y - y);
                if (sqrDist <= sqrSize)
                {
                    Vertex vertex = GetVertex(x, y);
                    if (vertex != null)
                    {
                        float weight = (size - Mathf.Sqrt(sqrDist)) / size;
                        switch (tool)
                        {
                            case ToolType.Normal:
                                vertex.UpdateHeight(weight * strength, this);
                                break;

                            case ToolType.Noise:
                                vertex.UpdateHeight(weight * Random.Range(0f, strength), this);
                                break;

                            case ToolType.Bumps:
                                vertex.UpdateHeight(weight * Random.Range(-strength, strength), this);
                                break;

                            case ToolType.Smooth:
                                float neighbours = 0f;
                                float height = 0f;
                                for (int i = -1; i <= 1; i++)
                                {
                                    for (int j = -1; j <= 1; j++)
                                    {
                                        Vertex neighbour = GetVertex(x + i, y + j);
                                        if (neighbour != null)
                                        {
                                            neighbours += 1f;
                                            height += neighbour.GetHeight(this);
                                        }
                                    }
                                }
                                float avg = height / neighbours;
                                vertex.SetHeight((1f - weight) * vertex.GetHeight(this) + weight * avg, this);
                                break;
                        }
                        for (int i = 0; i < vertex.meshIndices.Length; i++)
                        {
                            meshUpdates[vertex.meshIndices[i]] = true;
                        }
                    }
                }
            }
        }

        for (int i = 0; i < meshes.Length; i++)
        {
            if (meshUpdates[i])
            {
                meshes[i].vertices = vertexData.RangeSubset(i * chunkSize, Mathf.Min(chunkSize, vertexData.Length - i * chunkSize));
                meshes[i].RecalculateNormals();
                //Terrain.GetChild(i).GetComponent<MeshCollider>().sharedMesh = Meshes[i];
            }
        }
    }

    public void ModifyTexture(Vector2 world, float size, float strength, Color color)
    {
        float sqrSize = size * size;
        for (float y = world.y - size; y <= world.y + size; y += vertexDistance.y)
        {
            for (float x = world.x - size; x <= world.x + size; x += vertexDistance.x)
            {
                float sqrDist = (world.x - x) * (world.x - x) + (world.y - y) * (world.y - y);
                if (sqrDist <= sqrSize)
                {
                    Vertex vertex = GetVertex(x, y);
                    if (vertex != null)
                    {
                        vertex.SetColor(Color.Lerp(vertex.GetColor(this), color, (size - Mathf.Sqrt(sqrDist)) / size), this);
                    }
                }
            }
        }
        texture.SetPixels(colorMap);
        texture.Apply();
    }

    public Vertex GetVertex(float worldX, float worldY)
    {
        return GetVertex(GetCoordinates(worldX, worldY));
    }

    public Vertex GetVertex(Vector2i grid)
    {
        return GetVertex(grid.x, grid.y);
    }

    public Vertex GetVertex(int gridX, int gridY)
    {
        if (gridX >= 0 && gridY >= 0 && gridX < resolution.x && gridY < resolution.y)
            return vertices[GridToArray(gridX, gridY)];
        else
            return null;
    }

    public Vector2i GetCoordinates(float worldX, float worldY)
    {
        int x = Mathf.RoundToInt((worldX + size.x / 2f) / size.x * resolution.x);
        int y = Mathf.RoundToInt((worldY + size.y / 2f) / size.y * resolution.y);
        return new Vector2i(x, y);
    }

    public Vector2 GridToWorld(int gridX, int gridY)
    {
        return new Vector2(size.x * (float)gridX / ((float)resolution.x - 1f) - size.x / 2f, size.y * (float)gridY / ((float)resolution.y - 1) - size.y / 2f);
    }

    public int GridToArray(int gridX, int gridY)
    {
        return gridY * resolution.x + gridX;
    }

    public void SetHeightMap(float[] heightMap)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].SetHeight(heightMap[i], this);
        }
        for (int i = 0; i < meshes.Length; i++)
        {
            meshes[i].vertices = vertexData.RangeSubset(i * chunkSize, Mathf.Min(chunkSize, vertexData.Length - i * chunkSize));
            meshes[i].RecalculateNormals();
            //Terrain.GetChild(i).GetComponent<MeshCollider>().sharedMesh = Meshes[i];
        }
    }

    public void SetColorMap(Color[] colorMap)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].SetColor(colorMap[i], this);
        }
        texture.SetPixels(colorMap);
        texture.filterMode = filterMode;
        texture.Apply();
    }

    public float[] CreateHeightMap(int seed, float scale, int octaves, float persistance, float lacunarity, float falloffStrength, float falloffRamp, float falloffRange, Vector2 offset, float heightMultiplier, AnimationCurve heightCurve)
    {
        float[] heightMap = new float[resolution.x * resolution.y];

        Vector2[] octaveOffsets = new Vector2[octaves];

        Random.InitState(seed);

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = Random.Range(-100f, 100f);
            float offsetY = Random.Range(-100f, 100f);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;
        for (int y = 0; y < resolution.y; y++)
        {
            for (int x = 0; x < resolution.x; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float xPos = (((float)x + offset.x) - (float)resolution.x / 2f) / (float)resolution.x;
                    float yPos = (((float)y + offset.y) - (float)resolution.y / 2f) / (float)resolution.y;
                    float sampleX = frequency * scale * xPos + octaveOffsets[i].y;
                    float sampleY = frequency * scale * yPos + octaveOffsets[i].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                maxLocalNoiseHeight = Mathf.Max(maxLocalNoiseHeight, noiseHeight);
                minLocalNoiseHeight = Mathf.Min(minLocalNoiseHeight, noiseHeight);

                heightMap[GridToArray(x, y)] = noiseHeight;
            }
        }

        for (int y = 0; y < resolution.y; y++)
        {
            for (int x = 0; x < resolution.x; x++)
            {
                float value = Mathf.Max(Mathf.Abs(x / (float)resolution.x * 2f - 1f), Mathf.Abs(y / (float)resolution.y * 2f - 1f));
                float a = Mathf.Pow(value, falloffRamp);
                float b = Mathf.Pow(falloffRange - falloffRange * value, falloffRamp);
                float falloff = 1f - (a + b != 0f ? falloffStrength * a / (a + b) : 0f);
                heightMap[GridToArray(x, y)] = heightMultiplier * heightCurve.Evaluate(falloff * Utility.Normalise(heightMap[GridToArray(x, y)], minLocalNoiseHeight, maxLocalNoiseHeight, 0f, 1f));
            }
        }

        return heightMap;
    }

    public Color[] CreateColorMap()
    {
        Color[] colorMap = new Color[resolution.x * resolution.y];
        for (int y = 0; y < resolution.y; y++)
        {
            for (int x = 0; x < resolution.x; x++)
            {
                float height = GetVertex(x, y).GetHeight(this) / generator.HeightMultiplier;
                int index = GetBiomeIndex(height);
                if (index != -1)
                {
                    Color color, colorPrevious, colorNext;
                    color = biomes[index].color;

                    if (index > 0)
                        colorPrevious = biomes[index - 1].color;
                    else
                        colorPrevious = biomes[index].color;
                    if (index < biomes.Length - 1)
                        colorNext = biomes[index + 1].color;
                    else
                        colorNext = biomes[index].color;
                    
                    float distPrevious = interpolation * (1f - (height - biomes[index].startHeight) / (biomes[index].endHeight - biomes[index].startHeight));
                    float distNext = interpolation * (1f - (biomes[index].endHeight - height) / (biomes[index].endHeight - biomes[index].startHeight));
                    color = Color.Lerp(Color.Lerp(color, colorPrevious, distPrevious), Color.Lerp(color, colorNext, distNext), 0.5f);
                    colorMap[GridToArray(x, y)] = color;
                }
                else
                {
                    colorMap[GridToArray(x, y)] = Color.white;
                }
            }
        }
        return colorMap;
    }

    public void SetBiomeStartHeight(int index, float value)
    {
        if (index > 0)
        {
            biomes[index].startHeight = Mathf.Max(biomes[index - 1].startHeight, value);
            biomes[index - 1].endHeight = biomes[index].startHeight;
        }
        else
        {
            biomes[index].startHeight = 0f;
        }
    }

    public void SetBiomeEndHeight(int index, float value)
    {
        if (index < biomes.Length - 1)
        {
            biomes[index].endHeight = Mathf.Min(biomes[index + 1].endHeight, value);
            biomes[index + 1].startHeight = biomes[index].endHeight;
        }
        else
        {
            biomes[index].endHeight = 1f;
        }
    }

    public void SetBiomeColor(int index, Color color)
    {
        biomes[index].color = color;
    }

    public int GetBiomeIndex(float height)
    {
        for (int i = 0; i < biomes.Length; i++)
        {
            if (biomes[i].startHeight <= height && biomes[i].endHeight >= height)
            {
                return i;
            }
        }
        return -1;
    }

    private void CreateTerrain()
    {
        float[] heightMap = new float[resolution.x * resolution.y];
        Color[] colorMap = new Color[resolution.x * resolution.y];
        for (int i = 0; i < colorMap.Length; i++)
        {
            colorMap[i] = Color.grey;
        }
        CreateTerrain(heightMap, colorMap);
    }

    private void CreateTerrain(float[] heightMap)
    {
        Color[] colorMap = new Color[resolution.x * resolution.y];
        for (int i = 0; i < colorMap.Length; i++)
        {
            colorMap[i] = Color.grey;
        }
        CreateTerrain(heightMap, colorMap);
    }

    private void CreateTerrain(Color[] colorMap)
    {
        float[] heightMap = new float[resolution.x * resolution.y];
        CreateTerrain(heightMap, colorMap);
    }

    private void CreateTerrain(float[] _heightMap, Color[] _colorMap)
    {
        //Clean up
        while (terrain.childCount > 0)
        {
            DestroyImmediate(terrain.GetChild(0).gameObject);
        }
        DestroyImmediate(material);
        Resources.UnloadUnusedAssets();

        //Allocate memory
        heightMap = _heightMap;
        colorMap = _colorMap;
        vertices = new Vertex[resolution.x * resolution.y];
        vertexData = new Vector3[resolution.x * resolution.y];
        triangleData = new int[6 * resolution.x * resolution.y];
        uvData = new Vector2[resolution.x * resolution.y];

        //Calculate vertex distance
        vertexDistance = new Vector2(size.x / (float)resolution.x, size.y / (float)resolution.y);

        //Create vertices
        for (int y = 0; y < resolution.y; y++)
        {
            for (int x = 0; x < resolution.x; x++)
            {
                int index = GridToArray(x, y);
                Vector2 position = GridToWorld(x, y);
                vertices[index] = new Vertex(index);
                vertexData[index] = new Vector3(position.x, heightMap[index], position.y);
            }
        }

        //Create triangles
        int triangleIndex = 0;
        for (int y = 0; y < resolution.y - 1; y++)
        {
            for (int x = 0; x < resolution.x - 1; x++)
            {
                int a = GridToArray(x, y);
                int b = GridToArray(x, y + 1);
                int c = GridToArray(x + 1, y);
                int d = GridToArray(x + 1, y + 1);

                triangleData[triangleIndex] = a;
                vertices[a].AddVertexIndex(triangleIndex);
                vertices[a].AddMeshIndex(Mathf.FloorToInt((float)triangleIndex / (float)chunkSize));
                triangleIndex += 1;

                triangleData[triangleIndex] = d;
                vertices[d].AddVertexIndex(triangleIndex);
                vertices[d].AddMeshIndex(Mathf.FloorToInt((float)triangleIndex / (float)chunkSize));
                triangleIndex += 1;

                triangleData[triangleIndex] = c;
                vertices[c].AddVertexIndex(triangleIndex);
                vertices[c].AddMeshIndex(Mathf.FloorToInt((float)triangleIndex / (float)chunkSize));
                triangleIndex += 1;

                triangleData[triangleIndex] = d;
                vertices[d].AddVertexIndex(triangleIndex);
                vertices[d].AddMeshIndex(Mathf.FloorToInt((float)triangleIndex / (float)chunkSize));
                triangleIndex += 1;

                triangleData[triangleIndex] = a;
                vertices[a].AddVertexIndex(triangleIndex);
                vertices[a].AddMeshIndex(Mathf.FloorToInt((float)triangleIndex / (float)chunkSize));
                triangleIndex += 1;

                triangleData[triangleIndex] = b;
                vertices[b].AddVertexIndex(triangleIndex);
                vertices[b].AddMeshIndex(Mathf.FloorToInt((float)triangleIndex / (float)chunkSize));
                triangleIndex += 1;

            }
        }

        //Create UVs
        for (int y = 0; y < resolution.y; y++)
        {
            for (int x = 0; x < resolution.x; x++)
            {
                float percentX = (float)x / ((float)resolution.x);
                float percentY = (float)y / ((float)resolution.y);
                uvData[GridToArray(x, y)] = new Vector2(percentX, percentY);
            }
        }

        //Apply flat shading
        Utility.FlatShading(ref vertexData, ref triangleData, ref uvData);

        //Create meshes
        meshes = Utility.CreateMeshes(vertexData, triangleData, uvData, chunkSize);

        //Create material, texture, colormap
        material = new Material(Shader.Find("Standard"));
        texture = Utility.CreateTexture(colorMap, resolution.x, resolution.y, filterMode);
        material.mainTexture = texture;

        //Instantiate
        for (int i = 0; i < meshes.Length; i++)
        {
            GameObject instance = new GameObject("Mesh");
            instance.transform.SetParent(terrain);
            MeshRenderer renderer = instance.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            MeshFilter filter = instance.AddComponent<MeshFilter>();
            filter.sharedMesh = meshes[i];
            instance.AddComponent<MeshCollider>();
        }
    }
}
