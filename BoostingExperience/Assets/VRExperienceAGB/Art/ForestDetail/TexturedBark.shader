Shader "VRExperienceAGB/TexturedBark"
{
    Properties {
        _MainTex ("Bark", 2D) = "white" {}
        _Color ("Moonlight", Color) = (.38,.42,.48,1)
        _VerticalRepeat ("Vertical repeat", Float) = 5
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
                float _VerticalRepeat;
            CBUFFER_END
            struct Input {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Output {float4 position:SV_POSITION;float2 uv:TEXCOORD0;half3 tint:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
            Output vert(Input v) {
                Output o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.position=TransformObjectToHClip(v.vertex.xyz);o.uv=v.uv*float2(1,_VerticalRepeat);
                half light=.65+.35*saturate(dot(TransformObjectToWorldNormal(v.normal),normalize(half3(-.4,.8,-.3))));
                o.tint=_Color.rgb*light;return o;
            }
            half4 frag(Output i):SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                return half4(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).rgb*i.tint,1);
            }
            ENDHLSL
        }
    }
}
