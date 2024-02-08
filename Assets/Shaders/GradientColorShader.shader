Shader "UnityCTVisualizer/GradientColorShader"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Colors (RGBA) Gradient Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags {"RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert alpha
            #pragma fragment frag alpha
            #include "UnityCG.cginc"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 clipVertex : SV_POSITION;
            };

            sampler2D _MainTex;

            v2f vert (float4 modelVertex: POSITION, float2 uv: TEXCOORD0)
            {
                v2f output;
                output.clipVertex = UnityObjectToClipPos(modelVertex);
                output.uv = uv;
                return output;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = float4(tex2D(_MainTex, i.uv).xyz, 1.0f);
                return col;
            }
            ENDCG
        }
    }
}
