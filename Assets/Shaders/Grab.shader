Shader "Custom/Grab"
{
	Properties
	{
		_MainTex ("Composit (RGB)", 2D) = "white" {}
	}
	
	CGINCLUDE
	#include "UnityCG.cginc"
	
	uniform sampler2D _TemporaryRT;
	uniform sampler2D _MainTex;
	
	float4 _MainTex_ST;
	
	float4 fragGrab(v2f_img i) : COLOR
	{
		return tex2D(_TemporaryRT, i.uv);
	}	
	
	float4 fragComposit(v2f_img i) : COLOR
	{
		return tex2D(_MainTex, i.uv);
	}	
	ENDCG	
	
	SubShader
	{
		// 0 Grab
		Pass
		{
			Name "Grab"
		
			ZTest Always Cull Off ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
				
			CGPROGRAM
			#pragma target 3.0
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert_img
			#pragma fragment fragGrab
			ENDCG			
		}
		
		// 1 Composit
		Pass
		{
			Name "Composit"
		
			ZTest Always Cull Off ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
				
			CGPROGRAM
			#pragma target 3.0
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert_img
			#pragma fragment fragComposit
			ENDCG			
		}		
	} 
	FallBack Off
}