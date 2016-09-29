Shader "Hidden/ColoredFog" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "black" {}
}

CGINCLUDE

	#include "UnityCG.cginc"
	#include "AutoLight.cginc"

	uniform sampler2D _MainTex;
	uniform sampler2D_float _CameraDepthTexture;
	
	uniform float _GlobalDensity;
	uniform float _FogMaxDistance;
	uniform float4 _FogColor;
	uniform float4 _FogColor2;
	uniform float4 _FogColor3;
	uniform float4 _FogColor4;
	uniform float4 _FogColor5;
	uniform float4 _StartDistance;
	uniform float4 _Y;
	uniform float4 _MainTex_TexelSize;
	uniform float4 _SkyColor;
	uniform float _takeAlpha;
	
	
	// for fast world space reconstruction
	
	uniform float4x4 _FrustumCornersWS;
	uniform float4 _CameraWS;
	 
	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		float2 uv_depth : TEXCOORD1;
		float4 interpolatedRay : TEXCOORD2;
	};
	
	v2f vert( appdata_img v )
	{
		v2f o;
		half index = v.vertex.z;
		v.vertex.z = 0.1;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv = v.texcoord.xy;
		o.uv_depth = v.texcoord.xy;
		
		#if UNITY_UV_STARTS_AT_TOP
		if (_MainTex_TexelSize.y < 0)
			o.uv.y = 1-o.uv.y;
		#endif				
		
		o.interpolatedRay = _FrustumCornersWS[(int)index];
		o.interpolatedRay.w = index;
		
		return o;
	}
	
	float ComputeFogForYAndDistance (in float3 camDir, in float3 wsPos) 
	{
		float fogInt = saturate(length(camDir) * _StartDistance.x-1.0) * _StartDistance.y;	
		float fogVert = max(0.0, (wsPos.y-_Y.x) * _Y.y);
		fogVert *= fogVert; 
		return  (1-exp(-_GlobalDensity*fogInt)) * exp (-fogVert);
	}
	
	half4 fragAbsoluteYAndDistance (v2f i) : SV_Target
	{
		float dpth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,i.uv_depth));
		float4 wsDir = dpth * i.interpolatedRay;
		float4 wsPos = _CameraWS + wsDir;
		return lerp(tex2D(_MainTex, i.uv), _FogColor, ComputeFogForYAndDistance(wsDir.xyz,wsPos.xyz));
	}

	half4 fragRelativeYAndDistance (v2f i) : SV_Target
	{
		float dpth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,i.uv_depth));
		float4 wsDir = dpth * i.interpolatedRay;
		return lerp(tex2D(_MainTex, i.uv), _FogColor, ComputeFogForYAndDistance(wsDir.xyz, wsDir.xyz));
	}

	half4 fragAbsoluteY (v2f i) : SV_Target
	{
		float dpth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,i.uv_depth));
		float4 wsPos = (_CameraWS + dpth * i.interpolatedRay);
		float fogVert = max(0.0, (wsPos.y-_Y.x) * _Y.y);
		fogVert *= fogVert; 
		fogVert = (exp (-fogVert));
		return lerp(tex2D( _MainTex, i.uv ), _FogColor, fogVert);				
	}
	
	float4 DistanceToColor(float pixDistance)
	{
		float4 fogCol = float4(0,0,0,0);
		fogCol += _FogColor * saturate(1 - (pixDistance * 4));
		fogCol += _FogColor2 * saturate(1 - abs((0.25-pixDistance) * 4));
		fogCol += _FogColor3 * saturate(1 - abs((0.5-pixDistance) * 4));
		fogCol += _FogColor4 * saturate(1 - abs((0.75-pixDistance) * 4));
		fogCol += _FogColor5 * saturate(1 - abs((1-pixDistance) * 4));
		
		return fogCol;
	}
	
	float4 ApplyFogColor(float4 MT_Color, float4 fogCol, float pixDistance)
	{
		MT_Color = saturate(MT_Color);
		float alpha = lerp(1,MT_Color.a,_takeAlpha);
		fogCol *= fogCol.a * _GlobalDensity;
		float4 results = lerp(MT_Color,fogCol,0.2 * pixDistance * alpha);
		results += (float4(1,1,1,1) - results) * fogCol * alpha;
		//results = lerp(results,results*fogCol,0.0);
	
		return float4(results.rgb, 0.0/*MT_Color.a*/);
	}

	half4 fragDistance (v2f i) : SV_Target
	{
		float dpth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,i.uv_depth));		
		float4 camDir = ( /*_CameraWS  + */ dpth * i.interpolatedRay);
		float fogInt = saturate(length( camDir ) * _StartDistance.x * 0.1 - 1.0) * _StartDistance.y;	
//		return exp(-_GlobalDensity*fogInt);

		
		float pixDistance = saturate(pow(length(camDir) * _FogMaxDistance,0.7));
		float4 fogColor = DistanceToColor(pixDistance);
		fogColor = lerp(fogColor,_SkyColor,pixDistance * dot(float4(0,1,0,0),camDir * _FogMaxDistance));
		
		//return UNITY_SAMPLE_SHADOW(_ShadowMapTexture, float3(1,2,3));
		//return float4(unitySampleShadow(float4(i.uv_depth.x,i.uv_depth.y,0.5,0.5)));
		
		//camDir
		
		
		//float4 results = ApplyFogColor(tex2D(_MainTex, i.uv),fogColor,pixDistance);
		//results += (1-results) * abs(normalize(camDir)) * 0.5;
		//return results;
		
		//return ApplyFogColor(tex2D(_MainTex, i.uv),fogColor,pixDistance) - normalize(camDir) * 0.07;
		return ApplyFogColor(tex2D(_MainTex, i.uv),fogColor,pixDistance) - normalize(camDir) * 0.07;
		//ApplyFogColor(tex2D(_MainTex, i.uv),fogColor,pixDistance) - normalize(camDir) * 0.5;
		
		
		//return length(camDir)*0.01;
		//return lerp(_FogColor, tex2D(_MainTex, i.uv), exp(-_GlobalDensity*fogInt));				
	}
	

ENDCG

SubShader {
	Pass {
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }

		CGPROGRAM

		#pragma vertex vert
		#pragma fragment fragAbsoluteYAndDistance
		#pragma fragmentoption ARB_precision_hint_fastest 
		#pragma exclude_renderers flash
		
		ENDCG
	}

	Pass {
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }

		CGPROGRAM

		#pragma vertex vert
		#pragma fragment fragAbsoluteY
		#pragma fragmentoption ARB_precision_hint_fastest 
		#pragma exclude_renderers flash
		
		ENDCG
	}

	Pass {
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }

		CGPROGRAM

		#pragma vertex vert
		#pragma fragment fragDistance
		#pragma fragmentoption ARB_precision_hint_fastest 
		#pragma exclude_renderers flash
		
		ENDCG
	}

	Pass {
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }

		CGPROGRAM

		#pragma vertex vert
		#pragma fragment fragRelativeYAndDistance
		#pragma fragmentoption ARB_precision_hint_fastest 
		#pragma exclude_renderers flash
		
		ENDCG
	}
}

Fallback off

}