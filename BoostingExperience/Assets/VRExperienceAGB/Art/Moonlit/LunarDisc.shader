Shader "VRExperienceAGB/LunarDisc"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Output { float4 position:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Output vert(Input v)
            {
                Output o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.position=TransformObjectToHClip(v.vertex.xyz); o.uv=v.uv; return o;
            }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p)
            {
                float2 cell=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash(cell),hash(cell+float2(1,0)),f.x),lerp(hash(cell+float2(0,1)),hash(cell+1),f.x),f.y);
            }
            half4 frag(Output i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 p=(i.uv-.5)*2; float radius=length(p)/.63;
                float edge=max(fwidth(radius),.003);
                float disc=1-smoothstep(1-edge,1+edge,radius);
                float maria=smoothstep(.40,.65,noise(p*5+8)+noise(p*11)*.22);
                float detail=noise(p*46)*.06;
                float crater=0;
                // Small crater rims supplement broad lunar maria; no scene-sized post effect is needed.
                for(int k=0;k<12;k++)
                {
                    float2 center=float2(hash(float2(k,2)),hash(float2(k,9)))*1.1-.55;
                    float d=length(p-center)/( .025+hash(float2(k,5))*.065);
                    crater+=exp(-pow((d-.85)*8,2))*.09-exp(-d*d*3)*.06;
                }
                float limb=sqrt(saturate(1-radius*radius));
                float value=.88-maria*.25+detail+crater+limb*.10;
                float halo=exp(-max(radius-1,0)*9)*.12*(1-disc);
                return half4(lerp(float3(.65,.26,.12),float3(1,.64,.40)*value,disc),saturate(disc+halo));
            }
            ENDHLSL
        }
    }
}
