Shader "VRExperienceAGB/CarezzaScenery"
{
    Properties {
        _MainTex ("Landscape", 2D) = "white" {}
        _Color ("Moonlight tint", Color) = (1,1,1,1)
        _Cutoff ("Silhouette cutoff", Range(0,1)) = .45
        [HideInInspector] _ZWrite ("Depth write", Float) = 1
        [HideInInspector] _SideAtmosphere ("Side atmosphere", Float) = 0
    }
    SubShader {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite [_ZWrite]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "CarezzaPanorama.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Cutoff;
                half _SideAtmosphere;
            CBUFFER_END
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Output { float4 position:SV_POSITION; float2 uv:TEXCOORD0; float2 horizon:TEXCOORD1; half4 color:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            Output vert(Input v) {
                Output o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.position=TransformObjectToHClip(v.vertex.xyz); o.uv=v.uv; o.horizon=v.vertex.xz; o.color=v.color; return o;
            }
            half4 frag(Output i):SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float degrees=atan2(i.horizon.x,-i.horizon.y)*57.29578;
                half side=CarezzaSideBlend(degrees)*_SideAtmosphere;
                float2 uv=_SideAtmosphere>.5?CarezzaPanoramaUV(degrees,i.uv.y):i.uv;
                clip(1-uv.y);
                half4 tex=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);
                clip(tex.a-_Cutoff);
                half3 color=tex.rgb*_Color.rgb*i.color.rgb;
                color=lerp(color,half3(.025,.04,.065),side*.72);
                return half4(color,i.color.a);
            }
            ENDHLSL
        }
    }
}
