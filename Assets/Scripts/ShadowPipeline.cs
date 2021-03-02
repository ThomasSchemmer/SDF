using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ShadowPipeline : RenderPipeline {
    private ComputeShader computeShader;
    private Material mat;

    private ComputeBuffer vertexBuffer;
    private ComputeBuffer grassBuffer;
    private ComputeBuffer drawArgsBuffer;

    private GraphicsBuffer triangleBuffer;

    private int kCreatePositions, kCreateBase, kUpdateBase;
    private int amountOfVertices = 256;

    private CullingResults cull;
    private CommandBuffer buffer = new CommandBuffer() {
            name = "Render Camera"
    };



    public ShadowPipeline(ComputeShader shader, Material mat) {
        computeShader = shader;
        this.mat = mat;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
        foreach(var cam in cameras) {
            Render(context, cam);
        }
    }


    void Render(ScriptableRenderContext context, Camera camera) {
        if (!camera.TryGetCullingParameters(camera, out ScriptableCullingParameters cullingParams))
            return;

        cull = context.Cull(ref cullingParams);

        context.SetupCameraProperties(camera);
        CameraClearFlags clearFlags = camera.clearFlags;
        buffer.ClearRenderTarget(
            (clearFlags & CameraClearFlags.Depth) != 0,
            (clearFlags & CameraClearFlags.Color) != 0,
            camera.backgroundColor
        );
        buffer.BeginSample("Clear Camera");
        //SetShadersAndBuffer(context, camera);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        SortingSettings sorting = new SortingSettings(camera);
        sorting.criteria = SortingCriteria.CommonOpaque;
        ShaderTagId id = new ShaderTagId("ForwardBase"); //SRPDefaultUnlit
        DrawingSettings drawing = new DrawingSettings(id, sorting);

        FilteringSettings filtering = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cull, ref drawing, ref filtering);

        context.DrawSkybox(camera);

        filtering.renderQueueRange = RenderQueueRange.transparent;
        sorting.criteria = SortingCriteria.CommonTransparent;
        context.DrawRenderers(cull, ref drawing, ref filtering);

        buffer.EndSample("Clear Camera");
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

        context.Submit();

    }

    private void SetShadersAndBuffer(ScriptableRenderContext context, Camera camera) {
        kCreatePositions = computeShader.FindKernel("createPositions");
        kCreateBase = computeShader.FindKernel("createBase");
        kUpdateBase = computeShader.FindKernel("updateBase");
        int kUpdateTest = computeShader.FindKernel("test");

        buffer.SetGlobalInt("verticesSize", amountOfVertices);
        buffer.SetComputeFloatParam(computeShader, "desiredSize", 10f);
        buffer.SetComputeVectorParam(computeShader, "camPos", camera.transform.position);
        buffer.SetComputeVectorParam(computeShader, "camForward", camera.transform.forward);
        //stores the base mesh vertices
        vertexBuffer = new ComputeBuffer(amountOfVertices, sizeof(float) * 3);
        //stores the grass mesh vertices, updated each frame
        //we have to recaluclate the orientation each frame if the camera movess
        grassBuffer = new ComputeBuffer(amountOfVertices * 5 * 2, sizeof(float) * 3);

        //vertex count, instance count, vertex start location, instance start location
        drawArgsBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
        drawArgsBuffer.SetData(new int[] { amountOfVertices * 5 * 2, 1, 0, 0 });

        triangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index | GraphicsBuffer.Target.Structured, amountOfVertices * 4 * 6, sizeof(int));

        buffer.SetComputeBufferParam(computeShader, kCreatePositions, "vertexBuffer", vertexBuffer);
        buffer.SetComputeBufferParam(computeShader, kCreateBase, "vertexBuffer", vertexBuffer);
        buffer.SetComputeBufferParam(computeShader, kCreateBase, "grassBuffer", grassBuffer);
        buffer.SetComputeBufferParam(computeShader, kCreateBase, "triangleBuffer", triangleBuffer);
        buffer.SetComputeBufferParam(computeShader, kUpdateBase, "vertexBuffer", vertexBuffer);
        buffer.SetComputeBufferParam(computeShader, kUpdateBase, "grassBuffer", grassBuffer);
        mat.SetBuffer("vertices", grassBuffer);

        //generate the positions mesh and the base grass mesh
        buffer.DispatchCompute(computeShader, kCreatePositions, 1, 1, 1);
        buffer.DispatchCompute(computeShader, kCreateBase, 1, 1, 1);

        Vector3[] vertices = new Vector3[amountOfVertices * 5 * 2];
        grassBuffer.GetData(vertices);

        buffer.SetComputeVectorParam(computeShader, "camPos", camera.transform.position);
        buffer.SetComputeVectorParam(computeShader, "camForward", camera.transform.forward);
        buffer.DispatchCompute(computeShader, kUpdateBase, 1, 1, 1);

        buffer.DrawProcedural(triangleBuffer, camera.cameraToWorldMatrix, mat, 0, MeshTopology.Triangles, amountOfVertices * 6 * 4);

        ReleaseShader();
    }

    public void ReleaseShader() {
        if(vertexBuffer != null)
            vertexBuffer.Release();
        if (grassBuffer != null)
            grassBuffer.Release();
        if (triangleBuffer != null)
            triangleBuffer.Release();
        if (drawArgsBuffer != null)
            drawArgsBuffer.Release();
    }

}
