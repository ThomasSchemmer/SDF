using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public ComputeShader shader;

    public struct Node {
        public Vector2 center;
        public float width;
        public uint id;
        public uint split;
        public uint count;
    }

    private int partitionKernel, createNodesKernel;
    private ComputeBuffer particleBuffer, nodeBuffer, debugBuffer;
    private int maxTreeDepth = 3;
    private int maxParticlesInNode = 5;

    private Node[] nodes;

    List<Particle> particles; 
    // Start is called before the first frame update
    void Start()
    {
        CreateParticles();
        CreateBuffers();
        float[] ids = new float[particles.Count];
        for (int i = 0; i < ids.Length; i++) {
            ids[i] = 69.69f;
        }
        debugBuffer.SetData(ids);
        shader.Dispatch(partitionKernel, particles.Count / 64, 1, 1);
        nodeBuffer.GetData(nodes);
        debugBuffer.GetData(ids);
        shader.Dispatch(createNodesKernel, 1, 1, 1);
        nodeBuffer.GetData(nodes);
        shader.Dispatch(partitionKernel, particles.Count / 64, 1, 1);
        nodeBuffer.GetData(nodes);
        debugBuffer.GetData(ids);
        //TODO: Are particles in the right subdivision of quadtree? Only in id 1 and 2
    }

    void CreateParticles() {
        particles = new List<Particle>();
        for(int i = 0; i < 64; i++) {
            Particle p = new Particle(Vector2.one * i / 64f, Vector2.zero, Vector2.zero);
            particles.Add(p);
        }
    }

    private void CreateBuffers() {
        particleBuffer = new ComputeBuffer(particles.Count, sizeof(float) * 2 * 3 + sizeof(uint));
        particleBuffer.SetData(particles.ToArray());

        float[] ids = new float[particles.Count];
        for (int i = 0; i < ids.Length; i++) {
            ids[i] = 42u;
        }
        debugBuffer = new ComputeBuffer(particles.Count, sizeof(float));
        debugBuffer.SetData(ids);

        int size = (int)((Mathf.Pow(4, maxTreeDepth) - 1) / 3);
        nodeBuffer = new ComputeBuffer(size, sizeof(uint) * 3 + sizeof(float) * 3);

        Node node;
        node.count = 0;
        node.id = 0;
        node.split = 0;
        node.center = new Vector2(0.5f, 0.5f);
        node.width = 1;
        nodes = new Node[size];
        nodes[0] = node;
        nodeBuffer.SetData(nodes);


        partitionKernel = shader.FindKernel("Partition");
        createNodesKernel = shader.FindKernel("CreateNodes");

        shader.SetBuffer(partitionKernel, "particles", particleBuffer);
        shader.SetBuffer(partitionKernel, "nodes", nodeBuffer);
        shader.SetBuffer(partitionKernel, "debug", debugBuffer);
        shader.SetBuffer(createNodesKernel, "particles", particleBuffer);
        shader.SetBuffer(createNodesKernel, "nodes", nodeBuffer);
        shader.SetBuffer(createNodesKernel, "debug", debugBuffer);
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
    }
}
