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

    private int kMain, kUpdate;
    private int amountOfVertices = 256;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        SetShadersAndBuffer();
    }


    private void SetShadersAndBuffer() {

        kMain = computeShader.FindKernel("main");
        kUpdate = computeShader.FindKernel("update");

        computeShader.SetInt("verticesSize", amountOfVertices);
        computeShader.SetFloat("desiredSize", 10f);
        computeShader.SetVector("camPos", cam.transform.position);
        computeShader.SetVector("camForward", cam.transform.forward);
        //stores the base mesh vertices
        vertexBuffer = new ComputeBuffer(amountOfVertices, sizeof(float) * 3);
        //stores the actual grass mesh vertices
        grassBuffer = new ComputeBuffer(amountOfVertices * 5 * 2, sizeof(float) * 3);

        //vertex count, instance count, vertex start location, instance start location
        drawArgsBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
        drawArgsBuffer.SetData(new int[] { amountOfVertices * 5 * 2, 1, 0, 0 });

        triangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index | GraphicsBuffer.Target.Structured, amountOfVertices * 4 * 6, sizeof(int));

        computeShader.SetBuffer(kMain, "vertexBuffer", vertexBuffer);
        computeShader.SetBuffer(kUpdate, "vertexBuffer", vertexBuffer);
        computeShader.SetBuffer(kUpdate, "grassBuffer", grassBuffer);
        computeShader.SetBuffer(kUpdate, "triangleBuffer", triangleBuffer);

        //generate the base mesh
        computeShader.Dispatch(kMain, 1, 1, 1);
    }

    private void Update() {
        computeShader.Dispatch(kUpdate, 1, 1, 1);
        int[] triangles = new int[amountOfVertices * 4 * 6];
        Vector3[] vertices = new Vector3[amountOfVertices * 5 * 2];
        grassBuffer.GetData(vertices);
        triangleBuffer.GetData(triangles);
    }

    private void OnRenderObject() {
        mat.SetPass(0);
        mat.SetBuffer("vertices", grassBuffer);
        // Graphics.DrawProceduralIndirect(mat, new Bounds(Vector3.zero, new Vector3(10, 10, 10)), MeshTopology.Points, triangleBuffer, drawArgsBuffer);
        // Graphics.DrawProceduralIndirectNow(MeshTopology.Points, triangleBuffer, drawArgsBuffer);
        Graphics.DrawProceduralNow(MeshTopology.Points, amountOfVertices * 5 * 2);
    }


    private void OnDisable() {
        vertexBuffer.Release();
        grassBuffer.Release();
        triangleBuffer.Release();
        drawArgsBuffer.Release();
    }
}
