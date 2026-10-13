Shader "VRExperienceAGB/CarezzaGround"
{
    Properties {
        _MainTex ("Surface", 2D) = "white" {}
        _Color ("Moonlight", Color) = (.28,.34,.42,1)
        _Scale ("Texture scale", Float) = .4
    }
    SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off
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
            struct Input { float4 vertex:POSITION; float3 normal:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Output { float4 position:SV_POSITION; float3 world:TEXCOORD0; half3 normal:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            Output vert(Input v) {
                Output o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world=TransformObjectToWorld(v.vertex.xyz);o.normal=TransformObjectToWorldNormal(v.normal);
                o.position=TransformWorldToHClip(o.world);return o;
            }
            half4 frag(Output i):SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n=normalize(i.normal), weights=pow(abs(n),4);weights/=max(dot(weights,half3(1,1,1)),.001);
                float3 p=i.world*_Scale;
                half3 tex=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,p.yz).rgb*weights.x+
                    SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,p.xz).rgb*weights.y+
                    SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,p.xy).rgb*weights.z;
                half light=.6+.4*saturate(dot(n,normalize(half3(-.4,.8,-.3))));
                return half4(tex*_Color.rgb*light,1);
            }
            ENDHLSL
        }
    }
}
