Shader "VRExperienceAGB/TwilightSky"
{
    Properties
    {
        _MainTex ("Panorama", 2D) = "white" {}
        _Exposure ("Exposure", Range(0,2)) = 1
        _HorizonOffset ("Horizon height", Range(-0.25,0.25)) = -0.15
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            half _Exposure;
            float _HorizonOffset;
            struct Input { float4 vertex:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Output { float4 position:SV_POSITION; float3 direction:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Output vert(Input v)
            {
                Output o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.position=UnityObjectToClipPos(v.vertex); o.direction=v.vertex.xyz; return o;
            }
            half4 frag(Output i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 direction=normalize(i.direction);
                float2 uv=float2(atan2(direction.x,direction.z)/(2*UNITY_PI)+0.5,
                    asin(clamp(direction.y,-1,1))/UNITY_PI+0.5+_HorizonOffset);
                half3 color=tex2D(_MainTex,float2(uv.x,saturate(uv.y))).rgb;
                return half4(color*_Exposure,1);
            }
            ENDCG
        }
    }
}
