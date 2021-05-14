using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GravitySource))]
public class GravitySourceEditor : Editor
{
    public override void OnInspectorGUI() {
        GravitySource source = (GravitySource)target;

        source.type = (GravitySource.Type)EditorGUILayout.EnumPopup("Type", source.type);
        source.gravityStrength = EditorGUILayout.FloatField("Gravity strength", source.gravityStrength);
    }
}
