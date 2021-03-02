using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


[CreateAssetMenu(menuName = "Rendering/ShadowPipeline")]
public class ShadowPipelineAsset : RenderPipelineAsset {
    public ComputeShader computeShader;
    public Material mat;

    private ShadowPipeline pipeline; 

    protected override RenderPipeline CreatePipeline() {
        if (pipeline != null)
            pipeline.ReleaseShader();
        pipeline = new ShadowPipeline(computeShader, mat);
        return pipeline;
    }

}
