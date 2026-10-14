#version 450

layout(location = 0) in vec3 inPosition; // @vertex Position required
layout(location = 3) in vec4 inUv0;      // @vertex TexCoord0 optional @default = vec4(0.0, 0.0, 0.0, 0.0)

layout(push_constant) uniform Push { mat4 modelMatrix; } push;

layout(location = 1) out vec2 vUv;

layout(set = 0, binding = 0) uniform VertexUniforms
{
    mat4 ViewProj;
} uVertex;

// FragmentUniforms is bound to the vertex stage too (see MeshBatchRenderer); we only need WorldToCamera here.
layout(set = 0, binding = 1) uniform FragmentUniforms
{
    vec4 SunDir;
    mat4 WorldToCamera;
    mat4 ViewProj;
} uFragment;

void main()
{
    mat3 viewRotation = mat3(uFragment.WorldToCamera);
    vec3 viewTranslation = uFragment.WorldToCamera[3].xyz;
    vec3 cameraWorldPos = -(viewTranslation * viewRotation);

    vec4 world = push.modelMatrix * vec4(inPosition, 1.0);
    world.xyz += cameraWorldPos;
    gl_Position = uVertex.ViewProj * world;
    vUv = inUv0.xy;
}
