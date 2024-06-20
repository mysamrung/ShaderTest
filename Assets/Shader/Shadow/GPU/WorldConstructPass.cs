using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.XR.XRDisplaySubsystem;
using UnityEngine.UIElements;

public class WorldConstructPass : ScriptableRenderPass {
    // FrameDebugger��Profiler�p�̖��O
    private const string ProfilerTag = nameof(WorldConstructPass);
    private readonly ProfilingSampler _profilingSampler = new ProfilingSampler(ProfilerTag);

    // �ǂ̃^�C�~���O�Ń����_�����O���邩
    private readonly RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

    // �ΏۂƂ���RenderQueue
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

    // �����_�����O�����O�ɌĂ΂��
    // �����_�[�^�[�Q�b�g��ς�����ł���
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
        if (resultRenderTexture != null)
            return;

        resultRenderTexture = new RenderTexture(
                                    cameraTextureDescriptor.width, cameraTextureDescriptor.height, 
                                    24, 
                                    RenderTextureFormat.Default, 
                                    RenderTextureReadWrite.Default);
    }

    // �����_�����O����������
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        if (_worldConstructureMaterial == null)
            return;

        CommandBuffer commandBuffer = CommandBufferPool.Get(ProfilerTag);

        var cameraData = renderingData.cameraData;
        // ���ݕ`�悵�Ă���J�����̉𑜓x���@�u_downSample�v�ŏ��Z  
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

    // �����_�����O������ɌĂ΂��
    // �����_�����O�����Ɏg�p�������\�[�X��ЂÂ����肷��
    public override void FrameCleanup(CommandBuffer cmd) {
    }
}
