using System;
using System.Collections.Generic;

namespace OpenGLMinecraftClone.World
{
    public static class LightingConstants
    {
        // Light levels for each face (0.0 to 1.0)
        public static readonly Dictionary<Faces, float> FaceLightLevels = new Dictionary<Faces, float>
        {
            { Faces.TOP, 1.0f },        // Brightest - receiving direct light from above
            { Faces.BOTTOM, 0.4f },     // Darkest - no direct light
            { Faces.FRONT, 0.8f },      // Medium-bright
            { Faces.BACK, 0.8f },       // Medium-bright  
            { Faces.LEFT, 0.6f },       // Medium
            { Faces.RIGHT, 0.6f }       // Medium
        };

        // Alternative lighting scheme for more dramatic effect
        public static readonly Dictionary<Faces, float> DramaticFaceLightLevels = new Dictionary<Faces, float>
        {
            { Faces.TOP, 1.0f },        // Full brightness
            { Faces.BOTTOM, 0.3f },     // Very dark
            { Faces.FRONT, 0.85f },     // Bright
            { Faces.BACK, 0.65f },      // Medium
            { Faces.LEFT, 0.75f },      // Medium-bright
            { Faces.RIGHT, 0.55f }      // Medium-dark
        };

        // Get the current lighting scheme (you can switch between them)
        public static Dictionary<Faces, float> CurrentLightLevels => FaceLightLevels;
    }
}