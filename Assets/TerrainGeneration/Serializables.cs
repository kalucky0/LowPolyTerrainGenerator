using UnityEngine;

[System.Serializable]
public class Vector2i
{
    public int x;
    public int y;
    public Vector2i(int _x, int _y)
    {
        x = _x;
        y = _y;
    }
}

[System.Serializable]
public class Biome
{
    public Color color = Color.black;
    public float startHeight = 0f;
    public float endHeight = 1f;
}

[System.Serializable]
public class Vertex
{
    public int index;
    public int[] vertexIndices = new int[0];
    public int[] meshIndices = new int[0];

    public Vertex(int _index)
    {
        index = _index;
    }

    public void SetPosition(float x, float y, TerrainSystem terrainSystem)
    {
        for (int i = 0; i < vertexIndices.Length; i++)
        {
            terrainSystem.vertexData[vertexIndices[i]].x = x;
            terrainSystem.vertexData[vertexIndices[i]].z = y;
        }
    }

    public void SetHeight(float value, TerrainSystem terrainSystem)
    {
        terrainSystem.heightMap[index] = value;
        for (int i = 0; i < vertexIndices.Length; i++)
        {
            terrainSystem.vertexData[vertexIndices[i]].y = terrainSystem.heightMap[index];
        }
    }

    public void UpdateHeight(float value, TerrainSystem terrainSystem)
    {
        terrainSystem.heightMap[index] += value;
        for (int i = 0; i < vertexIndices.Length; i++)
        {
            terrainSystem.vertexData[vertexIndices[i]].y = terrainSystem.heightMap[index];
        }
    }

    public float GetHeight(TerrainSystem terrainSystem)
    {
        return terrainSystem.heightMap[index];
    }

    public void SetColor(Color color, TerrainSystem terrainSystem)
    {
        terrainSystem.colorMap[index] = color;
    }

    public Color GetColor(TerrainSystem terrainSystem)
    {
        return terrainSystem.colorMap[index];
    }

    public void AddVertexIndex(int _index)
    {
        System.Array.Resize(ref vertexIndices, vertexIndices.Length + 1);
        vertexIndices[vertexIndices.Length - 1] = _index;
    }

    public void AddMeshIndex(int _index)
    {
        for (int i = 0; i < meshIndices.Length; i++)
        {
            if (meshIndices[i] == _index)
            {
                return;
            }
        }
        System.Array.Resize(ref meshIndices, meshIndices.Length + 1);
        meshIndices[meshIndices.Length - 1] = _index;
    }
}