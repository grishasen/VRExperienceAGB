Shader "VRExperienceAGB/ForestFloor"
{
    Properties {
        _MainTex ("Forest floor", 2D) = "white" {}
        _Color ("Moonlight", Color) = (.34,.40,.48,1)
        _Scale ("Repeats per metre", Float) = .25
    }
    SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Scale;
            CBUFFER_END
            struct Input { float4 vertex:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Output { float4 position:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Output vert(Input v) {
                Output o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 world=TransformObjectToWorld(v.vertex.xyz);
                o.position=TransformWorldToHClip(world); o.uv=world.xz*_Scale; return o;
            }
            half4 frag(Output i):SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                return half4(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).rgb*_Color.rgb,1);
            }
            ENDHLSL
        }
    }
}
