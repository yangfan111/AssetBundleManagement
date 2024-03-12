// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unity Shaders Book/Chapter 13/Motion Blur With Depth Texture" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_BlurSize ("Blur Size", Float) = 1.0
	}
	SubShader {
		CGINCLUDE
		
		#include "UnityCG.cginc"
		
		sampler2D _MainTex;

		uniform  half4 _MainTex_TexelSize;
		struct v2f
		{
			float4 pos : SV_POSITION;
			half2 uv[9] :TEXCOORD0;//屏幕后处理：采样周围8个点uv坐标 
		};
//    
//屏幕后处理结构 float4 vertex : POSITION;  half2 texcoord : TEXCOORD0
	v2f vert1(appdata_img v)
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		//屏幕后处理：采样周围8个点uv坐标
		o.uv[0] = v.texcoord;
		o.uv[1] = v.texcoord + _MainTex_TexelSize.xy * half2(-1,-1);
		o.uv[2] = v.texcoord + _MainTex_TexelSize.xy * half2(1,-1);
		o.uv[3] = v.texcoord + _MainTex_TexelSize.xy * half2(1,-1);
		//..
	}
	struct v2f2 {
			float4 pos : SV_POSITION;
			half2 uv : TEXCOORD0;
			half2 uv_depth : TEXCOORD1;
	};
	v2f2 vert2(appdata_img v) {
			v2f2 o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = v.texcoord;
			o.uv_depth = v.texcoord;
			
			#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y < 0)
				o.uv_depth.y = 1 - o.uv_depth.y;
			#endif
					 
			return o;
	}
		//采样相机深度图->重建世界坐标
	sampler2D _CameraDepthTexture;
	float4x4 _CurrentViewProjectionInverseMatrix;
	fixed4 frag(v2f2 i) : SV_Target{
		 float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,i.uv_depth);
		float4 ndc = float4(i.uv.x*2-1,i.uv.y*2-1,depth*2-1,1);
		float worldPosW = mul(_CurrentViewProjectionInverseMatrix,ndc);
		float worldPos = worldPosW/worldPosW.w;
		
	}




		ENDCG
		
		Pass {      
			ZTest Always Cull Off ZWrite Off
			    	
			CGPROGRAM  
			
			#pragma vertex vert  
			#pragma fragment frag  
			  
			ENDCG  
		}
	} 
	FallBack Off
}
