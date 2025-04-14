// InstancedAnimatedHLSL.shader
Shader "Custom/InstancedAnimatedHLSL"
{
    Properties
    {
        [Header(Surface Options)]
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass // ForwardLit Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #pragma vertex InstancedAnimVertex
            #pragma fragment InstancedAnimFragment

            #pragma multi_compile_instancing

            // Includes for ForwardLit pass
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Includes for my code
            #include "InstancedAnimatedHLSLInclude.hlsl" // Include shared code

            ENDHLSL
        } // End ForwardLit Pass


        // ***** ENTIRE ShadowCaster Pass Block REMOVED *****


    } // End SubShader
    Fallback "Hidden/InternalErrorShader"
} // End Shader