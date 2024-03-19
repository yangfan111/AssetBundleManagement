#include "UnityCG.cginc"

#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"
sampler2D _MainTex;
float4 _MainTex_ST;
fixed4 _Color;
float _Smoothness;
fixed _SpecularHint;
sampler2D _BumpMap;
float _Metallic;
float4 _BumpMap_ST;

struct VertexData
{
    UNITY_VERTEX_INPUT_INSTANCE_ID
    float4 position : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
};

struct a2v
{
    float3 vertex:POISITON;
    float3 normal:NORMAL;
    float4 tangent:TANGET;
    float2 uv:TEXCOORD0;
};

struct v2f
{
    float4 postion:SV_POSTION;
    float4 uv:TEXCOORD0;
    float3 worldPos:TEXCOORD1;
    float3 normal:TEXCOORD2;
    float3 tanget:TEXCOORD3;
    float3 bionormal:TEXCOORD4;
    SHADOW_COORDS(5);
};

v2f vertfunc(a2v v)
{
    v2f output;
    output.postion = UnityObjectToClipPos(v.vertex);
    output.worldPos = mul(unity_ObjectToWorld, v.vertex);

    output.normal = UnityObjectToWorldNormal(v.normal);
    output.tanget = UnityObjectToWorldDir(v.tangent.xyz);
    output.bionormal = cross(normalize(output.normal), normalize(output.tanget.xyz)) * v.tangent.w;

    output.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
    output.uv.zw = TRANSFORM_TEX(v.uv, _BumpMap);
    TRANSFER_SHADOW(output);
    return output;
}
UnityLight CreateLight(v2f input)
{
    UnityLight output;
    output.dir = UnityWorldSpaceLightDir(input.worldPos);
    UNITY_LIGHT_ATTENUATION(attent, input, input.worldPos); //计算光照衰减
    output.color = attent * _LightColor0;
    return output;
}

UnityIndirect CreateIndirectLight(v2f input)
{
    UnityIndirect output;
    return output;
}
fixed4 fragFunc(v2f input)
{
    float3 bump = UnpackNormal(tex2D(_BumpMap, input.uv.zw)); //tex2d采样出fixed4 ->UnpackNormal一次转换
    float3 bumpedNormal = normalize(float3(bump.x * input.tanget + bump.y * input.bionormal + bump.z * input.normal));
    //bump分别乘以tbn之后相加
    float3 albedo = tex2D(_MainTex, input.uv).rgb * _Color.rgb;
    float3 viewDir = normalize(UnityWorldSpaceViewDir(input.worldPos));
    float3 specularTint;
    float oneMinusReflectivity;
    DiffuseAndSpecularFromMetallic(albedo, _Metallic, specularTint, oneMinusReflectivity);
    float4 color = UNITY_BRDF_PBS(
        albedo, specularTint,
        oneMinusReflectivity, _Smoothness,
        bumpedNormal, viewDir,
        CreateLight(input), CreateIndirectLight(input)
    );
}


