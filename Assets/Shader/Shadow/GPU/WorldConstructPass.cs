using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.XR.XRDisplaySubsystem;
using UnityEngine.UIElements;

public class WorldConstructPass : ScriptableRenderPass {
    // FrameDebuggerやProfiler用の名前
    private const string ProfilerTag = nameof(WorldConstructPass);
    private readonly ProfilingSampler _profilingSampler = new ProfilingSampler(ProfilerTag);

    // どのタイミングでレンダリングするか
    private readonly RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

    // 対象とするRenderQueue
    private readonly RenderQueueRange _renderQueueRange = RenderQueueRange.all;

    private Material _worldConstructureMaterial;
    private FilteringSettings _filteringSettings;

    public static RenderTexture resultRenderTexture;

    public WorldConstructPass(Material worldConstructureMaterial) {
        _filteringSettings = new FilteringSettings(_renderQueueRange);
        renderPassEvent = _renderPassEvent;

        _worldConstructureMaterial = worldConstructureMaterial;

        if (resultRenderTexture != null)
            resultRenderTexture.Release();
    }

    // レンダリング処理前に呼ばれる
    // レンダーターゲットを変えたりできる
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
        if (resultRenderTexture != null)
            return;

        resultRenderTexture = new RenderTexture(
                                    cameraTextureDescriptor.width, cameraTextureDescriptor.height, 
                                    24, 
                                    RenderTextureFormat.Default, 
                                    RenderTextureReadWrite.Default);
    }

    // レンダリング処理を書く
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        if (_worldConstructureMaterial == null)
            return;

        CommandBuffer commandBuffer = CommandBufferPool.Get(ProfilerTag);

        var cameraData = renderingData.cameraData;
        // 現在描画しているカメラの解像度を　「_downSample」で除算  
        var w = cameraData.camera.scaledPixelWidth;
        var h = cameraData.camera.scaledPixelHeight;

        using (new ProfilingScope(commandBuffer, _profilingSampler)) {
            commandBuffer.SetRenderTarget(new RenderTargetIdentifier(resultRenderTexture, 0, CubemapFace.Unknown, -1));
            commandBuffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _worldConstructureMaterial);
        }

        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();


        CommandBufferPool.Release(commandBuffer);
    }

    // レンダリング処理後に呼ばれる
    // レンダリング処理に使用したリソースを片づけたりする
    public override void FrameCleanup(CommandBuffer cmd) {
    }
}
