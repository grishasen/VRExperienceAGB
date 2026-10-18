Shader "VRExperienceAGB/CarezzaWater"
{
    Properties {
        _MainTex ("Distant scenery reflection", 2D) = "black" {}
        _Color ("Water", Color) = (.025,.065,.08,1)
    }
    SubShader {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite On
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
            CBUFFER_END
            struct Input { float4 vertex:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Output { float4 position:SV_POSITION; float3 world:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Output vert(Input v) {
                Output o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world=TransformObjectToWorld(v.vertex.xyz); o.position=TransformWorldToHClip(o.world); return o;
            }
            half4 frag(Output i):SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float t=_Time.y*.22;
                float wave=sin(i.world.z*2.7+i.world.x*.31+t)*.6+sin(i.world.z*5.1-i.world.x*.7-t*.8)*.4;
                float3 n=normalize(float3(sin(i.world.x*.9+t)*.012,1,wave*.018));
                float3 view=normalize(GetWorldSpaceViewDir(i.world));
                float3 ray=reflect(-view,n);
                float degrees=atan2(ray.x,-ray.z)*57.29578;
                float v=(ray.y/max(length(ray.xz),.01)*220+8)/148;
                float2 uv=CarezzaPanoramaUV(degrees,v);
                half side=CarezzaSideBlend(degrees);
                half4 reflection=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,float2(uv.x,saturate(uv.y)));
                reflection.rgb=lerp(reflection.rgb,half3(.025,.04,.065),side*.72);
                half3 sky=half3(.04,.075,.12);
                half ridgeFade=1-smoothstep(115,145,abs(degrees));
                half3 reflected=lerp(sky,reflection.rgb,reflection.a*ridgeFade*step(uv.y,1));
                half fresnel=pow(1-saturate(dot(n,view)),3);
                half shimmer=pow(saturate(sin(i.world.z*7+wave)*.5+.5),12)*.008;
                return half4(lerp(_Color.rgb,reflected*half3(.48,.63,.72),fresnel*.68)+shimmer,1);
            }
            ENDHLSL
        }
    }
}
