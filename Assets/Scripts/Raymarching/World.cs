using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public Vector3 size;
    public float sections = 16f;
    public float scale;
    public Material mat;

    GameObject[,,] chunks;

    void Start() {
        chunks = new GameObject[(int) size.x, (int) size.y, (int) size.z];
        for (int x = 0; x < size.x; x++) {
            for(int y = 0; y < size.y; y++) {
                for (int z = 0; z < size.z; z++) {
                    chunks[x, y, z] = CreateChunk(new Vector3(x, y, z) * scale * sections, scale);
                }
            }   
        }
    }

    private GameObject CreateChunk(Vector3 center, float scale) {
        GameObject obj = new GameObject();
        obj.transform.position = this.transform.position + center;
        obj.transform.parent = this.transform;
        obj.name = "Chunk " + center.x + " " + center.y + " " + center.z;
        obj.AddComponent<MeshRenderer>().material = mat;
        obj.AddComponent<MeshFilter>();

        Chunk chunk = obj.AddComponent<Chunk>();
        chunk.scale = this.scale;
        chunk.sections = this.sections;

        return obj;
    }

}
