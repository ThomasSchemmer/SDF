using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public ComputeShader shader;

    public struct Node {
        public Vector2 center;
        public float width;
        public uint created;
        public uint split;
        public uint count;
        public uint dataCount;
        public uint dataOffset;
    }

    private int partitionKernel, createNodesKernel, updateDataOffsetsKernel, fillDataPointsKernel;
    private ComputeBuffer particleBuffer, nodeBuffer, debugBuffer, xValuesBuffer, yValuesBuffer;
    private int maxTreeDepth = 3;
    private int maxParticlesInNode = 5;
    private int amountOfParticles = 500000;

    private Node[] nodes;

    List<Particle> particles; 
    // Start is called before the first frame update
    void Start()
    {
        CreateParticles();
        CreateBuffers();
        GPUCreateAndFillQuadTree();
    }

    void GPUCreateAndFillQuadTree() {
        shader.Dispatch(partitionKernel, particles.Count / 64, 1, 1);
        shader.Dispatch(createNodesKernel, 1, 1, 1);
        shader.Dispatch(partitionKernel, particles.Count / 64, 1, 1);
        shader.Dispatch(createNodesKernel, 1, 1, 1);
        shader.Dispatch(partitionKernel, particles.Count / 64, 1, 1);
        shader.Dispatch(updateDataOffsetsKernel, 1, 1, 1);
        Node[] nodes = new Node[21];
        nodeBuffer.GetData(nodes);
        shader.Dispatch(fillDataPointsKernel, particles.Count / 64, 1, 1);
        float[] xs = new float[particles.Count];
        float[] ys = new float[particles.Count];
        xValuesBuffer.GetData(xs);
        yValuesBuffer.GetData(ys);
        nodeBuffer.GetData(nodes);
        Particle[] ps = new Particle[amountOfParticles];
        particleBuffer.GetData(ps);
        //TODO: Particles dont get saved correctly, dataCount is wrong, particles are empty
    }

    void CreateParticles() {
        particles = new List<Particle>();
        for(int i = 0; i < amountOfParticles; i++) {
            Particle p = new Particle(new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f)), Vector2.zero, Vector2.zero);
            particles.Add(p);
        }
    }

    private void CreateBuffers() {
        particleBuffer = new ComputeBuffer(particles.Count, sizeof(float) * 2 * 3 + sizeof(uint));
        particleBuffer.SetData(particles.ToArray());
        xValuesBuffer = new ComputeBuffer(particles.Count, sizeof(float));
        yValuesBuffer = new ComputeBuffer(particles.Count, sizeof(float));

        float[] ids = new float[particles.Count];
        for (int i = 0; i < ids.Length; i++) {
            ids[i] = 42u;
        }
        debugBuffer = new ComputeBuffer(particles.Count, sizeof(float));
        debugBuffer.SetData(ids);

        int size = (int)((Mathf.Pow(4, maxTreeDepth) - 1) / 3);
        nodeBuffer = new ComputeBuffer(size, sizeof(uint) * 5 + sizeof(float) * 3);

        Node node;
        node.count = 0;
        node.dataCount = 0;
        node.created = 0;
        node.split = 0;
        node.center = new Vector2(0.5f, 0.5f);
        node.width = 1;
        node.dataOffset = 0;
        nodes = new Node[size];
        nodes[0] = node;
        nodeBuffer.SetData(nodes);


        partitionKernel = shader.FindKernel("Partition");
        createNodesKernel = shader.FindKernel("CreateNodes");
        updateDataOffsetsKernel = shader.FindKernel("UpdateDataOffsets");
        fillDataPointsKernel = shader.FindKernel("FillDataPoints");

        shader.SetBuffer(partitionKernel, "particles", particleBuffer);
        shader.SetBuffer(partitionKernel, "nodes", nodeBuffer);
        shader.SetBuffer(partitionKernel, "debug", debugBuffer);
        shader.SetBuffer(createNodesKernel, "particles", particleBuffer);
        shader.SetBuffer(createNodesKernel, "nodes", nodeBuffer);
        shader.SetBuffer(createNodesKernel, "debug", debugBuffer);
        shader.SetBuffer(updateDataOffsetsKernel, "debug", debugBuffer);
        shader.SetBuffer(updateDataOffsetsKernel, "nodes", nodeBuffer);
        shader.SetBuffer(fillDataPointsKernel, "nodes", nodeBuffer);
        shader.SetBuffer(fillDataPointsKernel, "particles", particleBuffer);
        shader.SetBuffer(fillDataPointsKernel, "xValues", xValuesBuffer);
        shader.SetBuffer(fillDataPointsKernel, "yValues", yValuesBuffer);
        shader.SetInt("particleAmount", particles.Count);
        shader.SetInt("maxTreeDepth", maxTreeDepth);
        shader.SetInt("maxParticlesInNode", maxParticlesInNode);
        shader.SetInt("groupX", 1);
        shader.SetInt("groupY", 1);
    }

    private void OnDestroy() {
        particleBuffer.Dispose();
        nodeBuffer.Dispose();
        debugBuffer.Dispose();
        xValuesBuffer.Dispose();
        yValuesBuffer.Dispose();
    }
}
