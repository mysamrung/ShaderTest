using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WorldConstructPass : ScriptableRenderPass {
    private struct MeshTriangleProperty {
        Vector3 normalRaw;
        float normalArea;
        Vector3 normal;
        float normalLength;

        Vector3 pointA;
        float triangleCALength2;

        Vector3 pointB;
        float ln;

        Vector3 pointC;
        int isIgnore; // 1 : ignore 
    };

    // FrameDebugger��Profiler�p�̖��O
    private const string ProfilerTag = nameof(WorldConstructPass);
    private readonly ProfilingSampler _profilingSampler = new ProfilingSampler(ProfilerTag);

    // �ǂ̃^�C�~���O�Ń����_�����O���邩
    private readonly RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

    // �ΏۂƂ���RenderQueue
    private readonly RenderQueueRange _renderQueueRange = RenderQueueRange.all;

    private Material _worldConstructureMaterial;
    private ComputeShader _shadowConstructComputeShader;

    private FilteringSettings _filteringSettings;

    public static RenderTexture resultWorldPositionRenderTexture;
    public static RenderTexture resultShadowRenderTexture;

    public static ComputeBuffer debugBuffer;
    public static Vector4[] debugData;

    public static bool readBuffer;

    private RTHandle cameraColorTarget;
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

    // �����_�����O�����O�ɌĂ΂��
    // �����_�[�^�[�Q�b�g��ς�����ł���
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
        if (resultWorldPositionRenderTexture == null) {
            resultWorldPositionRenderTexture = new RenderTexture(
                                            cameraTextureDescriptor.width, cameraTextureDescriptor.height,
                                            24,
                                            RenderTextureFormat.Default,
                                            RenderTextureReadWrite.Default
                                        );

            resultWorldPositionRenderTexture.format = RenderTextureFormat.ARGBFloat;
            resultWorldPositionRenderTexture.enableRandomWrite = true;
            resultWorldPositionRenderTexture.Create();
        }

        if (resultShadowRenderTexture == null) {
            resultShadowRenderTexture = new RenderTexture(
                                            cameraTextureDescriptor.width, cameraTextureDescriptor.height,
                                            24,
                                            RenderTextureFormat.Default,
                                            RenderTextureReadWrite.Default
                                        );
            resultShadowRenderTexture.enableRandomWrite = true;
            resultShadowRenderTexture.format = RenderTextureFormat.RFloat;
            resultShadowRenderTexture.Create();
        }


        if (debugData == null) {
            int bufferCount = cameraTextureDescriptor.width * cameraTextureDescriptor.height;
            debugData = new Vector4[bufferCount];
            for (int i = 0; i < bufferCount; i++) {
                debugData[i] = Vector4.zero;
            }
            Debug.Log(bufferCount);
        }

        if (debugBuffer == null) {
            int bufferCount = cameraTextureDescriptor.width * cameraTextureDescriptor.height;
            debugBuffer = new ComputeBuffer(bufferCount, Marshal.SizeOf(typeof(Vector4)), ComputeBufferType.Default);
            debugBuffer.SetData(debugData);
        }
    }

    public void SetupRenderPass(ScriptableRenderer renderer, in RenderingData renderingData) {
        cameraColorTarget = renderer.cameraColorTargetHandle;
    }

    // �����_�����O����������
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        if (_worldConstructureMaterial == null)
            return;

        CommandBuffer commandBuffer = CommandBufferPool.Get(ProfilerTag);

        var cameraData = renderingData.cameraData;
        // ���ݕ`�悵�Ă���J�����̉𑜓x���@�u_downSample�v�ŏ��Z  
        int width = cameraData.camera.scaledPixelWidth;
        int height = cameraData.camera.scaledPixelHeight;

        Light[] directionalLights = Light.GetLights(LightType.Directional, 0);


        using (new ProfilingScope(commandBuffer, _profilingSampler)) {
            commandBuffer.SetRenderTarget(resultWorldPositionRenderTexture);
            commandBuffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _worldConstructureMaterial);

            commandBuffer.SetRenderTarget(cameraColorTarget);

            // Test
            GameObject shadowCaster = GameObject.FindGameObjectWithTag("TestShadow");
            if (shadowCaster != null) {
                MeshRenderer meshRenderer = shadowCaster.GetComponent<MeshRenderer>();
                MeshFilter meshFilter = shadowCaster.GetComponent<MeshFilter>();
                Mesh mesh = meshFilter.sharedMesh;

                mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.Index;

                GraphicsBuffer vertexBuffer = mesh.GetVertexBuffer(0);
                GraphicsBuffer indexBuffer = mesh.GetIndexBuffer();

                int indexCount = (int)mesh.GetIndexCount(0);

                Texture modelTexture = meshRenderer.sharedMaterial.mainTexture;
                if (modelTexture == null)
                    modelTexture = Texture2D.whiteTexture;

                Vector2 screenSize = new Vector2(width, height);
                Vector2 modelTextureSize = modelTexture.texelSize;

                Matrix4x4 worldToobject = shadowCaster.transform.worldToLocalMatrix;
                Matrix4x4 objectToWorld = shadowCaster.transform.localToWorldMatrix;

                Vector3 globalLightDirection = directionalLights[0].transform.forward;
                Vector3 localLightDirection = shadowCaster.transform.InverseTransformDirection(globalLightDirection).normalized;
                localLightDirection.Normalize();

                Vector3 objectCenter = meshRenderer.bounds.center;
                Vector3 objectBound = meshRenderer.bounds.size;

                int csMainKernel = _shadowConstructComputeShader.FindKernel("CSMain");
                _shadowConstructComputeShader.GetKernelThreadGroupSizes(
                    csMainKernel,
                    out uint mainThreadnumThreadsX,
                    out uint mainThreadnumThreadsY,
                    out uint mainThreadnumThreadsZ
                );

                int csPrepareKernel = _shadowConstructComputeShader.FindKernel("CSPrepareMain");
                _shadowConstructComputeShader.GetKernelThreadGroupSizes(
                    csMainKernel,
                    out uint prepareThreadnumThreadsX,
                    out uint prepareThreadnumThreadsY,
                    out uint prepareThreadnumThreadsZ
                );
                ComputeBuffer meshTriangleBuffer = new ComputeBuffer(indexCount / 3, Marshal.SizeOf(typeof(MeshTriangleProperty)), ComputeBufferType.Structured);

                //ComputeBuffer triangleIndexListBuffer = new ComputeBuffer(indexCount, Marshal.SizeOf(typeof(int)), ComputeBufferType.Append);
                //triangleIndexListBuffer.SetCounterValue(0);

                commandBuffer.SetComputeTextureParam(_shadowConstructComputeShader, csMainKernel, "gWorldPositionTexture", resultWorldPositionRenderTexture);
                commandBuffer.SetComputeTextureParam(_shadowConstructComputeShader, csMainKernel, "resultShadowTexture", resultShadowRenderTexture);
                commandBuffer.SetComputeVectorParam(_shadowConstructComputeShader, "screenSize", screenSize);

                commandBuffer.SetComputeBufferParam(_shadowConstructComputeShader, csPrepareKernel, "modelVertices", vertexBuffer);
                commandBuffer.SetComputeBufferParam(_shadowConstructComputeShader, csPrepareKernel, "modelIndices", indexBuffer);

                commandBuffer.SetComputeIntParam(_shadowConstructComputeShader, "vertexStride", vertexBuffer.stride);
                commandBuffer.SetComputeIntParam(_shadowConstructComputeShader, "indexCount", indexCount);

                commandBuffer.SetComputeTextureParam(_shadowConstructComputeShader, csMainKernel, "modelTexture", modelTexture);
                commandBuffer.SetComputeVectorParam(_shadowConstructComputeShader, "modelTextureSize", modelTextureSize);

                commandBuffer.SetComputeMatrixParam(_shadowConstructComputeShader, "worldToObject", worldToobject);
                commandBuffer.SetComputeMatrixParam(_shadowConstructComputeShader, "objectToWorld", objectToWorld);

                commandBuffer.SetComputeVectorParam(_shadowConstructComputeShader, "objectCenter", objectCenter);
                commandBuffer.SetComputeVectorParam(_shadowConstructComputeShader, "objectBound", objectBound / 2);

                commandBuffer.SetComputeVectorParam(_shadowConstructComputeShader, "localLightDirection", localLightDirection);
                commandBuffer.SetComputeVectorParam(_shadowConstructComputeShader, "globalLightDirection", globalLightDirection);

                commandBuffer.SetComputeBufferParam(_shadowConstructComputeShader, csMainKernel, "debugBuffer", debugBuffer);

                commandBuffer.SetComputeBufferParam(_shadowConstructComputeShader, csMainKernel, "meshTriangleBuffer", meshTriangleBuffer);
                commandBuffer.SetComputeBufferParam(_shadowConstructComputeShader, csPrepareKernel, "meshTriangleBuffer", meshTriangleBuffer);

                commandBuffer.DispatchCompute(
                    _shadowConstructComputeShader,
                    csPrepareKernel,
                    Mathf.CeilToInt((indexCount / 3.0f) / prepareThreadnumThreadsX),
                    1,
                    1
                );

                commandBuffer.SetComputeIntParam(_shadowConstructComputeShader, "indexCount", (indexCount / 3));
                Debug.Log(indexCount);

                commandBuffer.DispatchCompute(
                    _shadowConstructComputeShader,
                    csMainKernel,
                    Mathf.CeilToInt(width * 1.0f / mainThreadnumThreadsX),
                    Mathf.CeilToInt(height * 1.0f / mainThreadnumThreadsY),
                    1
                );

                if (readBuffer) {
                    debugBuffer.GetData(debugData);
                    readBuffer = false;
                }


                string verts = "";
                foreach (var vertex in mesh.vertices) {
                    verts += vertex + "\n";
                }

                string tris = "";
                foreach (var tri in mesh.triangles) {
                    tris += tri + "\n";
                }
                //Debug.Log(tris + " Triangles Count" + indexBuffer.count);
                //Debug.Log(verts + "  Vertex Count" + vertexBuffer.count);
            }
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
