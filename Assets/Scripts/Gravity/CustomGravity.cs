using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomGravity 
{
    private static List<GravitySource> sources = new List<GravitySource>();
    private static Vector3 lastUpVector = Vector3.up;

    public static Vector3 GetGravity(Vector3 position) {
        Vector3 res = new Vector3();
        bool totalInRange = false;
        foreach (var source in sources) {
            res += source.GetGravity(position, out bool sourceInRange);
            totalInRange |= sourceInRange;
        }
        return res;
    }

    public static Vector3 GetUpAxis(Vector3 position) {
        Vector3 res = new Vector3();
        bool totalInRange = false;
        foreach (var source in sources) {
            res += source.GetUpAxis(position, out bool sourceInRange);
            totalInRange |= sourceInRange;
        }
        res = res.normalized;
        if (totalInRange)
            lastUpVector = res;
        return lastUpVector;
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
