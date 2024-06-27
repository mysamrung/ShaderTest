using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.XR.XRDisplaySubsystem;
using UnityEngine.UIElements;
using UnityEditor.SearchService;

public class WorldConstructFeature : ScriptableRendererFeature {
    public Material worldConstructureMaterial;
    public ComputeShader shadowConstructComputeShader;

    private WorldConstructPass m_RenderPass = null;
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if (renderingData.cameraData.cameraType == CameraType.Game) {
            //Calling ConfigureInput with the ScriptableRenderPassInput.Color argument ensures that the opaque texture is available to the Render Pass
            m_RenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
            renderer.EnqueuePass(m_RenderPass);
        }
    }

    public override void Create() {
        m_RenderPass = new WorldConstructPass(worldConstructureMaterial, shadowConstructComputeShader);
    }

    protected override void Dispose(bool disposing) {
    }
}
