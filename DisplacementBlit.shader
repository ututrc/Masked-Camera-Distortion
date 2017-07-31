// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "DisplacementBlit"
{
    Properties
    {
        _MainTex ("ImageTexture", 2D) = "white" {}
		_DisplacementTex ("DisplacementTexture", 2D) = "white" {}
		_Offset ("Offset", Vector) = (0,0,0,0)		// for animating the displacement map
        _AnimSpeed ("Animation speed", Range(-2,2)) = 1.0
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

            sampler2D _MainTex;
			sampler2D _DisplacementTex;
            float4 _MainTex_ST;
            float2 _Offset;
            float _AnimSpeed;

			float2 uvOffsetInvScales;	// Set from the MainCameraSrcipt, verify the values if video/screen resolutions change

            v2f vert (appdata v)
            {
                v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
            	float2 displaceOffset = _Offset;
                float2 uvOffsetInvScalesScale = float2(35.0, 35.0); // Modify this constant to change the displacement scaling 

                float2 animatedOffset = float2(sin(_Time.x*75)*0.05, _Time.y*0.25) * _AnimSpeed;

                // first displaced uv set
				float4 uvDisplacement = tex2D(_DisplacementTex, i.uv-displaceOffset.xy-animatedOffset);	// animate the displace
				float2 uvDisplacementScaled = float2((uvDisplacement.x - 0.5)*uvOffsetInvScales.x*uvOffsetInvScalesScale.x, (uvDisplacement.y - 0.5)*uvOffsetInvScales.y*uvOffsetInvScalesScale.y);
				float2 displacedUV = float2(i.uv.x+uvDisplacementScaled.x, i.uv.y+uvDisplacementScaled.y);

                // second sample for mixing two 
                float4 uvDisplacement2 = tex2D(_DisplacementTex, i.uv-displaceOffset.xy-animatedOffset*0.5 + float2(0.33, 0.33)); // animate the displace
                float2 uvDisplacementScaled2 = float2((uvDisplacement2.x - 0.5)*uvOffsetInvScales.x*uvOffsetInvScalesScale.x, (uvDisplacement2.y - 0.5)*uvOffsetInvScales.y*uvOffsetInvScalesScale.y);
                float2 displacedUV2 = float2(i.uv.x+uvDisplacementScaled2.x, i.uv.y+uvDisplacementScaled2.y);
				
                // blend two sets for extra haziness
				fixed4 col = tex2D(_MainTex, displacedUV) * 0.5 + tex2D(_MainTex, displacedUV2) * 0.5;
                return col;
            }
            ENDCG
        }
    }
}
