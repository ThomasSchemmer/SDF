using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravitySource : MonoBehaviour
{

    public enum Type { SPHERE}


    public float gravityStrength = 9.81f;
    public Type type = Type.SPHERE;
    public Vector3 origin;


    public Vector3 GetGravity(Vector3 position) {
        switch (type) {
            case Type.SPHERE: return (origin - position).normalized * gravityStrength;
            default: return Physics.gravity; 
        }
    }

    public Vector3 GetUpAxis(Vector3 position) {
        return -GetGravity(position).normalized;
    }

    public Vector3 GetGravity(Vector3 position, out Vector3 upAxis) {
        upAxis = GetUpAxis(position);
        return GetGravity(position);
    }

    private void OnEnable() {
        CustomGravity.Register(this);
    }

    private void OnDisable() {
        CustomGravity.Unregister(this);
    }
}
