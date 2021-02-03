using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneSetter : MonoBehaviour
{

    public Camera cam;
    public new Light light;
    private Mesh mesh;
    private MeshFilter filter;
    private MeshRenderer rend;

    private void Start() {
        mesh = new Mesh();
        filter = this.GetComponent<MeshFilter>();
        rend = this.GetComponent<MeshRenderer>();

        Matrix4x4 m = Matrix4x4.Rotate(Quaternion.AngleAxis(90, new Vector3(0, 0, 1)));
        rend.material.SetMatrix("rotMatrix", m);
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
        mesh.vertices = new Vector3[4] { v0 - c, v1 - c, v2 - c, v3 - c };
        mesh.triangles = new int[6] { 0, 1, 3, 1, 2, 3};
        mesh.uv = new Vector2[4] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        mesh.tangents = new Vector4[4] { d0, d1, d2, d3};
        mesh.RecalculateNormals();
        filter.mesh = mesh;
        this.transform.position = c;

        rend.material.SetVector("_CameraPosition", cam.transform.position);
        rend.material.SetVector("_CameraDirection", cam.transform.forward);
        rend.material.SetVector("_SunDirection", light.transform.forward);
    }
}
