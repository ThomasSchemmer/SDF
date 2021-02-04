using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneSetter : MonoBehaviour
{

    public Camera cam;
    public ComputeShader shader;
    public new Light light;
    public Color shapeColor, shadowColor, bounceColor;
    private Mesh mesh;
    private MeshFilter filter;
    private RenderTexture rt;
    private int width = 1920, height = 1080, kernel;

    private void Start() {
        mesh = new Mesh();
        filter = this.GetComponent<MeshFilter>();
        Init();
    }


    private void OnApplicationQuit() {
        if (rt)
            RenderTexture.ReleaseTemporary(rt);
    }


    private void Init() {
        //create and assign rendertexture
        rt = RenderTexture.GetTemporary(width, height);
        rt.enableRandomWrite = true;
        rt.Create();
        this.GetComponent<Renderer>().material.mainTexture = rt;

        cam = Camera.main;
        mesh = new Mesh();
        filter = this.GetComponent<MeshFilter>();

        //update compute shader
        kernel = shader.FindKernel("main");
        shader.SetTexture(kernel, "result", rt);
        shader.SetVector("sunDirection", light.transform.forward);
        shader.SetVector("shapeColor", shapeColor);
        shader.SetVector("shadowColor", shadowColor);
        shader.SetVector("bounceColor", bounceColor);
    }

    // Update is called once per frame
    void Update() {

        Vector3 v0 = cam.ScreenToWorldPoint(new Vector3(0, 0, cam.nearClipPlane + 0.001f));
        Vector3 v1 = cam.ScreenToWorldPoint(new Vector3(0, Screen.height, cam.nearClipPlane + 0.001f));
        Vector3 v2 = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, cam.nearClipPlane + 0.001f));
        Vector3 v3 = cam.ScreenToWorldPoint(new Vector3(Screen.width, 0, cam.nearClipPlane + 0.001f));
        Vector3 c = (v0 + v1 + v2 + v3) / 4;
        Vector3 d0 = Quaternion.AngleAxis(180, cam.transform.up) * (v0 - cam.transform.position).normalized;
        Vector3 d1 = Quaternion.AngleAxis(180, cam.transform.up) * (v1 - cam.transform.position).normalized;
        Vector3 d2 = Quaternion.AngleAxis(180, cam.transform.up) * (v2 - cam.transform.position).normalized;
        Vector3 d3 = Quaternion.AngleAxis(180, cam.transform.up) * (v3 - cam.transform.position).normalized;

        Vector3 lr = (v3 - v0);
        Vector3 du = (v1 - v0);


        mesh.vertices = new Vector3[4] { v0 - c, v1 - c, v2 - c, v3 - c };
        mesh.triangles = new int[6] { 0, 1, 3, 1, 2, 3};
        mesh.uv = new Vector2[4] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        mesh.RecalculateNormals();
        filter.mesh = mesh;
        this.transform.position = c;

        shader.SetVector("cameraPos", cam.transform.position);
        shader.SetVector("cameraDirX", lr);
        shader.SetVector("cameraDirY", du);
        shader.SetVector("cameraPosLL", v0);
        shader.SetFloat("width", width);
        shader.SetFloat("height", height); 


        shader.Dispatch(kernel, Mathf.CeilToInt(width / 32f), Mathf.CeilToInt(height / 32f), 1);
    }
}
