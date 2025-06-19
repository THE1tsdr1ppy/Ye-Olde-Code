#version 330 core

in vec2 texCoord;
in float lightLevel;
in float fogFactor;

out vec4 FragColor;

uniform sampler2D texture0;
uniform vec3 fogColor = vec3(0.5, 0.5, 0.5); // should match vertex shader

void main() 
{
    vec4 textureColor = texture(texture0, texCoord);
    
    // Apply lighting by multiplying the texture color with the light level
    vec4 litColor = vec4(textureColor.rgb * lightLevel, textureColor.a);
    
    // Mix with fog color based on fog factor
    FragColor = mix(vec4(fogColor * lightLevel, litColor.a), litColor, fogFactor);
}