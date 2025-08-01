Shader "FieldDay/Screen Space/Vertex Color Texture" {
	Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }

	SubShader {

		Tags
        {
            "Queue"="Overlay"
            "IgnoreProjector"="True"
            "RenderType"="Overlay"
            "PreviewType"="Plane"
        }

		ZTest Always
		Cull Off
		ZWrite Off
		Lighting Off

		Pass {

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#include "UnityCG.cginc"

			struct Attributes {
				float4 vertex: POSITION;
				fixed4 color: COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 vertex: SV_POSITION;
				fixed4 color: COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
            fixed4 _Color;

			Varyings Vert(Attributes attr) {
				Varyings v;
				UNITY_SETUP_INSTANCE_ID(attr);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(v);

				v.vertex = attr.vertex;
				v.texcoord = attr.texcoord;
				v.color = attr.color;
				return v;
			}

			fixed4 Frag(Varyings v) : SV_Target {
				return v.color * tex2D(_MainTex, v.texcoord) * _Color;
			}

			ENDCG
		}
	}

	Fallback Off
}