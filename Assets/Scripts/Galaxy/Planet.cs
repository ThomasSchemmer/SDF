using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Planet : MonoBehaviour
{
    //GENERATION
    public ComputeShader shader;
    public RenderTexture rt;
    public RawImage rawImage;

    private ComputeBuffer particleBuffer;
    private int drawKernel, updateKernel, clearKernel;
    private int width = 512, height = 512;
    private Camera cam;

    //PLANET
    private List<Particle> particles = new List<Particle>();
    private int maxParticles = 51200;
    private float gravity = 3;




    void Start()
    {
        if (!cam)
            cam = Camera.main;
        CreateParticles();
        CreateRT();
        CreateBuffers();
        rawImage = GameObject.Find("Canvas/RawImage").GetComponent<RawImage>();
        rawImage.texture = rt;
    }

    void Update()
    {
        shader.SetFloat("deltaTime", Time.deltaTime);
        shader.Dispatch(clearKernel, 16, 16, 1);
        shader.Dispatch(updateKernel, 16, 16, 1);
        shader.Dispatch(drawKernel, 16, 16, 1);   
       
        
    }

    private void CreateRT() {
        if (rt)
            return;
        rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        rt.enableRandomWrite = true;
        rt.Create();
    }

    private void OnDisable() {
        if (rt)
            RenderTexture.ReleaseTemporary(rt);
        if (particleBuffer != null)
            particleBuffer.Release();
    }

    private void CreateBuffers() {
        particleBuffer = new ComputeBuffer(particles.Count, sizeof(float) * 2 * 3);
        Particle[] arr = particles.ToArray();
        particleBuffer.SetData(arr);

        drawKernel = shader.FindKernel("Draw");
        updateKernel = shader.FindKernel("Update");
        clearKernel = shader.FindKernel("Clear");
        shader.SetTexture(drawKernel, "Result", rt);
        shader.SetTexture(updateKernel, "Result", rt);
        shader.SetTexture(clearKernel, "Result", rt);
        shader.SetBuffer(drawKernel, "particles", particleBuffer);
        shader.SetBuffer(updateKernel, "particles", particleBuffer);
        shader.SetInt("width", width);
        shader.SetInt("height", height);
        shader.SetInt("groupX", 16);
        shader.SetInt("groupY", 16);

        shader.SetVector("worldExtent", new Vector2(5, 5));
        //shader.SetVector("worldExtent", cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0)));
        shader.SetInt("particleAmount", particles.Count);
    }

    private void CreateParticles() {
        for (int i = 0; i < maxParticles / 2f; i++) {
            CreateRandomParticle();
        }
    }

    


    Vector2 WorldToPixel(Vector2 worldPos) {
        Vector2 scale = new Vector2(worldPos.x / 5, worldPos.y / 5);
        return new Vector2((1 + scale.x) * 256 / 2, (1 + scale.y) * 256 / 2);
    }

    private void CreateRandomParticle() {
        float angle = Random.Range(0f, 1f) * Mathf.PI * 2;
        float gravity = Random.Range(0.75f * this.gravity, 1.25f * this.gravity);
        Vector2 pos = new Vector2(
            Mathf.Cos(angle),
            Mathf.Sin(angle)
            );
        Vector2 velocity = new Vector2(pos.y, -pos.x);

        Particle p = new Particle(
            this.transform.position + (Vector3)pos * gravity,
            RotateVelocity(velocity),
            this.transform.position
            ) ; 
        particles.Add(p);
    }

    private Vector2 RotateVelocity(Vector2 input) {
        float alpha = Random.Range(-15.0f, 15.0f) * Mathf.Deg2Rad;
        float x = Mathf.Cos(alpha) * input.x - Mathf.Sin(alpha) * input.y;
        float y = Mathf.Sin(alpha) * input.x + Mathf.Cos(alpha) * input.y;
        return new Vector2(x, y);
    }

}
