using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Grass : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material mat;

    private ComputeBuffer vertexBuffer;
    private ComputeBuffer grassBuffer;
    private ComputeBuffer drawArgsBuffer;

    private GraphicsBuffer triangleBuffer;

    private int kCreatePositions, kCreateBase, kUpdateBase;
    private int amountOfVertices = 4096;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        SetShadersAndBuffer();
    }


    private void SetShadersAndBuffer() {

        kCreatePositions = computeShader.FindKernel("createPositions");
        kCreateBase = computeShader.FindKernel("createBase");
        kUpdateBase = computeShader.FindKernel("updateBase");

        computeShader.SetInt("verticesSize", amountOfVertices);
        computeShader.SetFloat("desiredSize", 10f);
        computeShader.SetVector("camPos", cam.transform.position);
        computeShader.SetVector("camForward", cam.transform.forward);
        //stores the base mesh vertices
        vertexBuffer = new ComputeBuffer(amountOfVertices, sizeof(float) * 3);
        //stores the grass mesh vertices, updated each frame
        //we have to recaluclate the orientation each frame if the camera movess
        grassBuffer = new ComputeBuffer(amountOfVertices * 5 * 2, sizeof(float) * 3);

        //vertex count, instance count, vertex start location, instance start location
        drawArgsBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
        drawArgsBuffer.SetData(new int[] { amountOfVertices * 5 * 2, 1, 0, 0 });

        triangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index | GraphicsBuffer.Target.Structured, amountOfVertices * 4 * 6, sizeof(int));

        computeShader.SetBuffer(kCreatePositions, "vertexBuffer", vertexBuffer);
        computeShader.SetBuffer(kCreateBase, "vertexBuffer", vertexBuffer);
        computeShader.SetBuffer(kCreateBase, "grassBuffer", grassBuffer);
        computeShader.SetBuffer(kCreateBase, "triangleBuffer", triangleBuffer);
        computeShader.SetBuffer(kUpdateBase, "vertexBuffer", vertexBuffer);
        computeShader.SetBuffer(kUpdateBase, "grassBuffer", grassBuffer);
        mat.SetBuffer("vertices", grassBuffer);

        //generate the positions mesh and the base grass mesh
        computeShader.Dispatch(kCreatePositions, 1, 1, 1);
        computeShader.Dispatch(kCreateBase, 1, 1, 1);

    }

    private void CreateDebugMesh() {
        int[] triangles = new int[amountOfVertices * 4 * 6];
        Vector3[] vertices = new Vector3[amountOfVertices * 5 * 2];
        grassBuffer.GetData(vertices);
        triangleBuffer.GetData(triangles);
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        gameObject.AddComponent<MeshRenderer>();
        gameObject.AddComponent<MeshFilter>().mesh = mesh;
    }

    private void Update() {
        computeShader.SetVector("camPos", cam.transform.position);
        computeShader.SetVector("camForward", cam.transform.forward);
        computeShader.Dispatch(kUpdateBase, 1, 1, 1);
    }


    private void OnRenderObject() {
        mat.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, triangleBuffer, amountOfVertices * 6 * 4);
    }



    private void OnDisable() {
        vertexBuffer.Release();
        grassBuffer.Release();
        triangleBuffer.Release();
        drawArgsBuffer.Release();
    }
}
