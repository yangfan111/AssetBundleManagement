Shader "Custom/Prac-BlinnPhong" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		//包含这个编译宏
		#include "Lighting.cginc" 
		fixed4 _Color; //(1,1,1,1)->fixed4/float4 
		sampler2D _MainTex;//2d->Sample2d,_ST
		float4 _MainTex_ST;
		float _Glossiness;//range->float/fixed
		float _Metallic;
		struct a2v
		{
			float4 vertex:POSITION; //一定是float4,包含w
			float3 normal:NORMAL; 
			float4 uv:TEXCOORD0;//一定是flaot4,包含uv+offset
		};
		struct v2f
		{
			float3 position:SV_POSITION;
			float3 worldNornal:TEXCOORD0;
			float3 worldPos:TEXCOORD1;
			float2 uv:TEXCOORD2;//uv from float4->float2
		};
		//region:顶点坐标系基础变换
		v2f vert(a2v input) //vert action
		{
			v2f output;
			output.position = UnityObjectToClipPos(input.vertex); //POS
			output.worldPos = mul(unity_ObjectToWorld,input.vertex).xyz;//WORLDPOS
			output.worldNornal = UnityObjectToWorldNormal(input.normal);//NORMAL
			output.uv = TRANSFORM_TEX(input.uv,_MainTex);//UV 
			return output;
		}
		fixed4 frag(v2f input):SV_Target //片段阶段基本都用fixed4表示
		{
			fixed3 amb = UNITY_LIGHTMODEL_AMBIENT.xyz;
			fixed3 worldNormal = normalize(input.worldNornal);//normalized之后变为fixed3
			fixed3 worldLightDir = normalize(_WorldSpaceLightPos0.xyz);
			//float4通过xyz取值，fixed4通过rgb取值
			fixed3 diffise = _LightColor0.rgb * tex2D(_MainTex,input.uv) * _Color.rgb * max(0,dot(worldNormal,worldLightDir));
			fixed3 viewDir = normalize((_WorldSpaceCameraPos.xyz - input.worldPos.xyz)) ;
			fixed3 halfDir = normalize(worldLightDir + viewDir);
			fixed3 specular = _LightColor0.rgb * _SpecColor.rgb * pow(max(0,dot(halfDir,worldNormal)),_Glossiness);
			return fixed4(diffise + specular+amb,1.0); //加入alpha分量
		}
		ENDCG
	}
	FallBack "Diffuse"
}