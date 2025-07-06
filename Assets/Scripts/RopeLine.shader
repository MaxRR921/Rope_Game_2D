Shader "Custom/RopeLineURP_2D"
{
    Properties
    {
        _Color("Line Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        Pass
        {
            Name "SRPDefaultUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }

            ZTest Always
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // exactly matches your C# Point struct
            struct Point
            {
                float2 position;
                float2 prevPosition;
                float  friction;
                int    isFixed;
            };

            StructuredBuffer<Point> points;
            float4                  _Color;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                Point p = points[IN.vertexID];

                // 2D → world (z=0)
                float3 worldPos = float3(p.position, 0);
                // URP helper: world→clip
                OUT.positionCS = TransformWorldToHClip(worldPos);
                OUT.color      = _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return IN.color;
            }
            ENDHLSL
        }
    }
}

