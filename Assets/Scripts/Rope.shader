Shader "Custom/Rope" {
SubShader {
// Transparent queue for overlay
Tags { "RenderType"="Transparent" "Queue"="Transparent" }

```
    Pass {
        Name "UniversalForward"
        Tags { "LightMode"="UniversalForward" }

        ZTest Always
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma multi_compile_fog
        #pragma target 5.0

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // Matches C# Point struct
        struct Point {
            float2 position;
            float2 prevPosition;
            float  friction;
            int    isFixed;
        };

        StructuredBuffer<Point> points;
        float4 _Color;

        struct Attributes {
            uint vertexID : SV_VertexID;
        };

        struct Varyings {
            float4 positionCS : SV_POSITION;
            UNITY_FOG_COORDS(1)
            float4 color      : COLOR;
        };

        Varyings vert(Attributes IN) {
            Varyings OUT;
            Point p = points[IN.vertexID];
            float3 worldPos = float3(p.position, 0.0);
            OUT.positionCS = TransformWorldToHClip(worldPos);
            OUT.color = _Color;
            UNITY_TRANSFER_FOG(OUT, OUT.positionCS);
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target {
            half4 col = IN.color;
            UNITY_APPLY_FOG(IN.fogCoord, col);
            return col;
        }
        ENDHLSL
    }
}
FallBack "Unlit/Color"
```

}

