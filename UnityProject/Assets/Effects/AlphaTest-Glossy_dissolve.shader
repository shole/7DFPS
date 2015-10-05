Shader "Custom/Specular_Dissolve" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 0)
	_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
	_MainTex ("Base (RGB) TransGloss (A)", 2D) = "white" {}
	_DissolveTex ("Dissolve Mask", 2D) = "white" {}
	_DissolveOrigin("Dissolve Origin", Vector) = (0,0,0,0)
	_DissolveNormal("Dissolve Normal", Vector) = (0,1,0,0)
}

SubShader {
	Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
	LOD 300

CGPROGRAM
#pragma surface surf BlinnPhong

sampler2D _MainTex;
sampler2D _DissolveTex;
fixed4 _Color;
half _Shininess;
float3 _DissolveOrigin;
float3 _DissolveNormal;

struct Input {
	float2 uv_MainTex;
	float2 uv_DissolveTex;
	float3 worldNormal;
	float3 worldPos;
};

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
	float3 fromDissolveOrigin = IN.worldPos-_DissolveOrigin;
	float dissolveFactor = 1-dot(fromDissolveOrigin, _DissolveNormal)*20;
	float3 pos = IN.worldPos*30;
	fixed dissolve = tex2D(_DissolveTex, pos.xz).a*abs(IN.worldNormal.y) + tex2D(_DissolveTex, pos.xy).a*abs(IN.worldNormal.z) + tex2D(_DissolveTex, pos.yz).a*abs(IN.worldNormal.x);
	o.Albedo = tex.rgb * _Color.rgb;
	//o.Albedo = fixed3(dissolveFactor,dissolve,0);
	o.Gloss = tex.a;
	clip(dissolve-dissolveFactor);
	o.Specular = _Shininess;
}
ENDCG
}

Fallback "Legacy Shaders/Transparent/Cutout/VertexLit"
}
