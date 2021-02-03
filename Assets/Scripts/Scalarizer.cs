using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// SDF to mesh converter
// adapted from http://paulbourke.net/geometry/polygonise/
// eg uses the same edge ordering and edge indexing
public class Scalarizer : MonoBehaviour {
    public Material mat;
    public float scale = 1f;
    Camera _cam;
    Mesh mesh;
    float threshold = 0.01f;
    Vector3 lastPos;
    float lastScale;


    Vector3[] positions = new Vector3[8]{
        0.5f * new Vector3(-1, -1, 1),
        0.5f * new Vector3(1, -1, 1),
        0.5f * new Vector3(1, -1, -1),
        0.5f * new Vector3(-1, -1, -1),
        0.5f * new Vector3(-1, 1, 1),
        0.5f * new Vector3(1, 1, 1),
        0.5f * new Vector3(1, 1, -1),
        0.5f * new Vector3(-1, 1, -1)
    };

    Vector2[] edges = new Vector2[12] {
        new Vector2(0, 1),
        new Vector2(1, 2),
        new Vector2(2, 3),
        new Vector2(3, 0),
        new Vector2(4, 5),
        new Vector2(5, 6),
        new Vector2(6, 7),
        new Vector2(7, 4),
        new Vector2(0, 4),
        new Vector2(1, 5),
        new Vector2(2, 6),
        new Vector2(3, 7),
    };

    void Start()
    {
        _cam = Camera.main;
        lastPos = Vector3.one * float.MaxValue;
        lastScale = float.MaxValue;
    }


    void Update()
    {
        if (!_cam)
            _cam = Camera.main;
        if (!HasChanged())
            return;
        Debug.Log("Redrawing");
        lastPos = this.transform.position;
        lastScale = scale;
        CreateMesh();
    }

    private bool HasChanged() {
        return ((lastPos - this.transform.position).magnitude > threshold || Mathf.Abs(lastScale - scale) > threshold);
    }

    private void CreateMesh() {
        mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> tris = new List<int>();

        //create the chunk, scale the offset! 
        for(int x = 0; x < 16; x++) {
            for(int y = 0; y < 16; y++) {
                for(int z = 0; z < 16; z++) {
                    AddCube(new Vector3(x, y, z) * scale, ref vertices, ref tris);
                }
            }
        }
        mesh.vertices = vertices.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        this.GetComponent<MeshFilter>().mesh = mesh;
    }

    private void AddCube(Vector3 center, ref List<Vector3> vertices, ref List<int> tris) {
        int index = 0;
        //get the edge index position according to the function values
        for (int i = 0; i < positions.Length; i++) {
            float value = Defines.Map(positions[i] * scale + this.transform.position + center);
            index += value > 0.5 ? 1 << i : 0;
        }
        int[] edgesIndices = Defines.triTable[index];
        for (int i = 0; i < edgesIndices.Length; i += 3) {
            if (edgesIndices[i] == -1)
                break;

            int c = vertices.Count;
            //adapt each vertex to where the value actually crosses, not just in the middle of the edge
            Vector3 p0 = GetVertexOnEdge(center, edgesIndices, i);
            Vector3 p1 = GetVertexOnEdge(center, edgesIndices, i + 1);
            Vector3 p2 = GetVertexOnEdge(center, edgesIndices, i + 2);
            vertices.AddRange(new Vector3[] { p0, p1, p2 });
            tris.AddRange(new int[] { c, c + 1, c + 2 });
        }
    }

    private Vector3 GetVertexOnEdge(Vector3 center, int[] edgesIndices, int i) {
        //proportional offset: calculate where the value would exactly be the threshold, if we assume a linear growth
        //this linear growth is - in most cases - wrong for SDF, but its much simpler to compute
        Vector3 p0 = positions[(int)edges[edgesIndices[i]].x] * scale + center;
        Vector3 p1 = positions[(int)edges[edgesIndices[i]].y] * scale + center;
        float v0 = Defines.Map(p0 + transform.position);
        float v1 = Defines.Map(p1 + transform.position);
        float d = (0.5f - v0) / (v1 - v0);
        Vector3 p = p0 + (p1 - p0) * d;
        return p;
    }


    private void OnDrawGizmos() {
        Gizmos.DrawWireCube(this.transform.position, Vector3.one);
    }
}
