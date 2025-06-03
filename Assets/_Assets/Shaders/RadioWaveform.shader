Shader "Astro/Radio Waveform"
{
    Properties
    {
		_BgColor("Background Color", Color) = (0, 0, 0, 1)
        _LineColor ("Line Color", Color) = (1,1,1,1)

		_LineThickness ("Line Thickness", float) = 0.1
		_LineCenter("Line Center", float) = 0.5

		_LocalTimeScale("Time Scale", float) = 1

		_DataScaleX("Data Scale X", float) = 1
		_DataScaleY("Data Scale Y", float) = 1

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend Mode", Int) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DestBlend("Destination Blend Mode", Int) = 10
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp("Blend Operation", Int) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Geometry"
            "IgnoreProjector"="True"
            "RenderType"="Opaque"
            "PreviewType"="Plane"
        }

        Cull Back
        Lighting Off
        ZWrite On
		ZTest LEqual

        Blend [_SrcBlend] [_DestBlend]
        BlendOp [_BlendOp]

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
			#include "UnityCG.cginc"
			#include "Assets/_Assets/Shaders/WaveformShaderIncludes.cginc"

			fixed4 _LineColor;
			fixed4 _BgColor;

			float _LocalTimeScale;

			float _LineCenter;
			float _LineThickness;

			float _DataScaleY;
			float _DataScaleX;

			struct VertIn {
				float4 vertex   : POSITION;
				fixed2 uv		: TEXCOORD0;
			};

            struct VertOut
            {
                float4 vertex   : SV_POSITION;
                fixed2 uv		: TEXCOORD0;
            };

            VertOut vert(VertIn IN)
            {
                VertOut OUT;

				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.uv = IN.uv;

                return OUT;
            }


            fixed4 frag(VertOut IN) : SV_Target
            {
				float time = _Time.y * _LocalTimeScale;

				float dist = 1;

				float waveX = IN.uv.x * _DataScaleX;
				fixed2 linePos = IN.uv;
				float offset = WaveStatic(time, waveX);
				offset *= sin(3.1415 * IN.uv.x);
				linePos.y = _LineCenter + _DataScaleY * offset;
				
				dist = min(dist, SdfCircle(linePos, IN.uv, _LineThickness));
				return SdfAABlend(_BgColor, _LineColor, dist * 64);
            }
        ENDCG
        }
    }
}
