// Upgrade NOTE: replaced '_LightMatrix0' with 'unity_WorldToLight'
// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/ShadowTest" {
	Properties {
		_Diffuse ("Diffuse", Color) = (1, 1, 1, 1)
		_Specular ("Specular", Color) = (1, 1, 1, 1)
		_Gloss ("Gloss", Range(8.0, 256)) = 20
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		
		Pass {
			// Pass for ambient light & first pixel light (directional light)
			Tags { "LightMode"="ForwardBase" }
		
			CGPROGRAM
			
			// Apparently need to add this declaration 
			#pragma multi_compile_fwdbase	
			
			#pragma vertex vert
			#pragma fragment frag
			
			// Need these files to get built-in macros
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			
			fixed4 _Diffuse;
			fixed4 _Specular;
			float _Gloss;
			
			struct a2v {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 uv:TEXCOORD0;
			};
			
			struct v2f {
				float4 pos : SV_POSITION;
				float3 worldNormal : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
				//shadow uv坐标
				UNITY_SHADOW_COORDS(2)
			};
			
			v2f vert(a2v v) {
			 	v2f o;
			 	o.pos = UnityObjectToClipPos(v.vertex);
			 	
			 	o.worldNormal = UnityObjectToWorldNormal(v.normal);

			 	o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
			 	//shadow transfer
				TRANSFER_SHADOW(o)
			 	return o;
			}
			
			fixed4 frag(v2f i) : SV_Target {
				fixed3 worldNormal = normalize(i.worldNormal);
				fixed3 worldLightDir = normalize(_WorldSpaceLightPos0.xyz);
				
				fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz;

			 	fixed3 diffuse = _LightColor0.rgb * _Diffuse.rgb * max(0, dot(worldNormal, worldLightDir));

			 	fixed3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
			 	fixed3 halfDir = normalize(worldLightDir + viewDir);
			 	fixed3 specular = _LightColor0.rgb * _Specular.rgb * pow(max(0, dot(worldNormal, halfDir)), _Gloss);

				//shadow采样
				UNITY_LIGHT_ATTENUATION(atten,i,i.worldPos);
				
				return fixed4(ambient + (diffuse + specular) * atten , 1.0);
			}
			
			ENDCG
		}
	
		Pass {
			// Pass for other pixel lights
			Tags { "LightMode"="ForwardAdd" }
			
			Blend One One
		
			CGPROGRAM
			
			//...
			
			ENDCG
		}
	}
	FallBack "Specular"
}