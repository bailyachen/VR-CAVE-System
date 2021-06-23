Shader "Project2/material"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTexLeft ("Left Texture", 2D) = "white" {}
        _MainTexRight ("Right Texture", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTexLeft;
        sampler2D _MainTexRight;

        struct Input
        {
            float2 uv_MainTexLeft;
            float2 uv_MainTexRight;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        uniform int _RenderingMode;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert(inout appdata_full v) {
          switch (_RenderingMode) {
          default:
          case 0: // Stereo
          case 1: // Mono
            break;
          case 2: // LeftOnly
            if (unity_StereoEyeIndex == 1) // Right
                v.vertex = float4(0, 0, 0, 0);
            break;
          case 3: // RightOnly
            if (unity_StereoEyeIndex == 0) // Left
                v.vertex = float4(0, 0, 0, 0);
            break;
          }
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c;
            if (unity_StereoEyeIndex == 0) { // Left
                c = tex2D( _MainTexLeft, IN.uv_MainTexLeft ) *_Color;
            }
            else { // Right
                c = tex2D( _MainTexRight, IN.uv_MainTexRight ) *_Color;
            }
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
