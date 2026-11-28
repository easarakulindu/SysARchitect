Shader "SysARchitect/HolographicHardware"
{
    Properties
    {
        [MainColor] _BaseColor("Accent", Color) = (0, 0.85, 1, 1)
        _EmissionColor("Indicator emission", Color) = (0, 0.3, 0.4, 1)
        _Opacity("Glass opacity", Range(0, 1)) = .3
        _HardwareDetails("Opaque hardware strips", Range(0, 1)) = 1
        [HideInInspector] _Surface("Surface", Float) = 0
        [HideInInspector] _Blend("Blend", Float) = 0
        [HideInInspector] _SrcBlend("Source blend", Float) = 5
        [HideInInspector] _DstBlend("Destination blend", Float) = 10
        [HideInInspector] _ZWrite("Depth write", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                half _Opacity;
                half _HardwareDetails;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; half3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; half4 color : COLOR; };
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS); o.color = v.color; return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                // Polished glass with view-dependent rims and two broad studio highlights.
                // No refraction/depth copies: suitable for the existing mobile URP renderer.
                half3 normal = normalize(i.normalWS + half3(0, .001h, 0));
                half3 view = GetWorldSpaceNormalizeViewDir(i.positionWS);
                half rim = pow(1 - saturate(abs(dot(normal, view))), 3);
                half3 key = normalize(half3(.4h, .8h, -.5h));
                half3 fill = normalize(half3(-.7h, .3h, .5h));
                half specular = pow(saturate(dot(normal, normalize(key + view))), 72) * .85h
                    + pow(saturate(dot(normal, normalize(fill + view))), 32) * .3h;
                half light = step(.8h, i.color.r) * i.color.a * _HardwareDetails;
                half3 tint = lerp(_BaseColor.rgb * .65h + .15h, i.color.rgb * _BaseColor.rgb, light);
                half3 reflected = GlossyEnvironmentReflection(reflect(-view, normal), .08h, 1);
                half3 glass = tint + rim * lerp(_BaseColor.rgb, half3(1, 1, 1), .55h) * .65h
                    + reflected * (.2h + rim * .6h) + specular;
                half opacity = lerp(saturate(_Opacity + rim * .45h + specular * .25h), 1, light);
                return half4(glass + _EmissionColor.rgb * light * .35h, opacity * _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
