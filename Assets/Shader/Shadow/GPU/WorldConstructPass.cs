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
    private ComputeShader _shadowConstructComputeShader;

    private FilteringSettings _filteringSettings;

    public static RenderTexture resultWorldPositionRenderTexture;
    public static RenderTexture resultShadowRenderTexture;

    public WorldConstructPass(Material worldConstructureMaterial, ComputeShader shadowConstructComputeShader) {
        _filteringSettings = new FilteringSettings(_renderQueueRange);
        renderPassEvent = _renderPassEvent;

        _worldConstructureMaterial = worldConstructureMaterial;
        _shadowConstructComputeShader = shadowConstructComputeShader;

        if (resultWorldPositionRenderTexture != null)
            resultWorldPositionRenderTexture.Release();

        if (resultShadowRenderTexture != null)
            resultShadowRenderTexture.Release();
    }

    // レンダリング処理前に呼ばれる
    // レンダーターゲットを変えたりできる
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
        if (resultWorldPositionRenderTexture == null) {
            resultWorldPositionRenderTexture = new RenderTexture(
                                            cameraTextureDescriptor.width, cameraTextureDescriptor.height,
                                            24,
                                            RenderTextureFormat.Default,
                                            RenderTextureReadWrite.Default
                                        );
            resultWorldPositionRenderTexture.enableRandomWrite = true;
            resultWorldPositionRenderTexture.Create();
        } 

        if(resultShadowRenderTexture == null) {
            resultShadowRenderTexture = new RenderTexture(
                                            cameraTextureDescriptor.width, cameraTextureDescriptor.height,
                                            0
                                        );
            resultShadowRenderTexture.enableRandomWrite = true;
            resultShadowRenderTexture.format = RenderTextureFormat.RFloat;
            resultShadowRenderTexture.Create();
        }
    }

    // レンダリング処理を書く
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        if (_worldConstructureMaterial == null)
            return;

        CommandBuffer commandBuffer = CommandBufferPool.Get(ProfilerTag);

        var cameraData = renderingData.cameraData;
        // 現在描画しているカメラの解像度を　「_downSample」で除算  
        int width = cameraData.camera.scaledPixelWidth;
        int height = cameraData.camera.scaledPixelHeight;

        Light[] directionalLights = Light.GetLights(LightType.Directional, 0);

     
        using (new ProfilingScope(commandBuffer, _profilingSampler)) {
            commandBuffer.SetRenderTarget(resultWorldPositionRenderTexture);
            commandBuffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _worldConstructureMaterial);

            // Test
            GameObject shadowCaster = GameObject.FindGameObjectWithTag("TestShadow");
            if (shadowCaster != null) {
                MeshRenderer meshRenderer = shadowCaster.GetComponent<MeshRenderer>();
                MeshFilter meshFilter = shadowCaster.GetComponent<MeshFilter>();
                Mesh mesh = meshFilter.sharedMesh;

                mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw |GraphicsBuffer.Target.Index;

                GraphicsBuffer vertexBuffer = mesh.GetVertexBuffer(0);
                GraphicsBuffer indexBuffer = mesh.GetIndexBuffer();

                int indexCount = (int)mesh.GetIndexCount(0);

                Texture modelTexture = meshRenderer.sharedMaterial.mainTexture;
                if(modelTexture == null)
                    modelTexture = Texture2D.whiteTexture; 

                Vector2 modelTextureSize = modelTexture.texelSize;

                Matrix4x4 worldToobject = Matrix4x4.Transpose(shadowCaster.transform.worldToLocalMatrix);
                Matrix4x4 objectToWorld = shadowCaster.transform.localToWorldMatrix;

                Vector3 localLightDirection = shadowCaster.transform.InverseTransformDirection(directionalLights[0].transform.forward).normalized;

                int csMainKernel = _shadowConstructComputeShader.FindKernel("CSMain");
                _shadowConstructComputeShader.GetKernelThreadGroupSizes(
                    csMainKernel, 
                    out uint numThreadsX, 
                    out uint numThreadsY, 
                    out uint numThreadsZ
                );

                commandBuffer.SetComputeTextureParam(_shadowConstructComputeShader, csMainKernel, "gWorldPositionTexture", resultWorldPositionRenderTexture);
                commandBuffer.SetComputeTextureParam(_shadowConstructComputeShader, csMainKernel, "resultShadowTexture", resultShadowRenderTexture);
                commandBuffer.SetComputeBufferParam(_shadowConstructComputeShader, csMainKernel, "modelVertices", vertexBuffer);
                commandBuffer.SetComputeBufferParam(_shadowConstructComputeShader, csMainKernel, "modelIndices", indexBuffer);
                commandBuffer.SetComputeIntParam(_shadowConstructComputeShader, "indexCount", indexCount);
                commandBuffer.SetComputeTextureParam(_shadowConstructComputeShader, csMainKernel, "modelTexture", modelTexture);
                commandBuffer.SetComputeVectorParam(_shadowConstructComputeShader, "modelTextureSize", modelTextureSize);
                commandBuffer.SetComputeMatrixParam(_shadowConstructComputeShader, "worldToObject", worldToobject);
                commandBuffer.SetComputeMatrixParam(_shadowConstructComputeShader, "objectToWorld", objectToWorld);
                commandBuffer.SetComputeVectorParam(_shadowConstructComputeShader, "localLightDirection", localLightDirection);

                commandBuffer.DispatchCompute(
                    _shadowConstructComputeShader,
                    csMainKernel,
                    Mathf.CeilToInt(width * 1.0f / numThreadsX),
                    Mathf.CeilToInt(height * 1.0f / numThreadsY),
                    1
                );


                string verts = "";
                foreach(var vertex in mesh.vertices) {
                    verts += vertex + "\n";
                }

                string tris = "";
                foreach (var tri in mesh.triangles) {
                    tris += tri + "\n";
                }
                Debug.Log(tris + " Triangles Count" + indexBuffer.count);
                Debug.Log(verts + "  Vertex Count" + vertexBuffer.count);
            }
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
