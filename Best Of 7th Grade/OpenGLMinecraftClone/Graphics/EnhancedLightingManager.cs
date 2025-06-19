using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace OpenGLMinecraftClone.World
{
    public static class EnhancedLightingConstants
    {
        // Base light levels for each face
        private static readonly Dictionary<Faces, float> BaseFaceLightLevels = new Dictionary<Faces, float>
        {
            { Faces.TOP, 1.0f },        // Brightest - receiving direct light from above
            { Faces.BOTTOM, 0.4f },     // Darkest - no direct light
            { Faces.FRONT, 0.8f },      // Medium-bright
            { Faces.BACK, 0.8f },       // Medium-bright  
            { Faces.LEFT, 0.6f },       // Medium
            { Faces.RIGHT, 0.6f }       // Medium
        };

        // Current ambient light multiplier (updated by skybox)
        private static float ambientLightLevel = 1.0f;
        private static Vector3 ambientLightColor = Vector3.One;
        private static Vector3 sunDirection = new Vector3(0, 1, 0);

        public static void UpdateLighting(float ambientLevel, Vector3 ambientColor, Vector3 sunDir)
        {
            ambientLightLevel = ambientLevel;
            ambientLightColor = ambientColor;
            sunDirection = sunDir.Normalized();
        }

        public static Dictionary<Faces, float> GetCurrentLightLevels()
        {
            var adjustedLevels = new Dictionary<Faces, float>();

            foreach (var face in BaseFaceLightLevels)
            {
                float baseLevel = face.Value;

                // Apply ambient lighting
                float finalLevel = baseLevel * ambientLightLevel;

                // Add directional lighting bonus for faces facing the sun
                Vector3 faceNormal = GetFaceNormal(face.Key);
                float sunDot = Math.Max(0.0f, Vector3.Dot(faceNormal, sunDirection));
                float sunBonus = sunDot * 0.3f * ambientLightLevel;

                finalLevel = Math.Min(1.0f, finalLevel + sunBonus);
                finalLevel = Math.Max(0.1f, finalLevel); // Minimum visibility

                adjustedLevels[face.Key] = finalLevel;
            }

            return adjustedLevels;
        }

        private static Vector3 GetFaceNormal(Faces face)
        {
            return face switch
            {
                Faces.TOP => new Vector3(0, 1, 0),
                Faces.BOTTOM => new Vector3(0, -1, 0),
                Faces.FRONT => new Vector3(0, 0, 1),
                Faces.BACK => new Vector3(0, 0, -1),
                Faces.LEFT => new Vector3(-1, 0, 0),
                Faces.RIGHT => new Vector3(1, 0, 0),
                _ => Vector3.UnitY
            };
        }

        // Legacy property for backward compatibility
        public static Dictionary<Faces, float> CurrentLightLevels => GetCurrentLightLevels();

        public static Vector3 AmbientLightColor => ambientLightColor;
        public static float AmbientLightLevel => ambientLightLevel;
    }
}