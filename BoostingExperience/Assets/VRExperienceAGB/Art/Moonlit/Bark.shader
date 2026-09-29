Shader "VRExperienceAGB/Bark"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Input { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Output { float4 position:SV_POSITION; float3 normal:TEXCOORD0; float2 uv:TEXCOORD1; float fog:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            Output vert(Input v)
            {
                Output o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.position=TransformObjectToHClip(v.vertex.xyz); o.normal=TransformObjectToWorldNormal(v.normal);
                o.uv=v.uv; o.fog=ComputeFogFactor(o.position.z); return o;
            }
            half4 frag(Output i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float a=i.uv.x*6.2831853, h=i.uv.y;
                float winding=a*22+sin(h*29+a*3)*.7+sin(h*73)*.15;
                float groove=pow(saturate(.5+.5*sin(winding)),9);
                float chips=saturate(sin(h*170+sin(a*17)*2)*sin(a*37+h*21));
                float grain=.5+.5*sin(a*71+sin(h*91));
                half3 albedo=lerp(half3(.22,.125,.065),half3(.075,.037,.020),groove*.8+chips*.18);
                albedo*=.85+grain*.3;
                Light light=GetMainLight();
                half3 lighting=SampleSH(normalize(i.normal))+light.color*saturate(dot(normalize(i.normal),light.direction));
                return half4(MixFog(albedo*lighting,i.fog),1);
            }
            ENDHLSL
        }
    }
}
