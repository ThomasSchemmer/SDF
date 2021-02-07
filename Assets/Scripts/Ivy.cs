using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ivy : MonoBehaviour
{
    public Material mat;
    public float speed = 1f;

    Mesh mesh;
    MeshFilter filter;
    MeshRenderer renderer;


    float width = 0.01f;
    float maxWidth = 0.05f;
    float length = 0.1f;
    int maxChildren = 15;

    class BranchInfo {
        public Vector3 startPos, endPos;
        public int start;
        public float length;
        public float width;
        public float maxWidth;
        public bool isGrowing;
        public int maxChildren;

        public BranchInfo(Vector3 sp, Vector3 ep, int s, float l, float w, float maxW, int c) {
            startPos = sp;
            endPos = ep;
            start = s;
            length = l;
            width = w;
            maxWidth = maxW;
            isGrowing = true;
            maxChildren = c;
        }
    }
    List<BranchInfo> branches;

    void Start() {
        branches = new List<BranchInfo>();
        renderer = gameObject.AddComponent<MeshRenderer>();
        filter = gameObject.AddComponent<MeshFilter>();
        mesh = new Mesh();
        mesh.MarkDynamic();
        filter.mesh = mesh;
        renderer.material = mat;

        CreateBranch(Vector3.zero, GetRandomVector(), maxChildren, width, maxWidth);
    }

    void Update()
    {
        for(int i = 0; i < branches.Count; i++) {
            if (branches[i].isGrowing) {
                branches[i].width += 0.01f * Time.deltaTime * speed;
                branches[i].length += 0.1f * Time.deltaTime * speed;
                Grow(branches[i]);
                if (branches[i].width > branches[i].maxWidth ) {
                    branches[i].isGrowing = false;
                    if(branches[i].maxChildren > 0)
                        Split(branches[i]);
                }

            }
        }
    }

    private void Split(BranchInfo info) {
        int s = info.start;
        Vector3 c =   (mesh.vertices[s + 4] + mesh.vertices[s + 7]) / 2f;
        Vector3 c1 =  (mesh.vertices[s + 5] + mesh.vertices[s + 6]) / 2f;
        Vector3 dir = (info.endPos - info.startPos).normalized;

        float dist = Mathf.Sqrt(3) / 2 * info.maxWidth;
        Vector3 v0 = c + dir * dist;
        Vector3 v1 = c1 + dir * dist;

        List<Vector3> vertices = new List<Vector3>(mesh.vertices);
        List<int> triangles = new List<int>(mesh.triangles);

        int newStart = vertices.Count;
        vertices.AddRange(new Vector3[] {
            mesh.vertices[s + 4],
            mesh.vertices[s + 5],
            mesh.vertices[s + 6],
            mesh.vertices[s + 7],
            v0, 
            v1
        });
        s = newStart;

        //top/bottom triangles
        triangles.AddRange(new int[] { s + 0, s + 3, s + 4 });
        triangles.AddRange(new int[] { s + 1, s + 5, s + 2 });
        //right
        triangles.AddRange(new int[] { s + 0, s + 5, s + 1 });
        triangles.AddRange(new int[] { s + 0, s + 4, s + 5 });
        //left
        triangles.AddRange(new int[] { s + 5, s + 3, s + 2 });
        triangles.AddRange(new int[] { s + 5, s + 4, s + 3 });

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.MarkModified();

        //left / right center, new positions
        Vector3 s0 = (mesh.vertices[s + 2] + mesh.vertices[s + 3] + mesh.vertices[s + 4] + mesh.vertices[s + 5]) / 4;
        Vector3 s1 = (mesh.vertices[s + 0] + mesh.vertices[s + 1] + mesh.vertices[s + 5] + mesh.vertices[s + 4]) / 4;
        //left / right normals
        Vector3 n0 = (s0 - (c + c1) / 2).normalized;
        Vector3 n1 = (s1 - (c + c1) / 2).normalized;

        float maxWidth = info.width * 3 / 4f;
        float mC0 = Random.Range(0, 1f);
        float mC1 = Random.Range(0, 1f);
        if(Random.Range(0, 1f) > 0.15f)
            CreateBranch(s0, n0, (int)(info.maxChildren * mC0), maxWidth * 0.5f, maxWidth);
        if (Random.Range(0, 1f) > 0.05f)                                                       
            CreateBranch(s1, n1, (int)(info.maxChildren * mC1), maxWidth * 0.5f, maxWidth);
    }

    private void CreateBranch(Vector3 pos, Vector3 dir, int mc, float width, float maxW) {
        int s = mesh.vertexCount;
        List<Vector3> vertices = new List<Vector3>(mesh.vertices);
        List<int> triangles = new List<int>(mesh.triangles);
        Vector3 norm = Vector3.Cross(dir, Vector3.up).normalized;
        Vector3 norm2 = Vector3.Cross(dir, norm).normalized;
        Vector3 pos2 = pos + dir * length;

        Vector3 p0 = pos + norm * width + norm2 * width;
        Vector3 p1 = pos + norm * width - norm2 * width;
        Vector3 p2 = pos - norm * width - norm2 * width;
        Vector3 p3 = pos - norm * width + norm2 * width;

        float w2 = width * 3 / 4f;
        Vector3 p4 = pos2 + norm * w2 + norm2 * w2;
        Vector3 p5 = pos2 + norm * w2 - norm2 * w2;
        Vector3 p6 = pos2 - norm * w2 - norm2 * w2;
        Vector3 p7 = pos2 - norm * w2 + norm2 * w2;
        vertices.AddRange(new Vector3[] { p0, p1, p2, p3, p4, p5, p6, p7 });

        triangles.AddRange(new int[] { s + 0, s + 1, s + 2 });
        triangles.AddRange(new int[] { s + 2, s + 3, s + 0 });
        triangles.AddRange(new int[] { s + 4, s + 6, s + 5 });
        triangles.AddRange(new int[] { s + 6, s + 4, s + 7 });
               
        triangles.AddRange(new int[] { s + 0, s + 5, s + 1 });
        triangles.AddRange(new int[] { s + 0, s + 4, s + 5 });
        triangles.AddRange(new int[] { s + 3, s + 7, s + 4 });
        triangles.AddRange(new int[] { s + 3, s + 4, s + 0 });
        triangles.AddRange(new int[] { s + 3, s + 2, s + 7 });
        triangles.AddRange(new int[] { s + 2, s + 6, s + 7 });
        triangles.AddRange(new int[] { s + 2, s + 1, s + 6 });
        triangles.AddRange(new int[] { s + 6, s + 1, s + 5 });

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        branches.Add(new BranchInfo(pos, pos2, s, length, width, maxW, mc));

    }

    private void Grow(BranchInfo info) {
        Vector3[] vertices = mesh.vertices;
        Vector3 c0 = Vector3.zero, c1 = Vector3.zero;
        for (int i = info.start; i < info.start + 4; i++) {
            c0 += vertices[i];
            c1 += vertices[i + 4];
        }
        c0 /= 4;
        c1 /= 4;
        Vector3 dir = (c1 - c0).normalized;
        Vector3 norm = Vector3.Cross(dir, Vector3.up).normalized;
        Vector3 norm2 = Vector3.Cross(dir, norm).normalized;

        vertices[info.start] =     c0 + norm * info.width + norm2 * info.width;
        vertices[info.start + 1] = c0 + norm * info.width - norm2 * info.width;
        vertices[info.start + 2] = c0 - norm * info.width - norm2 * info.width;
        vertices[info.start + 3] = c0 - norm * info.width + norm2 * info.width;

        float w2 = info.width * 3 / 4f;
        vertices[info.start + 4] = c0 + norm * w2 + norm2 * w2 + dir * info.length;
        vertices[info.start + 5] = c0 + norm * w2 - norm2 * w2 + dir * info.length;
        vertices[info.start + 6] = c0 - norm * w2 - norm2 * w2 + dir * info.length;
        vertices[info.start + 7] = c0 - norm * w2 + norm2 * w2 + dir * info.length;
        mesh.vertices = vertices;
        mesh.MarkModified();
    }

    

    Vector3 GetRandomVector() {
        return new Vector3(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f)).normalized;
    }

    Vector3 GetSpotlightVector(Vector3 position, Vector3 dir) {
        float radius = Mathf.Tan(Mathf.Deg2Rad * 45 / 2) * 2;
        Vector2 circle = Random.insideUnitCircle * radius;
        Vector3 target = position + dir * 2 + Quaternion.LookRotation(dir) * new Vector3(circle.x, circle.y);
        return (target - position).normalized;
    }
}
