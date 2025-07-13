Shader "Custom/RopeLineURP_2D"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color   ("Tint"  , Color) = (1,1,1,1)
        _Thickness ("World-space radius", Float) = 0.02
    }
    SubShader
    {
        Tags{ "RenderPipeline"="UniversalPipeline"
              "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "RopeLineURP_2D"
            Tags{ "LightMode"="Universal2D" }

            Cull Off     // view-oriented ribbons
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5               // StructuredBuffer + instancing
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── data coming from C# ────────────────────────────────────────────
            struct Point      { float2 pos; float2 prev; float  fric; int fix; uint pinID; };
            struct Constraint { int    iA;  int    iB;  float  rest; };

            StructuredBuffer<Point>      _Points;
            StructuredBuffer<Constraint> _Constraints;

            // ── per-material params ───────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _Thickness;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            // ── vertex inputs / outputs ───────────────────────────────────────
            struct Attributes { uint vertexID  : SV_VertexID;
                                uint instanceID: SV_InstanceID; };
            struct Varyings   { float4 posCS   : SV_POSITION;
                                float2 uv      : TEXCOORD0;
                                half4  color   : COLOR;};

            // six vertices per quad → which corner?
            static const float2 corner[6] = {
                float2(-1,-1), float2( 1,-1), float2( 1, 1),
                float2(-1,-1), float2( 1, 1), float2(-1, 1)
            };

            Varyings vert (Attributes IN)
            {
                Constraint seg = _Constraints[IN.instanceID];

                float2 pA = _Points[seg.iA].pos;
                float2 pB = _Points[seg.iB].pos;

                // world positions (z = 0)
                float3 A = float3(pA, 0);
                float3 B = float3(pB, 0);

                // tangent & billboard basis
                float3 T = normalize(B - A);              // along the segment
                float3 V = GetWorldSpaceViewDir(0.5*(A+B)); // camera→segment
                float3 R = normalize(cross(T, V));        // right vector

                // pick corner, offset by thickness
                float2 c = corner[IN.vertexID];
                float3 worldPos = (c.x * R + c.y * T) * _Thickness +    // thickness & along
                                  (c.y > 0 ? B : A);                    // anchor to ends

                Varyings OUT;
                OUT.posCS = TransformWorldToHClip(worldPos);
                OUT.uv    = float2(c.y > 0, c.x > 0);     // simple strip UVs
                OUT.color = _Color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
                return col;
            }
            ENDHLSL
        }
    }
}


