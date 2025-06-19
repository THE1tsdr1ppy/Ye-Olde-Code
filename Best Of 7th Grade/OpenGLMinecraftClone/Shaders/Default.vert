#version 330 core
layout (location = 0) in vec3 aPosition; // vertex coordinates
layout (location = 1) in vec2 aTexCoord; // texture coordinates
layout (location = 2) in float aLightLevel; // per-vertex light level

out vec2 texCoord;
out float lightLevel;
out float fogFactor;

// uniform variables
uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

// Fog uniforms
uniform vec3 fogColor = vec3(0.5, 0.5, 0.5); // gray fog by default
uniform float fogDensity = 0.01;
uniform float fogStart = 50.0;
uniform float fogEnd = 100.0;

void main() 
{
    vec4 worldPosition = vec4(aPosition, 1.0) * model;
    vec4 viewPosition = worldPosition * view;
    gl_Position = viewPosition * projection;
    
    texCoord = aTexCoord;
    lightLevel = aLightLevel;
    
    // Calculate fog factor based on distance from camera
    float distance = length(viewPosition);
    fogFactor = clamp((fogEnd - distance) / (fogEnd - fogStart), 0.0, 1.0);
    fogFactor = exp(-fogDensity * distance); // exponential fog
}