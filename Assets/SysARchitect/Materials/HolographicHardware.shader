Shader "SysARchitect/HolographicHardware"
{
    Properties
    {
        [MainColor] _BaseColor("Accent", Color) = (0, 0.85, 1, 1)
        _EmissionColor("Indicator emission", Color) = (0, 0.3, 0.4, 1)
        [HideInInspector] _Surface("Surface", Float) = 0
        [HideInInspector] _Blend("Blend", Float) = 0
        [HideInInspector] _SrcBlend("Source blend", Float) = 1
        [HideInInspector] _DstBlend("Destination blend", Float) = 0
        [HideInInspector] _ZWrite("Depth write", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; half3 normalWS : TEXCOORD0; half4 color : COLOR; };
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS); o.color = v.color; return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                // Flat face shading and unlit emissive strips, without lights/bloom/depth textures.
                half shade = .55h + .45h * saturate(dot(normalize(i.normalWS + half3(0, .001h, 0)), normalize(half3(.3h, .8h, .5h))));
                half light = step(.8h, i.color.r) * i.color.a;
                half3 tint = lerp(i.color.rgb, i.color.rgb * _BaseColor.rgb, i.color.a);
                return half4(tint * lerp(shade, 1.0h, light) + _EmissionColor.rgb * light * .25h, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
