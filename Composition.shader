// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Composition"
{
    Properties
    {
        _MaskTex ("MaskTexture", 2D) = "white" {}
		_DispTex ("DisplacedTexture", 2D) = "white" {}
		_ImageTex ("ImageTexture", 2D) = "white" {}
		_Saturation ("Saturation", Float) = 0.5
		_LiftColor ("Lift color", Color) = (0, 0, 0, 1)
		_GammaColor ("Gamma color", Color) = (0.5, 0.5, 0.5, 1)
		_GainColor ("Gain color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
                      
            #include "UnityCG.cginc"

            const float Saturation = 0.5;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MaskTex;
			sampler2D _DispTex;
			sampler2D _ImageTex;
            float4 _ImageTex_ST;
			float4 _LiftColor;
			float4 _GammaColor;
			float4 _GainColor;
			float _Saturation;

            v2f vert (appdata v)
            {
                v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _ImageTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float4 mask = tex2D(_MaskTex , i.uv);
				float4 inverseMask = float4(1.0-mask.x, 1.0-mask.y, 1.0-mask.z, 1.0);
				float4 displacedImageTexel = tex2D(_DispTex , i.uv);

				float4 lift = _LiftColor;
				float4 gamma = _GammaColor;
				float4 gain = _GainColor;
				float saturation = clamp(_Saturation,0,1);

//				float lumachange =
//					(clamp((0.299*lift.x + 0.587*lift.y + 0.114*lift.z),0,1) +
//					clamp((0.299*gamma.x + 0.587*gamma.y + 0.114*gamma.z),0,1) +
//					clamp((0.299*lift.x + 0.587*gain.y + 0.114*gain.z),0,1)) * 0.333;

//				float4 lift = float4(0.0, 0.0, 0.25, 1.0);
//				float4 gamma = float4(0.5, 0.5, 0.5, 1.0);
//				float4 gain = float4(1.5, 1.5, 1.0, 1.0);

				// Lift, gamma & gain color correction
				displacedImageTexel =
					clamp(
						pow((gain * (displacedImageTexel + lift * (1-displacedImageTexel))), (1/(gamma+0.5))),
					0, 1);
				float luma = 0.299*displacedImageTexel.x + 0.587*displacedImageTexel.y + 0.114*displacedImageTexel.z;		// Calculate luma per texel

				float4 col = tex2D(_ImageTex, i.uv);
				// blend desaturated displaced image with mask
				fixed4 comp = mask*(displacedImageTexel * saturation + luma * (1-saturation)) + inverseMask*col;
                return comp;
            }
            ENDCG
        }
    }
}
