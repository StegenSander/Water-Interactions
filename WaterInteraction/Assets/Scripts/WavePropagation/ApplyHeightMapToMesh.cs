using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplyHeightMapToMesh : MonoBehaviour
{
    [SerializeField] SimulationData _SimData;
    [SerializeField] Texture2D _CurrentHeightMap;
    [SerializeField] float _MinVertexHeightToBeSurface = 0.5f;
    [SerializeField] float _AverageWaterHeight;
    [SerializeField] float _WaterHeightMaxOffset;

    Renderer _Renderer;
    MeshCollider _MeshCollider;
    MeshFilter _MeshFilter;

    [SerializeField] List<Vector3> _Vertices;
    [SerializeField] List<Vector2> _UVS;
    [SerializeField] List<int> _ImportantIndices;
    // Start is called before the first frame update
    void Start()
    {
        _CurrentHeightMap = new Texture2D(_SimData.TextureSize, _SimData.TextureSize, TextureFormat.RGBAFloat, false);

        _Renderer = GetComponent<Renderer>();
        _MeshCollider = GetComponent<MeshCollider>();
        _MeshFilter = GetComponent<MeshFilter>();
        PrepareMesh();
    }

    public void PrepareMesh()
    {
        Mesh mesh = _MeshFilter.mesh;
        mesh.MarkDynamic();
        _MeshFilter.mesh = mesh;

        _Vertices = new List<Vector3>(mesh.vertices);
        _UVS = new List<Vector2>(mesh.uv);
        _ImportantIndices = new List<int>();

        for (int i =0; i < _Vertices.Count; i ++)
        {
            if (_Vertices[i].z > _MinVertexHeightToBeSurface)
            {
                _ImportantIndices.Add(i);
            }
        }
    }

    public void SetHeightMap(RenderTexture tex)
    {
        RenderTextureToHeightMap(tex);
        ApplyHeightMap();
        SetMeshData();
    }

    public void RenderTextureToHeightMap(RenderTexture renderTex)
    {
        var old_rt = RenderTexture.active;
        RenderTexture.active = renderTex;

        _CurrentHeightMap.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        _CurrentHeightMap.Apply();

        RenderTexture.active = old_rt;
    }

    public void ApplyHeightMap()
    {
        foreach (int vertexIndex in _ImportantIndices)
        {
            float heightOffset = GetHeightFromHeightMap(_UVS[vertexIndex]) - 0.5f;
            heightOffset *= _WaterHeightMaxOffset;

            float height = _AverageWaterHeight + heightOffset;

            Vector3 currentVertex = _Vertices[vertexIndex];
            currentVertex.z = height;
            _Vertices[vertexIndex] = currentVertex;
        }
    }

    void SetMeshData()
    {
        _MeshFilter.mesh.SetVertices(_Vertices);
        _MeshCollider.sharedMesh = _MeshFilter.mesh;
    }

    float GetHeightFromHeightMap(Vector2 uv)
    {
        Color c =_CurrentHeightMap.GetPixel((int)(uv.x * _SimData.TextureSize), (int)(uv.y * _SimData.TextureSize));
        return c.b;
    }
}
