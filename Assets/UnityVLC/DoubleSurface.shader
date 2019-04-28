Shader "Custom/DoubleSurface" {
	Properties{
		_Color("Main Color", Color) = (1,1,1,1)//Tint Color
		_MainTex("Base (RGB)", 2D) = "white" {} //背面纹理 
	}

		SubShader{
		Tags{ "RenderType" = "Opaque" }    //设置渲染类型 Opaque不透明
		LOD 100

		Pass{
		Cull Front       //关闭正面渲染
		Lighting Off
		SetTexture[_MainTex]{ combine texture }
		SetTexture[_MainTex]
	{
		ConstantColor[_Color]
		Combine Previous * Constant
	}
	}

		Pass
	{
		Cull Back      //关闭背面渲染
		Lighting Off
		SetTexture[_MainTex]{ combine texture }
		SetTexture[_MainTex]
	{
		ConstantColor[_Color]
		Combine Previous * Constant
	}
	}
	}
}