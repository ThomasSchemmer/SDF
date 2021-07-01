using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TexGen3D : MonoBehaviour
{
    public RenderTexture tex;
    public ComputeShader shader;

    private Material mat;
    private int fillTexKernel;
    private int size = 128;

    public void Start() {
        CreateTexture();
        CreateAndFillShader();
        shader.Dispatch(fillTexKernel, size / 8, size / 8, size / 8);
        TextureRenderer.instance.SetTexture(tex);
    }

    private void CreateTexture() {
        tex = RenderTexture.GetTemporary(size, size, 0);
        tex.enableRandomWrite = true;
        tex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        tex.volumeDepth = size;
        tex.Create();
    }

    private void CreateAndFillShader() {
        fillTexKernel = shader.FindKernel("FillTex");

        shader.SetTexture(fillTexKernel, "result", tex);
        shader.SetInt("size", size);
    }

    private void OnDestroy() {
        RenderTexture.ReleaseTemporary(tex);
    }
}
