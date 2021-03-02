using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Grass : MonoBehaviour
{
    public ComputeShader computeShader;
    public RenderTexture debug;
    public Material mat;
    public Light light;
    
    private ComputeBuffer vertexBuffer;
    private ComputeBuffer grassBuffer;
    private ComputeBuffer drawArgsBuffer;

    private GraphicsBuffer triangleBuffer;

    private int kCreatePositions, kUpdateBase;
    private int amountOfVertices = 4096;

    private Camera cam;

    struct Vertex {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
    }


    private CommandBuffer cameraCommandBuffer, lightCommandBuffer;

    void Start()
    {
        cam = Camera.main;
        SetShadersAndBuffer();
        if (cam.renderingPath == RenderingPath.DeferredShading) {
            cameraCommandBuffer = new CommandBuffer();
            cameraCommandBuffer.DrawProcedural(triangleBuffer, Matrix4x4.identity, mat, 0, MeshTopology.Triangles, amountOfVertices * 6 * 4);
            cam.AddCommandBuffer(CameraEvent.BeforeGBuffer, cameraCommandBuffer);
            
            lightCommandBuffer = new CommandBuffer();
            lightCommandBuffer.DrawProcedural(triangleBuffer, Matrix4x4.identity, mat, 1, MeshTopology.Triangles, amountOfVertices * 6 * 4);
            //TODO: create shadow map twice, once with and once without the grass
            //grass should not get self shadow, as it is blocky and ugly
            //see unity light examples for custom shadow casting?
           // light.AddCommandBuffer(LightEvent.BeforeShadowMapPass, lightCommandBuffer);

            //copy shadow texture from lightsource into debug tex
            //this way we can make it a global texture and easily access it in our shader
            CommandBuffer shadowMapCommandBuffer = new CommandBuffer();
            RenderTargetIdentifier shadowMap = BuiltinRenderTextureType.CurrentActive;
            RenderTexture shadowMapCopy = debug;
            shadowMapCommandBuffer.SetShadowSamplingMode(shadowMap, ShadowSamplingMode.RawDepth);
            var id = new RenderTargetIdentifier(shadowMapCopy);
           // shadowMapCommandBuffer.Blit(shadowMap, id);
            //"blur" by down- and subsequent upscaling into a temp rt
            RenderTargetIdentifier temp = new RenderTargetIdentifier(0);
            shadowMapCommandBuffer.GetTemporaryRT(0, debug.width / 40, debug.height / 40, 16, FilterMode.Point, RenderTextureFormat.ARGB32);
            shadowMapCommandBuffer.Blit(shadowMap, temp);
            shadowMapCommandBuffer.Blit(temp, id);
            shadowMapCommandBuffer.ReleaseTemporaryRT(0);
            shadowMapCommandBuffer.SetGlobalTexture("_ShadowMapCopy", id);
            light.AddCommandBuffer(LightEvent.AfterShadowMap, shadowMapCommandBuffer);
        }
    }


    private void SetShadersAndBuffer() {

        kCreatePositions = computeShader.FindKernel("createPositions");
        kUpdateBase = computeShader.FindKernel("updateBase");

        computeShader.SetInt("verticesSize", amountOfVertices);
        computeShader.SetFloat("desiredSize", 10f);
        computeShader.SetVector("camPos", cam.transform.position);
        computeShader.SetVector("camForward", cam.transform.forward);
        computeShader.SetVector("lightPos", light.transform.position);
        //stores the base mesh vertices
        vertexBuffer = new ComputeBuffer(amountOfVertices, sizeof(float) * 3);
        //stores the grass mesh vertices, updated each frame
        //we have to recaluclate the orientation each frame if the camera movess
        grassBuffer = new ComputeBuffer(amountOfVertices * 5 * 2, sizeof(float) * 8);

        //vertex count, instance count, vertex start location, instance start location
        drawArgsBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
        drawArgsBuffer.SetData(new int[] { amountOfVertices * 5 * 2, 1, 0, 0 });

        triangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index | GraphicsBuffer.Target.Structured, amountOfVertices * 4 * 6, sizeof(int));

        computeShader.SetBuffer(kCreatePositions, "vertexBuffer", vertexBuffer);
        computeShader.SetBuffer(kCreatePositions, "triangleBuffer", triangleBuffer);
        computeShader.SetBuffer(kUpdateBase, "vertexBuffer", vertexBuffer);
        computeShader.SetBuffer(kUpdateBase, "grassBuffer", grassBuffer);
        mat.SetBuffer("vertices", grassBuffer);


        //generate the positions mesh and the base grass mesh
        computeShader.Dispatch(kCreatePositions, 1, 1, 1);


        Vertex[] vertexs = new Vertex[amountOfVertices * 5 * 2];
        grassBuffer.GetData(vertexs);

    }

    private void CreateDebugMesh() {
        float worldSize = 100;
        Vector3[] vertices = new Vector3[2];
        vertices[0] = Vector3.one * -worldSize;
        vertices[1] = Vector3.one * +worldSize;
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.RecalculateBounds();

        gameObject.AddComponent<MeshRenderer>();
        gameObject.AddComponent<MeshFilter>().mesh = mesh;
    }

    private void Update() {
        computeShader.SetVector("camPos", cam.transform.position);
        computeShader.SetVector("camForward", cam.transform.forward);
        computeShader.SetVector("lightPos", light.transform.position);
        mat.SetVector("_lightDirection", light.transform.forward);
        mat.SetVector("_CameraPos", cam.transform.position);
        computeShader.Dispatch(kUpdateBase, 1, 1, 1);
    }

    private void OnDrawGizmos() {
        Vertex[] vertices = new Vertex[10];
        if (grassBuffer == null)
            return;
        grassBuffer.GetData(vertices, 0, 200, 10);
        for (int i = 0; i < 10; i++) {
            Gizmos.DrawLine(vertices[i].position, vertices[i].position + vertices[i].normal);
        }
    }

    private void OnRenderObject() {
        //used if forward rendering
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
