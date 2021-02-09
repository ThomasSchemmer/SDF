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
    private ComputeBuffer resultBuffer;
    private Vector3[] cubes;
    private RenderTexture rt;
    private Vector3 lastPos;
    private MeshRenderer rend;
    private MeshFilter filter;
    private Mesh mesh;

    void Start() {
        rend = this.gameObject.AddComponent<MeshRenderer>();
        filter = this.gameObject.AddComponent<MeshFilter>();
        lastPos = Vector3.one * float.MaxValue;
        rend.material = mat;
        InitShader();
    }

    private void Update() {
        if ((transform.position - lastPos).magnitude > 0.01f) {
            shader.SetVector("worldPosition", transform.position);
            shader.Dispatch(kernel, 1, 1, 1);
            resultBuffer.GetData(cubes);
            CreateMesh();
            lastPos = transform.position;
        }
    }

    private void CreateMesh() {
        mesh = new Mesh();
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
        int[] edges = new int[Defines.triTable.Length * 16];
        int i = 0;
        foreach (int[] e in Defines.triTable) {
            for(int j = 0; j < e.Length; j++) {
                edges[i * 16 + j] = e[j];
            }
            i++; 
        }
        shader.SetTexture(kernel, "triTex", ParseEdgesIntoTexture(edges));
        shader.SetInt("triTexWidth", rt.width);
    }

    //if we use a StructuredBuffer for the 16*256 uint edge indices, we very quickly pass 
    //the recommended amount of registers for eacah shader thread (~16k)
    //to go around that, simply merge all indices into a texture and pass that along
    //unfortunately, shader can't access simple texture2d, only render textures
    //so we create a tex2d, copy it into the rt, and pass that
    //update: nevermind, same reaction with an rt.. 
    private RenderTexture ParseEdgesIntoTexture(int[] edges) {
        Color[] colors = new Color[edges.Length / 4];
        for(int i = 0; i < colors.Length; i++) {
            //unity color only ranges internally from 0..1. So we scale our edge id with 256
            //we cannot pass the "edge not set" identifier "-1" into the color, as it will be cut off
            //within unity. NaN doesn't work as well. as Defines.TriEdgeTable has only entries to ~12
            //we set it to an approriately higher number
            colors[i] = new Color(  edges[i * 4 + 0] >= 0 ? edges[i * 4 + 0] / 256f : 20 / 256f,
                                    edges[i * 4 + 1] >= 0 ? edges[i * 4 + 1] / 256f : 20 / 256f,
                                    edges[i * 4 + 2] >= 0 ? edges[i * 4 + 2] / 256f : 20 / 256f,
                                    edges[i * 4 + 3] >= 0 ? edges[i * 4 + 3] / 256f : 20 / 256f);
        }
        int size = Mathf.CeilToInt(Mathf.Sqrt(edges.Length / 4));
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, 1, false);
        tex.SetPixels(colors);
        tex.Apply();
        rt = RenderTexture.GetTemporary(size, size, 1, RenderTextureFormat.ARGB32);
        rt.enableRandomWrite = true;

        Graphics.CopyTexture(tex, rt);
        Destroy(tex);
        return rt;
    }

    private void OnApplicationQuit() {
        resultBuffer.Dispose();
        RenderTexture.ReleaseTemporary(rt);
    }
}
