Shader "VRExperienceAGB/WinterSkyPoints"
{
    Properties {
        _Intensity ("Intensity", Float) = 1
        _FaintScale ("Faint star visibility", Range(0,1)) = 1
        _TwinkleTime ("Active sky time", Float) = 0
        _TwinkleStrength ("Twinkle strength", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent-100" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest LEqual Blend One One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            half _Intensity, _FaintScale;
            float _TwinkleTime;
            half _TwinkleStrength;
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 twinkle:TEXCOORD1; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Output { float4 position:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; half star:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            Output vert(Input v) {
                Output o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                // Per-eye camera-relative projection keeps the sky at optical infinity during locomotion.
                o.position=UnityWorldToClipPos(_WorldSpaceCameraPos + v.vertex.xyz * 100);
                #if defined(UNITY_REVERSED_Z)
                o.position.z=0.000001 * o.position.w;
                #else
                o.position.z=0.999999 * o.position.w;
                #endif
                float phase = _TwinkleTime * v.twinkle.y + v.twinkle.x;
                float shimmer = .68 * sin(phase) + .32 * sin(phase * 1.73 + v.twinkle.x);
                o.uv=v.uv; o.color=v.color;
                o.color.rgb *= 1 + shimmer * v.twinkle.z * _TwinkleStrength;
                o.star=v.twinkle.w; return o;
            }
            half4 frag(Output i):SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 p=i.uv*2-1; float falloff=pow(saturate(1-dot(p,p)),2);
                // A small defined disc with a one-pixel edge, rather than a broad Gaussian glow.
                // The minimal halo retains a little light between samples without a fuzzy skirt.
                float radius = length(p);
                float edge = max(fwidth(radius) * .5, .015);
                float core = 1 - smoothstep(.50 - edge, .50 + edge, radius);
                falloff = lerp(falloff, core + falloff * .015, i.star);
                return half4(i.color.rgb * i.color.a * falloff * _Intensity * lerp(_FaintScale, 1, saturate(i.color.a)),1);
            }
            ENDCG
        }
    }
}
