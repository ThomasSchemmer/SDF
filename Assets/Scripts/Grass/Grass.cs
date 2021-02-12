using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grass : MonoBehaviour
{
    public ComputeShader shader;
    public Material mat;

    private ComputeBuffer vertexBuffer;
    private ComputeBuffer grassBuffer;
    private int kMain, kUpdate;
    private int amountOfVertices = 512;

    private Vector3[] vertices;
    private Vector3[] grass;

    private MeshFilter filter;
    private MeshRenderer rend;
    private Mesh mesh;
    private Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        filter = gameObject.AddComponent<MeshFilter>();
        rend = gameObject.AddComponent<MeshRenderer>();
        cam = Camera.main;
        LoadVertices();
        CreateMesh();
        CreateGrass();

    }

    // update the grass
    void Update()
    {
    }

    private void CreateGrass() {

        shader.SetVector("camPos", cam.transform.position);
        shader.SetVector("camForward", cam.transform.forward);
        grassBuffer = new ComputeBuffer(amountOfVertices * 5 * 2, sizeof(float) * 3);
        grass = new Vector3[amountOfVertices * 5 * 2];
        shader.SetBuffer(kUpdate, "vertexBuffer", vertexBuffer);
        shader.SetBuffer(kUpdate, "grassBuffer", grassBuffer);
        shader.Dispatch(kUpdate, 1, 1, 1);
        grassBuffer.GetData(grass);

        Mesh mesh = new Mesh();
        List<Vector3> truncs = new List<Vector3>();
        foreach(Vector3 vec in grass) {
            if (!float.IsNaN(vec.x))
                truncs.Add(vec);
        }
        Vector3[] truncArr = truncs.ToArray();
        int[] indices = new int[truncArr.Length];
        for (int i = 0; i < truncArr.Length; i++) {
            indices[i] = i;
        }
        mesh.vertices = truncArr;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        mesh.RecalculateBounds();
        GameObject child = new GameObject();
        child.AddComponent<MeshFilter>().mesh = mesh;
        child.AddComponent<MeshRenderer>().material = mat;
    }

    private void CreateMesh() {
        int[] indices = new int[vertices.Length];
        for(int i = 0; i < vertices.Length; i++) {
            indices[i] = i;
        }
        if (!mesh)
            mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        mesh.RecalculateBounds();
        filter.mesh = mesh;
        rend.material = mat;
    }

    private void LoadVertices() {
        kMain = shader.FindKernel("main");
        kUpdate = shader.FindKernel("update");
        vertexBuffer = new ComputeBuffer(amountOfVertices, sizeof(float) * 3);
        vertices = new Vector3[amountOfVertices];
        shader.SetInt("verticesSize", amountOfVertices);
        shader.SetFloat("desiredSize", 10f);
        shader.SetBuffer(kMain, "vertexBuffer", vertexBuffer);
        shader.Dispatch(kMain, 1, 1, 1);
        vertexBuffer.GetData(vertices);
    }

    private void OnApplicationQuit() {
        vertexBuffer.Release();
        grassBuffer.Release();
    }
}
