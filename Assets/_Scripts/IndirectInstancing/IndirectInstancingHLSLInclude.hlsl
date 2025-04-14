// InstancedAnimatedHLSLInclude.hlsl
#ifndef INDIRECT_INSTANCED_HLSL_INCLUDED
#define INDIRECT_INSTANCED_HLSL_INCLUDED

// --- Includes ---
// Include URP Core library - provides necessary functions/macros for URP (like TransformWorldToHClip)
// It often includes Common.hlsl and others transitively.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
// Include instancing helpers (provides UNITY_INSTANCING_BUFFER_START etc.)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"


// --- Structs ---
// Define the per-instance data structure.
struct InstanceData
{
    float4x4 data_matrix; // Object-to-World matrix
    float animationFrame; // Current animation frame (passed as float)
    // Ensure padding matches C# if you added it there for stride alignment
    float padding1;
    float padding2;
    float padding3;
};

// Input to the vertex shader
struct Attributes
{
    float4 positionOS : POSITION; // Object space vertex position
    float2 uv : TEXCOORD0; // Texture Coordinates
    uint vertexID : SV_VertexID; // <<< System Value: Vertex Index (0, 1, 2...)
    uint instanceID : SV_InstanceID; // System Value: Instance Index
};

// Output from vertex / Input to fragment
struct Varyings
{
    float4 positionCS : SV_POSITION; // Clip space vertex position (mandatory)
    float2 uv : TEXCOORD0; // Texture Coordinates passed to fragment shader
};


// --- Buffer Declaration ---
// Declare the buffer holding per-instance data using Unity macros.
UNITY_INSTANCING_BUFFER_START(PerInstance)
    UNITY_DEFINE_INSTANCED_PROP(StructuredBuffer<InstanceData>, _MatricesBuffer)
UNITY_INSTANCING_BUFFER_END(PerInstance)

// Animation Vertex Data Buffer
StructuredBuffer<float3> _AnimationVertexBuffer; // Holds float3 vertex positions: [f0v0, f0v1..., f1v0, f1v1...]

// Uniforms passed from C#
int _NumVerticesPerFrame; // Number of vertices in the base mesh/each frame
int _NumAnimFrames; // Total number of animation frames in the buffer


// --- Global Properties ---
// Declared within a Constant Buffer for material properties
CBUFFER_START(UnityPerMaterial)
    float4 _Color; // Base color tint
    float4 _MainTex_ST; // Tiling and Offset for _MainTex (scale.xy, translation.xy)
CBUFFER_END

// Declare Texture and Sampler using SRP macros
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);


// --- Vertex Shader ---
Varyings vert(Attributes input)
{
    Varyings output = (Varyings) 0; // Initialize

    // Access per-instance data
    StructuredBuffer<InstanceData> instanceBuffer = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _MatricesBuffer);
    InstanceData instance = instanceBuffer[input.instanceID];

    // --- Vertex Animation Logic with Interpolation ---
    float3 animatedPositionOS = input.positionOS.xyz; // Default to base mesh pose

    if (_NumAnimFrames > 1 && _NumVerticesPerFrame > 0) // Need at least 2 frames to interpolate
    {
        // Get the raw frame value (could be 5.7, for example)
        float rawFrame = instance.animationFrame;

        // Ensure frame time loops correctly within the valid range [0, _NumAnimFrames)
        // Using fmod is crucial here for smooth looping with float values.
        rawFrame = fmod(rawFrame, (float) _NumAnimFrames);
        if (rawFrame < 0)
            rawFrame += _NumAnimFrames; // Ensure positive value after fmod

        // Calculate the two integer frame indices to blend between
        int frame0_idx = (int) floor(rawFrame);
        int frame1_idx = frame0_idx + 1;

        // Handle looping for the second frame index (wrap back to frame 0)
        if (frame1_idx >= _NumAnimFrames)
        {
            frame1_idx = 0;
        }
        // Ensure frame0 is also clamped/wrapped (fmod should handle positive cases, clamp for safety)
        frame0_idx = clamp(frame0_idx, 0, _NumAnimFrames - 1);


        // Calculate the interpolation factor (the fractional part of rawFrame)
        // frac(5.7) = 0.7
        float interpolationFactor = frac(rawFrame);

        // Calculate indices into the flattened animation buffer for both frames
        int baseIndex0 = frame0_idx * _NumVerticesPerFrame;
        int baseIndex1 = frame1_idx * _NumVerticesPerFrame;
        int animVertIndex0 = baseIndex0 + input.vertexID;
        int animVertIndex1 = baseIndex1 + input.vertexID;

        // Sample vertex positions for both frames (add bounds checks if really needed)
        float3 positionOS_Frame0 = _AnimationVertexBuffer[animVertIndex0];
        float3 positionOS_Frame1 = _AnimationVertexBuffer[animVertIndex1];

        // Interpolate between the two positions using the factor
        animatedPositionOS = lerp(positionOS_Frame0, positionOS_Frame1, interpolationFactor);
    }
    else if (_NumAnimFrames == 1 && _NumVerticesPerFrame > 0)
    {
        // If only one frame, just sample that frame directly
        int animVertIndex = input.vertexID; // Index for frame 0
        animatedPositionOS = _AnimationVertexBuffer[animVertIndex];
    }
    // --- End Vertex Animation Logic ---


    // Transform position: Use the ANIMATED Object Space position -> World Space
    float3 positionWS = mul(instance.data_matrix, float4(animatedPositionOS, 1.0)).xyz;

    // Transform position: World Space -> Clip Space using URP helper function
    output.positionCS = TransformWorldToHClip(positionWS);

    // Calculate and pass UV coordinates, applying tiling and offset
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);

    return output;
}

// --- Fragment Shader ---
half4 frag(Varyings input) : SV_Target
{
    // Sample the texture
    half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
    // Combine texture color with the base color tint
    half4 finalColor = texColor * half4(_Color.rgb, 1.0);
    finalColor.a = texColor.a;

    return finalColor;
}

#endif // INDIRECT_INSTANCED_HLSL_INCLUDED