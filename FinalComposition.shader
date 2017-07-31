// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "FinalComposition"
{
    Properties
    {
        _MainCameraTex ("MainCameraTexture", 2D) = "white" {}
		_MaskedAreaTex ("MaskedAreaTexture", 2D) = "white" {}
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

            sampler2D _MainCameraTex;
			sampler2D _MaskedAreaTex;
			float4 _MainCameraTex_ST;
            
            v2f vert (appdata v)
            {
                v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainCameraTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float4 mainCameraColor = tex2D(_MainCameraTex , i.uv);
				float4 maskedAreaColor = tex2D(_MaskedAreaTex , i.uv);
				float2 blendFactors = float2(0.5, 0.5);				// Currently only constant blend factors can be used
				
				// Blend virtual content on top of the masked content	
				float mainCameraColorChannelSum = mainCameraColor.x + mainCameraColor.y + mainCameraColor.z;
				if (mainCameraColorChannelSum > 0.0)
				{		
					//CustomStandard shader outputs depth fade value to aplha channel ([0.0, 1.0] range)
					//CustomStandard shader has material propeties _DepthBasedFadeRangeStart and _DepthBasedFadeRangeEnd.
					//These variables control depth based range, while the fade is linear between the start and end.
					//The both properties are exposed on the CustomShader material properties on editor.
					blendFactors.x = mainCameraColor.w;
					blendFactors.y = 1.0 - blendFactors.x;
					return (blendFactors.x*mainCameraColor + blendFactors.y*maskedAreaColor);
				}
				else
				    return maskedAreaColor;
            }
            ENDCG
        }
    }
}
