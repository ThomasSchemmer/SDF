using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class TextureRenderer : MonoBehaviour
{
    public static TextureRenderer instance;

    public void Start() {
        instance = this;
    }

    public Material mat;

    public void SetTexture(RenderTexture tex) {
        mat.SetTexture("_Data", tex);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(source, destination, mat);
    }
}
