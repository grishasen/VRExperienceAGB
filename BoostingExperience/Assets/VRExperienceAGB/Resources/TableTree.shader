Shader "VRExperienceAGB/TableTree"
{
    Properties {
        _MainTex ("Needle edges", 2D) = "white" {}
        _FillTex ("Dense needles", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite On
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_FillTex); SAMPLER(sampler_FillTex);
            struct Input { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Output { float4 position:SV_POSITION; half4 color:TEXCOORD0; float2 uv:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            Output vert(Input v)
            {
                Output o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.position=TransformObjectToHClip(v.vertex.xyz);
                float3 normal=TransformObjectToWorldNormal(v.normal);
                // Solid boughs retain their coverage while the outer sprigs add needle detail.
                half light=.48h+.52h*saturate(dot(normal,normalize(float3(-.4,.75,-.5))));
                o.color=half4(v.color.rgb*light,v.color.a); o.uv=v.uv; return o;
            }
            half4 frag(Output i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 needle=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                clip(lerp(1,needle.a-.26h,i.color.a));
                half3 sprig=needle.rgb*i.color.rgb*half3(10,7,7);
                half3 fill=SAMPLE_TEXTURE2D(_FillTex,sampler_FillTex,i.uv*.45).rgb;
                half3 body=i.color.rgb*max(half3(.30,.30,.30),fill*half3(6,4.5,4.5));
                return half4(lerp(body,sprig,i.color.a),1);
            }
            ENDHLSL
        }
    }
}
