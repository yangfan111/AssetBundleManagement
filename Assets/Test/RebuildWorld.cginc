#include "UnityCG.cginc"
sampler2D _CameraDepthTexture;
float4x4 _InverseVPMatrix;
//后处理阶段-重建世界坐标：ndc坐标重建    
fixed4 frag_rebuildworld_postprocess_ndc(v2f_img i) : SV_Target
{
    float depthVal = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture,i.uv)); //UNITY_SAMPLE_DEPTH->tex2D
    #if defined(UNITY_REVERSED_Z)
    depthVal = 1-depthVal;
    #endif
    float4 ndc = float4(i.uv.x *2-1,i.uv.y*2-1,depthVal*2-1,1);//重建ndc，将它作为坐标，构建用于矩阵乘法的坐标一定写w值，w为1表示坐标，w为0表示向量
    float4 worldPos = mul(_InverseVPMatrix, ndc); //_invserseVP变换到世界空间 
    worldPos /= worldPos.w; //将齐次坐标w分量变1得到世界坐标
    return worldPos;

}
float4x4 _ViewPortRay; //设置的相机四角的方向
struct v2f_ray
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 rayDir : TEXCOORD1;
};
//后处理阶段-重建世界坐标： campos + 射线方向重建    
v2f_ray vert_rebuildworld_postprocess_ray(appdata_img i)
{
    v2f_ray output;
    output.pos = UnityObjectToClipPos(i.vertex);
    output.uv = i.texcoord;
    output.rayDir = i.texcoord.x + i.texcoord.y *2 ; //计算出每个uv顶点对应的射线，这么看：x权重1，y权重2，计算出0->3的索引坐标
    return output;
}
fixed4 frag_rebuildworld_postprocess_ray(v2f_ray i) : SV_Target
{
    float depthVal = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture,i.uv)); //UNITY_SAMPLE_DEPTH->tex2D
    depthVal = Linear01Depth(depthVal);//得到线性深度值
    //worldpos = campos + 射线方向(世界空间下) * depth
    float3 worldPos = _WorldSpaceCameraPos + depthVal * i.rayDir.xyz;
    return fixed4(worldPos, 1.0);
}
struct v2f_viewVec
{
    float4 vertex : SV_POSITION;
    float4 screenPos : TEXCOORD0;
    float3 viewVec : TEXCOORD1;
};
fixed4 frag(v2f_viewVec i) : SV_Target
{
    float depth = UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture,i.screenPos)); //texProj取渲染物体的ComputeScreenPos的深度值
    depth = Linear01Depth(depth);
    float3 viewPos = i.viewVec * depth;//viewPos
    float3 worldPos = mul(UNITY_MATRIX_I_V,float4(viewPos,1)).xyz;//计算worldPos
    return float4(worldPos, 1.0);
}
