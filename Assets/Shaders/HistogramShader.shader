Shader "UnityCTVisualizer/HistogramShader"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Densities frequency 1D-ish texture", 2D) = "green" {}
        _BinColor("Bins color", Color) = (1.0, 1.0, 1.0, 1.0)
        _BackgroundColor("Background color", Color) = (0.0, 0.0, 0.0, 1.0)
        _PlotColor("Alpha plot color", Color) = (0.0, 1.0, 0.0, 1.0)
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
            #define MAX_ALPHA_CONTROL_POINTS 16

            
            sampler2D _MainTex;
            float4 _BinColor;
            float4 _BackgroundColor;
            float4 _PlotColor;
            int _AlphaCount = 0;
            float _AlphaPositions[MAX_ALPHA_CONTROL_POINTS] ;
            float _AlphaValues[MAX_ALPHA_CONTROL_POINTS];

            float plot(float y, float r) {
                return  smoothstep(r - 0.02f, r, y) - smoothstep(r, r + 0.02f, y);
            }

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
                for (int j = 0; j < _AlphaCount - 1; ++j) {
                    // TODO: optimize this loop
                    if ( i.uv.x >= _AlphaPositions[j] &&  i.uv.x < _AlphaPositions[j+1]) {
                        float p = plot(i.uv.y, lerp(_AlphaValues[j], _AlphaValues[j+1], (i.uv.x - _AlphaPositions[j]) / (_AlphaPositions[j+1] - _AlphaPositions[j])));
                        col = p * _PlotColor + (1.0f - p) * col;
                        break;
                    }
                }
                return col;
            }
            ENDCG
        }
    }
}
