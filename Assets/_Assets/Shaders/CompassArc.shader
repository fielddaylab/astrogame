// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Astro/Compass Arc"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend Mode", Int) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DestBlend("Destination Blend Mode", Int) = 10
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp("Blend Operation", Int) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
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

			struct VertIn {
				float4 vertex   : POSITION;
				fixed4 color    : COLOR;
			};

            struct VertOut
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
            };

            VertOut vert(VertIn IN)
            {
                VertOut OUT;

				OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.color = IN.color;

                return OUT;
            }


            fixed4 frag(VertOut IN) : SV_Target
            {
                return IN.color;
            }
        ENDCG
        }
    }
}
