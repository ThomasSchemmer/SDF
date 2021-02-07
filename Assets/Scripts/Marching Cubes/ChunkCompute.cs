using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//import of the MarchingCubes Chunk into a compute shader
//for every chunk in our world, we dispatch a own shader, which returns triplets of vector3 vertices
//unfortunately, we need to pass our defines, such as positions, edge combinations as well as the edge index lookup

//Todo: Remove debug offset and create actual chunk subdivision
public class ChunkCompute : MonoBehaviour
{
    Vector4[] positions = new Vector4[8]{
        0.5f * new Vector3(-1, -1, 1),
        0.5f * new Vector3(1, -1, 1),
        0.5f * new Vector3(1, -1, -1),
        0.5f * new Vector3(-1, -1, -1),
        0.5f * new Vector3(-1, 1, 1),
        0.5f * new Vector3(1, 1, 1),
        0.5f * new Vector3(1, 1, -1),
        0.5f * new Vector3(-1, 1, -1)
    };

    Vector4[] edges = new Vector4[12] {
        new Vector2(0, 1),
        new Vector2(1, 2),
        new Vector2(2, 3),
        new Vector2(3, 0),
        new Vector2(4, 5),
        new Vector2(5, 6),
        new Vector2(6, 7),
        new Vector2(7, 4),
        new Vector2(0, 4),
        new Vector2(1, 5),
        new Vector2(2, 6),
        new Vector2(3, 7),
    };

    public ComputeShader shader;
    public Material mat;

    private int kernel;
    private ComputeBuffer triTableBuffer;
    private ComputeBuffer resultBuffer;
    private Vector3[] cubes;

    // Start is called before the first frame update
    void Start()
    {
        InitShader();
        shader.Dispatch(kernel, 1, 1, 1);
        resultBuffer.GetData(cubes);
        CreateMesh();
    }

    private void CreateMesh() {
        MeshRenderer rend = this.gameObject.AddComponent<MeshRenderer>();
        MeshFilter filter = this.gameObject.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        //we can iterate in triples, as our shader only generates triangle data
        for(int i = 0; i < cubes.Length; i += 3) {
            if (float.IsNaN(cubes[i].x))
                break;
            int c = vertices.Count;
            vertices.Add(cubes[i + 0]);
            vertices.Add(cubes[i + 1]);
            vertices.Add(cubes[i + 2]);
            triangles.AddRange(new int[] {c + 0, c + 1, c + 2});
        }
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        filter.mesh = mesh;
        rend.material = mat;
    }

    private void InitShader() {
        Vector4 threadSizes = new Vector4(1, 1, 1, 0);
        int count = (int)(threadSizes.x * threadSizes.y * threadSizes.z);
        cubes = new Vector3[count * 16];
        kernel = shader.FindKernel("main");
        shader.SetVectorArray("positions", positions);
        shader.SetVectorArray("edges", edges);
        shader.SetVector("threadSizes", threadSizes);
        resultBuffer = new ComputeBuffer(count, sizeof(float) * 3 * 16);
        shader.SetBuffer(kernel, "result", resultBuffer);
        ParseTriTable();
    }

    private void ParseTriTable() {
        triTableBuffer = new ComputeBuffer(Defines.triTable.Length * 16, sizeof(uint));
        uint[] edges = new uint[Defines.triTable.Length * 16];
        int i = 0;
        foreach (int[] e in Defines.triTable) {
            for(int j = 0; j < e.Length; j++) {
                edges[i * 16 + j] = (uint)e[j];
            }
            i++; 
        }
        triTableBuffer.SetData(edges);
        shader.SetBuffer(kernel, "triTable", triTableBuffer);
    }

    private void OnApplicationQuit() {
        triTableBuffer.Dispose();
        resultBuffer.Dispose();
    }
}
