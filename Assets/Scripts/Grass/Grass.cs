using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Grass : MonoBehaviour
{
    public ComputeShader grassComputeShader, perlinComputeShader;
    public RenderTexture debug;
    public Material mat;
    public new Light light;
    public Texture2D myTex;
    
    private ComputeBuffer vertexBuffer;
    private ComputeBuffer grassBuffer;
    private ComputeBuffer drawArgsBuffer;

    private GraphicsBuffer triangleBuffer;
    private Vector3 position = new Vector3(1, 0, 1);

    private int kCreatePositions, kUpdateBase, kPerlin;
    private const int size = 32;
    private int amountOfVertices = size * size;
    private float desiredSize = 10;
    private Vector2 offset = new Vector2(0.25f, -0.3f);

    private Camera cam;
    private RenderTexture shadowMapRT, perlinRT;

    struct Vertex {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
    }


    private CommandBuffer cameraCommandBuffer, lightCommandBuffer;

    void Start()
    {
        cam = Camera.main;
        Perlin();
        SetShadersAndBuffer();
        ShowDebug();

        HandleDeferredRendering();
    }

    private void ShowDebug() {
        Vector3[] vex = new Vector3[amountOfVertices];
        Color[] colors = new Color[amountOfVertices];
        vertexBuffer.GetData(vex);
        Texture2D tex = new Texture2D(size, size);
        for(int i = 0; i < vex.Length; i++) {
            colors[i] = new Color(vex[i].x, vex[i].y, vex[i].z, 1);
        }
        tex.SetPixels(colors);
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        GameObject computeCanvas = GameObject.Find("Canvas/Compute");
        if(computeCanvas)
            computeCanvas.GetComponent<Image>().sprite = sprite;
        Destroy(tex);
    }

    private void Perlin() {
        if (!perlinRT) { 
            perlinRT = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
            perlinRT.enableRandomWrite = true;
            perlinRT.Create();
            kPerlin = perlinComputeShader.FindKernel("main");
            perlinComputeShader.SetTexture(kPerlin, "Result", perlinRT);
            perlinComputeShader.SetVector("size", new Vector4(perlinRT.width, perlinRT.height, 0, 0));
        }
        perlinComputeShader.SetVector("offset", offset * Time.time);
        perlinComputeShader.Dispatch(kPerlin, perlinRT.width / 32, perlinRT.height / 32, 1);
        GameObject.Find("Canvas/RT").GetComponent<RawImage>().texture = perlinRT;
    }


    private void SetShadersAndBuffer() {
        kCreatePositions = grassComputeShader.FindKernel("createPositions");
        kUpdateBase = grassComputeShader.FindKernel("updateBase");

        grassComputeShader.SetInt("verticesSize", amountOfVertices);
        grassComputeShader.SetFloat("desiredSize", desiredSize);
        grassComputeShader.SetVector("camPos", cam.transform.position);
        grassComputeShader.SetVector("camForward", cam.transform.forward);
        grassComputeShader.SetVector("lightPos", light.transform.position);
        grassComputeShader.SetVector("timeOffset", new Vector2(0.1f, 0.1f) * Time.time);
        grassComputeShader.SetVector("worldPos", position);
        grassComputeShader.SetVector("minPos", new Vector3(0, 0, 0));
        grassComputeShader.SetVector("maxPos", new Vector3(20, 0, 20));
        grassComputeShader.SetTexture(kCreatePositions, "perlinTex", perlinRT);
        grassComputeShader.SetTexture(kUpdateBase, "perlinTex", perlinRT);
        //stores the base mesh vertices
        vertexBuffer = new ComputeBuffer(amountOfVertices, sizeof(float) * 3);
        //stores the grass mesh vertices, updated each frame
        //we have to recaluclate the orientation each frame if the camera movess
        grassBuffer = new ComputeBuffer(amountOfVertices * 5 * 2, sizeof(float) * 8);

        //vertex count, instance count, vertex start location, instance start location
        drawArgsBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
        drawArgsBuffer.SetData(new int[] { amountOfVertices * 5 * 2, 1, 0, 0 });

        triangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index | GraphicsBuffer.Target.Structured, amountOfVertices * 4 * 6, sizeof(int));

        grassComputeShader.SetBuffer(kCreatePositions, "vertexBuffer", vertexBuffer);
        grassComputeShader.SetBuffer(kCreatePositions, "triangleBuffer", triangleBuffer);
        grassComputeShader.SetBuffer(kUpdateBase, "vertexBuffer", vertexBuffer);
        grassComputeShader.SetBuffer(kUpdateBase, "grassBuffer", grassBuffer);
        mat.SetBuffer("vertices", grassBuffer);

        //generate the positions mesh and the base grass mesh
        grassComputeShader.Dispatch(kCreatePositions, 1, 1, 1);
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
        if (grassComputeShader == null)
            return;

        Graphics.SetRandomWriteTarget(1, perlinRT);
        grassComputeShader.SetVector("camPos", cam.transform.position);
        grassComputeShader.SetVector("camForward", cam.transform.forward);
        grassComputeShader.SetVector("lightPos", light.transform.position);
        mat.SetVector("_lightDirection", light.transform.forward);
        mat.SetVector("_CameraPos", cam.transform.position);
        grassComputeShader.Dispatch(kUpdateBase, 1, 1, 1);

        Graphics.ClearRandomWriteTargets();

        Perlin();
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

    private void HandleDeferredRendering() {
        if (cam.renderingPath != RenderingPath.DeferredShading)
            return;

        shadowMapRT = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        shadowMapRT.enableRandomWrite = true;
        shadowMapRT.Create();
        cameraCommandBuffer = new CommandBuffer();
        cameraCommandBuffer.DrawProcedural(triangleBuffer, Matrix4x4.identity, mat, 0, MeshTopology.Triangles, amountOfVertices * 6 * 4);
        cam.AddCommandBuffer(CameraEvent.BeforeGBuffer, cameraCommandBuffer);

        lightCommandBuffer = new CommandBuffer();
        //TODO: create shadow map twice, once with and once without the grass
        //grass should not get self shadow, as it is blocky and ugly
        //see unity light examples for custom shadow casting?
        //lightCommandBuffer.DrawProcedural(triangleBuffer, Matrix4x4.identity, mat, 1, MeshTopology.Triangles, amountOfVertices * 6 * 4);
        light.AddCommandBuffer(LightEvent.BeforeShadowMapPass, lightCommandBuffer);

        //copy shadow texture from lightsource into debug tex
        //this way we can make it a global texture and easily access it in our shader
        CommandBuffer shadowMapCommandBuffer = new CommandBuffer();
        RenderTargetIdentifier shadowMap = BuiltinRenderTextureType.CurrentActive;
        shadowMapCommandBuffer.SetShadowSamplingMode(shadowMap, ShadowSamplingMode.RawDepth);
        var id = new RenderTargetIdentifier(shadowMapRT);
        // shadowMapCommandBuffer.Blit(shadowMap, id);
        //"blur" by down- and subsequent upscaling into a temp rt
        /*
        RenderTargetIdentifier temp = new RenderTargetIdentifier(0);
        shadowMapCommandBuffer.GetTemporaryRT(0, -1, -1, 16, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
        shadowMapCommandBuffer.Blit(shadowMap, temp);
        shadowMapCommandBuffer.Blit(temp, id);
        shadowMapCommandBuffer.ReleaseTemporaryRT(0);
        */
        shadowMapCommandBuffer.Blit(shadowMap, shadowMapRT);
        shadowMapCommandBuffer.SetGlobalTexture("_ShadowMapCopy", id);
        light.AddCommandBuffer(LightEvent.AfterShadowMap, shadowMapCommandBuffer);
    }

    private void OnDisable() {
        vertexBuffer.Release();
        grassBuffer.Release();
        triangleBuffer.Release();
        drawArgsBuffer.Release();
        if(shadowMapRT != null)
            shadowMapRT.Release();
        perlinRT.Release();
    }
}
