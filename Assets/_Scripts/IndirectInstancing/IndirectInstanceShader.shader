// Shader using HLSLPROGRAM block - Adapted for URP Indirect Instancing

Shader "Custom/IndirectInstancedShaderURP" // Renamed for clarity
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {} // Example for texture
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit" // Example Pass Name (URP uses specific pass names like ForwardLit, DepthOnly etc.)
            Tags { "LightMode"="UniversalForward" } // Important: Tag for URP forward rendering pass

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 5.0 // <<< ADD THIS LINE

            // --- Includes ---
            // Include the URP Core library. This defines necessary functions and types.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Include instancing helpers (often needed alongside URP Core for custom instancing)
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            #include "IndirectInstancingHLSLInclude.hlsl"


            ENDHLSL // End HLSLPROGRAM block
        }
    }
    Fallback "Hidden/InternalErrorShader"
}