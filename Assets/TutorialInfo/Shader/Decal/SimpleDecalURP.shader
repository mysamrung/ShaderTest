Shader "Unlit/SimpleDecalURP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            struct v2f
            {
                float4 vertex : SV_POSITION;
            };
            sampler2D _MainTex;
            float4 _MainTex_ST;
            v2f vert (float4 vertex : POSITION)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(vertex.xyz);
             
                return o;
            }
            float4 frag (v2f i) : SV_Target
            { 
                float2 UV = i.vertex.xy / _ScaledScreenParams.xy;
#if UNITY_REVERSED_Z
                float depth = SampleSceneDepth(UV);
#else
                // Adjust Z to match NDC for OpenGL ([-1, 1])
                float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
#endif

                float3 worldPos = ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);
                float3 localPos = mul(unity_WorldToObject, float4(worldPos, 1));

                float3 decalClipFade = saturate((0.5 - abs(localPos.xyz)) * 125);
                float clipFade = decalClipFade.x * decalClipFade.y * decalClipFade.z;

                clip(clipFade - 0.5);

                float depthL = unity_WorldToObject._m22;
                //Test
                float3 ldirection = float3(0, -1, 0);
                float3 center = unity_WorldToObject._m03_m13_m23;
                center -= ldirection * depthL * 0.5;
                float dotL = dot(worldPos - center, ldirection);
                dotL = dotL / depthL * 2;

                float2 textureUV = (localPos.xz + 0.5) * _MainTex_ST.xy + _MainTex_ST.zw;
                float4 mainColor = tex2D(_MainTex, textureUV);
                mainColor.a *= clipFade;

                return step(1 - mainColor.r, dotL);
                
                return mainColor;
            }
            ENDHLSL
        }
    }
}
 