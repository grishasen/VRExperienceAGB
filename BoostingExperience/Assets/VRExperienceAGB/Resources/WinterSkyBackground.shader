Shader "VRExperienceAGB/WinterSkyBackground"
{
    Properties {
        _HorizonColor ("Horizon", Color) = (.045,.085,.15,1)
        _ZenithColor ("Zenith", Color) = (.006,.015,.035,1)
        _MoonDirection ("Moon direction", Vector) = (0,1,0,0)
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
            half4 _HorizonColor, _ZenithColor;
            float4 _MoonDirection;
            struct Input { float4 vertex:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Output { float4 position:SV_POSITION; float3 direction:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Output vert(Input v) { Output o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.position=UnityObjectToClipPos(v.vertex); o.direction=v.vertex.xyz; return o; }
            half4 frag(Output i):SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 direction = normalize(i.direction);
                float altitude = saturate(direction.y);
                half3 color = lerp(_HorizonColor.rgb,_ZenithColor.rgb,pow(altitude,.55));
                // Low horizon haze and a soft lunar aureole, without hiding star positions.
                color += half3(.018,.028,.045) * exp(-altitude * 18);
                float moonDistance = max(0, 1-dot(direction,normalize(_MoonDirection.xyz)));
                color += half3(.065,.080,.10) * exp(-moonDistance * 1400);
                color += half3(.009,.014,.022) * exp(-moonDistance * 120);
                return half4(color,1);
            }
            ENDCG
        }
    }
}
