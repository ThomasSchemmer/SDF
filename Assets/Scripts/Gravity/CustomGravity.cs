using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomGravity 
{
    private static List<GravitySource> sources = new List<GravitySource>();


    public static Vector3 GetGravity(Vector3 position) {
        Vector3 res = new Vector3();
        foreach(var source in sources) {
            res += source.GetGravity(position);
        }
        return res;
    }

    public static Vector3 GetUpAxis(Vector3 position) {
        Vector3 res = new Vector3();
        foreach (var source in sources) {
            res += source.GetUpAxis(position);
        }
        return res;
    }

    public static Vector3 GetGravity(Vector3 position, out Vector3 upAxis) {
        upAxis = GetUpAxis(position);
        return GetGravity(position);
    }

    public static void Register(GravitySource source) {
        sources.Add(source);
    }

    public static void Unregister(GravitySource source) {
        sources.Remove(source);
    }
}
