Shader "Custom/NormalMapTest" {
	Properties {
		_Color ("Color Tint", Color) = (1, 1, 1, 1)
		_MainTex ("Main Tex", 2D) = "white" {}
		_BumpMap ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Bump Scale", Float) = 1.0
		_Specular ("Specular", Color) = (1, 1, 1, 1)
		_Gloss ("Gloss", Range(8.0, 256)) = 20
	}
	SubShader {
			Tags { "LightMode"="ForwardBase" }
		
			CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag
			
			#include "Lighting.cginc"
			
			fixed4 _Color;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _BumpMap;
			float4 _BumpMap_ST;
			float _BumpScale;
			fixed4 _Specular;
			float _Gloss;

			struct  a2v
			{
				float4 positon:POISTION; //float4带一个w
				float3 normal:NORMAL;
				float4 tanget:TAGENT; //增加TANGENT,float4带一个w
				float4 texCoord:TEXCOORD0;
			};
			struct v2f
			{
				float4 pos: SV_POSITION;
				//把worldpos塞到里面去
				float4 worldX:TEXCOORD0;
				float4 worldY:TEXCOORD1;
				float4 worldZ:TEXCOORD2;
				float4 uv:TEXCOORD3;
				//float3 worldPos:TEXCOORD3;
			};
			v2f vertFunc(a2v input)
			{
				v2f output;
				
				output.pos = UnityObjectToClipPos(input.positon);
				float3 worldPos = mul(unity_ObjectToWorld, input.positon).xyz;
				float3 worldNormal = UnityObjectToWorldNormal(input.normal);//等价于normalize((mul((float3x3)unity_WorldToObject,input.normal));
				float3 tagent = UnityObjectToWorldDir(input.tanget);//等价于normalize(mul((float3x3)unity_ObjectToWorld,input.tanget)) ;
				float3 binormal  = cross( normalize(worldNormal), normalize(tagent) ) * input.tanget.w;
		//	直接使用TANGENT_SPACE_ROTATION代替上面所有代码;
				output.worldX = float4(tagent.x,binormal.x,worldNormal.x,worldPos.x);
				output.worldY = float4(tagent.y,binormal.y,worldNormal.y,worldPos.y);
				output.worldZ = float4(tagent.z,binormal.z,worldNormal.z,worldPos.z);
				output.uv.xy = TRANSFORM_TEX(input.texCoord,_MainTex);
				output.uv.zw = TRANSFORM_TEX(input.texCoord,_BumpMap);//计算normalMap的uv坐标
				
				return output;
				
			}
			fixed4 fragFunc(v2f input):SV_Target
			{
				float3 worldPos = float3(input.worldX.x,input.worldY.x,input.worldZ.x);
				fixed3 lightDir = UnityWorldSpaceLightDir(worldPos); //使用内置函数获取lightDir
				fixed3 viewDir = UnityWorldSpaceViewDir(worldPos);//使用内置函数获取viewDir
				//通过TBN坐标获得世界空间下法线坐标
				fixed3 bump = UnpackNormal(tex2D(_BumpMap,input.uv.zw)) ;
				bump = normalize(fixed3(dot(input.worldX.xyz,bump),dot(input.worldY.xyz,bump),dot(input.worldZ.xyz,bump)));
				fixed3 diffuse = tex2D(_MainTex,input.uv.xy) * _LightColor0.rgb * max(0,dot(lightDir,bump));
				fixed3 halfDir = normalize(lightDir +  viewDir);
				fixed3 specular = _LightColor0.rgb * pow(max(0,dot(halfDir,bump)),_Gloss);
				fixed3 amb = UNITY_LIGHTMODEL_AMBIENT.xyz;
				return fixed4(diffuse + specular + amb,1.0);
				
			}

		ENDCG
	}
	FallBack "Diffuse"
}