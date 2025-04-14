// InstancedAnimatedHLSLInclude.hlsl
#ifndef INSTANCED_ANIMATED_HLSL_INCLUDED
#define INSTANCED_ANIMATED_HLSL_INCLUDED

// Include core render pipeline libraries for URP
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

// Include URP Core library - provides many necessary functions/macros for URP
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// Includes for URP specific functions (lighting, etc.) - uncomment if needed
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


// --- Data Structures ---

// Input to Vertex Shader from Mesh + Instance Data
struct Attributes
{
    float3 normalOS     : NORMAL;
    float2 uv           : TEXCOORD0;
    uint vertexID       : SV_VertexID;      // Index into _AnimatedVertices
};

// Output from Vertex Shader / Input to Fragment Shader
struct Varyings
{
    float4 positionCS   : SV_POSITION;      // Clip Space position
    float2 uv           : TEXCOORD0;
    float3 normalWS     : NORMAL;           // World Space normal for lighting
    // float3 positionWS   : TEXCOORD1;     // Optional: Pass world position if needed
};

// --- Buffers (matched in C# script) ---
StructuredBuffer<float3> _AnimatedVertices;         // Compute shader output (animated vertex positions in Object Space)


// --- Material Properties (defined in ShaderLab Properties block) ---
// These are accessed via a Constant Buffer Unity generates named "UnityPerMaterial"
CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST; // For UV tiling/offset (_ST convention: Scale.xy, Translate.zw)
    float4 _Color;
CBUFFER_END

// Texture/Sampler (defined in ShaderLab Properties block, accessed globally)
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);



// --- Vertex Shader ---
Varyings InstancedAnimVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    // *** DEBUG: Output hardcoded clip space position ***
    // Ignore ALL inputs, just output something simple at the center-ish
    output.positionCS = float4(0, 0, 0.5, 1.0); // Center X/Y, halfway into depth buffer
    output.uv = float2(0,0); // Dummy UV
    output.normalWS = float3(0,0,-1); // Dummy normal

    return output;
}

// --- Fragment Shader ---
// Renamed to avoid conflict
float4 InstancedAnimFragment(Varyings input) : SV_Target
{
    // Sample the texture
    float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

    // Apply material color
    float4 finalColor = texColor * _Color;

    // Optional: Add basic URP lighting here if needed, requires more includes/setup
    // e.g., Light mainLight = GetMainLight(); finalColor.rgb *= mainLight.color * saturate(dot(input.normalWS, mainLight.direction));

    return finalColor;
}

#endif // INSTANCED_ANIMATED_HLSL_INCLUDED