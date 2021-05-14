using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Card : MonoBehaviour
{
    private static Camera cam;

    public GameObject target;
    [Range(0, 1.1f)]
    public float t = 0;
    private float lastT = 0;

    private Mesh originalMesh, targetMesh;
    private int[] mapping;
    private bool isMapped;
    private Material mat;
    private int siblingIndex = -1;

    /*  A-----B
     *  |     |
     * C|_____|D
     */

    private Vector3[] corners = new Vector3[] {
        new Vector3(-4.5f, 0,  7),
        new Vector3( 4.5f, 0,  7),
        new Vector3(-4.5f, 0, -7),
        new Vector3( 4.5f, 0, -7)
    };

    void Start()
    {
        if (!cam)
            cam = Camera.main;
        SetRotationUI(0);
        originalMesh = this.GetComponent<MeshFilter>().sharedMesh;
        targetMesh = target.GetComponent<MeshFilter>().sharedMesh;
        mat = this.GetComponent<Renderer>().material;
        mapping = new int[originalMesh.triangles.Length];
        CreateMapping();
        CreateMesh();
    }

    private void Update() {
        if (Mathf.Abs(lastT - t) < 0.01)
            return;
        CreateMesh();

        lastT = t;
    }


    private void CreateMesh() {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector2> uv2s = new List<Vector2>();

        for (int i = 0; i < mapping.Length; i++) {
            Vector3 vertex, normal;
            int triangle;
            Vector2 uv, uv2;
            if (mapping[i] == -1) {
                vertex = originalMesh.vertices[i];
                normal = originalMesh.normals[i];
                triangle = originalMesh.triangles[i];
                uv = originalMesh.uv[i];
                uv2 = new Vector2(Mathf.Clamp(1 - t * 2, 0, 1), 0);
            } else {
                vertex = Vector3.Lerp(originalMesh.vertices[i], targetMesh.vertices[mapping[i]], t);
                normal = Vector3.Lerp(originalMesh.normals[i], targetMesh.normals[mapping[i]], t);
                triangle = t > 0.6f + i / (float)mapping.Length * 0.2f ? i : originalMesh.triangles[i];
                uv = t > 0.6f + i / (float)mapping.Length * 0.2f ? targetMesh.uv[mapping[i]] : originalMesh.uv[i];
                uv2 = new Vector2(1, 0);
            }
            vertices.Add(vertex);
            normals.Add(normal);
            triangles.Add(triangle);
            uvs.Add(uv);
            uv2s.Add(uv2);
        }
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.uv2 = uv2s.ToArray();
        mesh.RecalculateBounds();
        this.GetComponent<MeshFilter>().mesh = mesh;
        mat.SetFloat("_T", t);
    }

    private void CreateMapping() {
        for(int i = 0; i < targetMesh.triangles.Length; i++) {
            mapping[i] = targetMesh.triangles[i];
        }
        for(int i = targetMesh.triangles.Length; i < mapping.Length; i++) {
            mapping[i] = -1;
        }
        isMapped = true;
    }

    public void AlignUI(Vector2 position, float angleZ, int i) {
        SetPositionUI(position, i);
        SetRotationUI(angleZ);
    }

    //https://stackoverflow.com/a/17146376
    public bool ContainsPointUI(Vector2 p) {
        Vector3 a = LocalToScreenPoint(corners[0], this.transform);
        Vector3 b = LocalToScreenPoint(corners[1], this.transform);
        Vector3 c = LocalToScreenPoint(corners[2], this.transform);
        Vector3 d = LocalToScreenPoint(corners[3], this.transform);

        float sumP = Area(a, p, d);
        sumP += Area(d, p, c);
        sumP += Area(c, p, b);
        sumP += Area(p, b, a);

        float sum = Area(a, b, c) + Area(b, c, d);
        return sumP <= sum;
    }


    public void SetPositionUI(Vector3 screenPos, int i) {
        this.transform.position = ScreenPointToWorldOffset(screenPos, i);
    }

    public void SetRotationUI(float angleZ) {
        this.transform.rotation = GetStandardLookRotation(angleZ);
    }


    public void DragUI(float t, Vector3 offset) {
        offset = (1 - t) * offset;
        Vector3 pos = ScreenPointToWorldOffset(Input.mousePosition - offset, -1);
        if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition - offset), out RaycastHit hit))
            t = 0;

        this.transform.position = Vector3.Lerp(pos, hit.point, t);
        Vector3 up = Vector3.Lerp(cam.transform.up, hit.normal, t);
        Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, hit.normal);
        Quaternion projRot = Quaternion.LookRotation(forward, up);
        this.transform.rotation = Quaternion.Lerp(GetStandardLookRotation(0), projRot, t);
        this.t = t;
    }

    private Quaternion GetStandardLookRotation(float angleZ) {
        Vector3 forward = Quaternion.AngleAxis(angleZ, -cam.transform.forward) * cam.transform.up;
        return Quaternion.LookRotation(forward, -cam.transform.forward);
    }

    public static Vector3 ScreenPointToWorldOffset(Vector3 screenP, int i) {
        Vector3 result = cam.ScreenToWorldPoint(screenP);
        result += cam.transform.forward * (1 + 0.1f * i);
        return result;
    }

    public static Vector3 WorldToScreenPoint(Vector3 worldP) {
        return cam.WorldToScreenPoint(worldP);
    }

    public static Vector3 LocalToScreenPoint(Vector3 localP, Transform t) {
        Vector3 worldP = t.TransformPoint(localP);
        return WorldToScreenPoint(worldP);
    }

    private float Area(Vector2 a, Vector2 b, Vector2 c) {
        return Mathf.Abs(
            (b.x * a.y - a.x * b.y) + 
            (c.x * b.y - b.x * c.y) + 
            (a.x * c.y - c.x * a.y))
            / 2f;
    }

    public int GetSiblingIndex() {
        return siblingIndex;
    }

    public void SetSiblingIndex(int index) {
        siblingIndex = index;
    }

    public float GetRotationUI() {
        return Vector3.SignedAngle(this.transform.forward, cam.transform.up, cam.transform.forward);
    }

    public void ResetT() {
        this.t = 0;
    }


}
