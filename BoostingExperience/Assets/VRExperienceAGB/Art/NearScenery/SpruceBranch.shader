Shader "VRExperienceAGB/SpruceBranch"
{
    Properties {
        _MainTex ("Needle clusters", 2D) = "white" {}
        _Color ("Moonlight", Color) = (.52,.65,.74,1)
        _Cutoff ("Alpha cutoff", Range(0,1)) = .42
    }
    SubShader {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite On
        AlphaToMask On
        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Cutoff;
            CBUFFER_END
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Output { float4 position:SV_POSITION; float2 uv:TEXCOORD0; half3 tint:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            Output vert(Input v) {
                Output o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.position=TransformObjectToHClip(v.vertex.xyz); o.uv=v.uv; o.tint=v.color.rgb*_Color.rgb; return o;
            }
            half4 frag(Output i):SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 tex=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                clip(tex.a-_Cutoff);
                return half4(tex.rgb*i.tint,tex.a);
            }
            ENDHLSL
        }
    }
}
