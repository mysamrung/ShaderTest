Shader "Unlit/WorldConstruct"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
         
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct v2f {
                float4 vertex : SV_POSITION;
            };

            v2f vert (float4 vertex : POSITION) {
                v2f o;
                o.vertex = TransformObjectToHClip(vertex.xyz);
                return o;
            }

            float4 frag (v2f i) : SV_Target { 
                float2 UV = i.vertex.xy / _ScaledScreenParams.xy;
#if UNITY_REVERSED_Z
                float depth = SampleSceneDepth(UV);
#else
                float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
#endif

                float3 worldPos = ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);
                return float4(worldPos, 1);
            }
            ENDHLSL
        }
    }
}
 