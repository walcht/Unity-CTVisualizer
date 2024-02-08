Shader "UnityCTVisualizer/HistogramShader"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Densities frequency 1D-ish texture", 2D) = "green" {}
        _BinColor("Bins color", Color) = (1.0, 1.0, 1.0, 1.0)
        _BackgroundColor("Background color", Color) = (0.0, 0.0, 0.0, 1.0)
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        ZWrite Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            
            sampler2D _MainTex;
            float4 _BinColor;
            float4 _BackgroundColor;

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (float4 vertex : POSITION, float2 uv : TEXCOORD0)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(vertex);
                o.uv = uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the frequency for current density
                float freq = tex2Dlod(_MainTex, float4(i.uv.x, 0.0f, 0.0f, 0.0f));
                float t = step(freq, i.uv.y);
                float4 col = t * _BackgroundColor + (1 - t) * _BinColor;
                return col;
            }
            ENDCG
        }
    }
}
