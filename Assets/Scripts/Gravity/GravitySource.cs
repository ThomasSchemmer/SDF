using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravitySource : MonoBehaviour
{

    public enum Type { PLANE, SPHERE}


    public float gravityStrength = 9.81f;
    public Type type = Type.SPHERE;
    public float range = 1;



    public Vector3 GetGravity(Vector3 position, out bool isInRange) {
        Vector3 grav;
        switch (type) {
            case Type.SPHERE: grav = (transform.position - position).normalized * gravityStrength; break;
            case Type.PLANE: grav = -transform.up.normalized * gravityStrength; break;
            default: grav = Physics.gravity; break; 
        }
        float distance = Mathf.Abs(Vector3.Dot(-grav.normalized, position - transform.position));
        if (distance > range) {
            isInRange = false;
            return Vector3.zero;
        }
        isInRange = true;
        return grav * (1 - distance/range);
    }

    public Vector3 GetUpAxis(Vector3 position, out bool isInRange) {
        return -GetGravity(position, out isInRange);
    }

    public Vector3 GetGravity(Vector3 position, out Vector3 upAxis) {
        upAxis = GetUpAxis(position, out bool upInRange);
        return GetGravity(position, out bool gravInRange);
    }

    private void OnEnable() {
        CustomGravity.Register(this);
    }

    private void OnDisable() {
        CustomGravity.Unregister(this);
    }


    private void OnDrawGizmos() {
        Gizmos.color = Color.cyan;
        
        switch (type) {
            case Type.PLANE: {
                    Vector3 scale = transform.localScale;
                    Vector3 size = new Vector3(10f, 0f, 10f);
                    scale.y = range;
                    Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
                    Gizmos.DrawWireCube(Vector3.up, size); break;
                }
            case Type.SPHERE: {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireSphere(-transform.position, range + 20); break;
                }
        }
        
    }
}
