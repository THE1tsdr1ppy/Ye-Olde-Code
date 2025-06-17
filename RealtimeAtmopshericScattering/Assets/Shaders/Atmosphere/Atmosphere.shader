
Shader "Custom/PlanetAtmosphere"
{
    Properties
    {
        _PlanetCenter("Planet Center", Vector) = (0, 0, 0, 0)
        _PlanetRadius("Planet Radius", Float) = 6371000
        _AtmosphereHeight("Atmosphere Height", Float) = 100000
        _AtmosphereDensity("Atmosphere Density", Range(0, 10)) = 2.0
        _DensityFalloff("Density Falloff", Range(0.1, 10.0)) = 1.0
        
        _RayleighScatteringCoeff("Rayleigh Scattering Coefficient", Vector) = (5.8, 13.5, 33.1, 0)
        _MieScatteringCoeff("Mie Scattering Coefficient", Float) = 0.1
        _MieScatteringFromSpace("Mie Scattering From Space", Range(0, 10)) = 2.0
        _MieAnisotropy("Mie Anisotropy", Range(0, 0.999)) = 0.76
        _ScatteringScale("Scattering Scale", Range(0, 100)) = 20.0
        _ScatteringPower("Scattering Power", Range(0, 10)) = 3.0
        
        _SunIntensity("Sun Intensity", Range(0, 100)) = 50.0
        _SunFalloff("Sun Falloff", Range(1, 50)) = 10.0
        _SunPosition("Sun Position", Vector) = (0, 0, 0, 0)
        
        _SunsetStrength("Sunset Strength", Range(0, 10)) = 1.5
        _HorizonFalloff("Horizon Falloff", Range(1, 20)) = 8.0
        _LimbDarkeningPower("Limb Darkening Power", Range(0, 10)) = 2.0
        _TimeOfDay("Time of Day (0-24)", Range(0, 24)) = 12.0
        _SunriseTime("Sunrise Time", Range(0, 24)) = 6.0
        _SunsetTime("Sunset Time", Range(0, 24)) = 18.0
        
        _SunDaytimeColor("Sun Daytime Color", Color) = (1.0, 1.0, 0.9, 1.0)
        _SunSunriseColor("Sun Sunrise Color", Color) = (1.0, 0.5, 0.2, 1.0)
        _SunSunsetColor("Sun Sunset Color", Color) = (1.0, 0.4, 0.1, 1.0)
        _SunNightColor("Sun Night Color", Color) = (0.2, 0.2, 0.5, 1.0)
        
        _DaytimeSkyIntensity("Daytime Sky Intensity", Range(1, 10)) = 5.0
        _NighttimeSkyIntensity("Nighttime Sky Intensity", Range(0.1, 1)) = 0.3
        _DaytimeScatteringMultiplier("Daytime Scattering Multiplier", Range(1, 10)) = 3.0
        _SpaceVisibilityDaytime("Space Visibility Daytime", Range(0, 1)) = 0.05
        _SpaceVisibilityNight("Space Visibility Night", Range(0, 1)) = 0.9
        _SpaceVisibilityTransitionSharpness("Space Visibility Transition Sharpness", Range(1, 10)) = 5.0
        _DepthThreshold("Depth Threshold", Range(0.0001, 0.1)) = 0.001
        
        // New optimization parameters
        _MaxSteps("Max Steps", Range(32, 512)) = 256
        _MinSteps("Min Steps", Range(16, 128)) = 64
        _StepOptimizationDistance("Step Optimization Distance", Float) = 1000.0
        _QualityLevel("Quality Level", Range(0, 2)) = 1
        _MaxStepSize("Max Step Size Factor", Range(0.005, 0.05)) = 0.01
    }

    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureXR.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
    
    #define PI 3.14159265358979323846264338327  
    #define UNITY_REVERSED_Z 1 

    // Existing properties
    float4 _PlanetCenter;
    float _PlanetRadius;
    float _AtmosphereHeight;
    float _AtmosphereDensity;
    float _DensityFalloff;
    float3 _RayleighScatteringCoeff;
    float _MieScatteringCoeff;
    float _MieScatteringFromSpace;
    float _MieAnisotropy;
    float _ScatteringScale;
    float _ScatteringPower;
    float _SunIntensity;
    float _SunFalloff;
    float4 _SunPosition;
    float4 _PlanetCenterHigh;
    float4 _PlanetCenterLow;
    float _UseDoublePrecision;
    float _SunsetStrength;
    float _HorizonFalloff;
    float _LimbDarkeningPower;
    float _TimeOfDay;
    float _SunriseTime;
    float _SunsetTime;
    float _DepthThreshold;
    float4 _SunDaytimeColor;
    float4 _SunSunriseColor;
    float4 _SunSunsetColor;
    float4 _SunNightColor;
    float _DaytimeSkyIntensity;
    float _NighttimeSkyIntensity;
    float _DaytimeScatteringMultiplier;
    float _SpaceVisibilityDaytime;
    float _SpaceVisibilityNight;
    float _SpaceVisibilityTransitionSharpness;
    float _CameraDistance;
    float4 _CameraPosHigh;
    float4 _CameraPosLow;
    
    // New optimization properties
    float _MaxSteps;
    float _MinSteps;
    float _StepOptimizationDistance;
    int _QualityLevel;
    float _MaxStepSize;

    struct AtmosphereAttributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct AtmosphereVaryings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord : TEXCOORD0;
        float3 viewVector : TEXCOORD1;
        float3 worldPos : TEXCOORD2;
        float viewZ : TEXCOORD3;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    struct FragmentOutput
    {
        float4 color : SV_Target;
        float depth : SV_Depth;
    };
    
    AtmosphereVaryings Vert(AtmosphereAttributes input)
    {
        AtmosphereVaryings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        
        float4 worldPos = mul(UNITY_MATRIX_I_VP, float4(output.positionCS.xy, 0.0, 1.0));
        worldPos.xyz /= worldPos.w;
        output.viewVector = worldPos.xyz - _WorldSpaceCameraPos;
        output.worldPos = worldPos.xyz;
        
        float4 viewPos = mul(UNITY_MATRIX_V, float4(worldPos.xyz, 1.0));
        output.viewZ = -viewPos.z;
        
        return output;
    }

    float3 GetPlanetCenter()
    {
        if (_UseDoublePrecision > 0.5)
        {
            float3 centerHigh = _PlanetCenterHigh.xyz;
            float3 centerLow = _PlanetCenterLow.xyz;
            return centerHigh + centerLow;
        }
        else
        {
            return _PlanetCenter.xyz;
        }
    }

    bool RaySphereIntersect(float3 rayOrigin, float3 rayDir, float3 sphereCenter, float sphereRadius, out float t0, out float t1)
    {
        float3 L = sphereCenter - rayOrigin;
        float tca = dot(L, rayDir);
        float d2 = dot(L, L) - tca * tca;
        float radiusSquared = sphereRadius * sphereRadius;
        
        if (d2 > radiusSquared)
            return false;
            
        float thc = sqrt(radiusSquared - d2);
        t0 = tca - thc;
        t1 = tca + thc;
        
        return true;
    }

    float AtmosphereDensity(float3 pos)
    {
        float h = max(0.0, length(pos - GetPlanetCenter()) - _PlanetRadius);
        return exp(-h / (_AtmosphereHeight * _AtmosphereDensity) * _DensityFalloff);
    }

    float RayleighPhase(float cosTheta)
    {
        return (3.0 / (16.0 * PI)) * (1.0 + cosTheta * cosTheta);
    }

    float MiePhase(float cosTheta, float g)
    {
        float g2 = g * g;
        return (1.0 / (4.0 * PI)) * ((1.0 - g2) / pow(1.0 + g2 - 2.0 * g * cosTheta, 1.5));
    }

    float CalculateOpticalDepth(float3 startPos, float3 endPos, int steps)
    {
        float3 rayDir = normalize(endPos - startPos);
        float rayLength = length(endPos - startPos);
        float stepSize = rayLength / float(steps);
        
        float opticalDepth = 0;
        
        for (int i = 0; i < steps; i++)
        {
            float3 pos = startPos + rayDir * stepSize * (i + 0.5);
            float density = AtmosphereDensity(pos);
            opticalDepth += density * stepSize;
        }
        
        return opticalDepth;
    }

    float3 CalculateSunPathTransmittance(float3 pos, float3 sunDir, int steps)
    {
        float3 sunRayOrigin = pos;
        float3 sunRayDir = sunDir;
        
        float tSunAtmosphereNear, tSunAtmosphereFar;
        float atmosphereOuterRadius = _PlanetRadius + _AtmosphereHeight;
        bool sunRayIntersectsAtmosphere = RaySphereIntersect(sunRayOrigin, sunRayDir, GetPlanetCenter(), atmosphereOuterRadius, tSunAtmosphereNear, tSunAtmosphereFar);
        
        float tSunPlanetNear, tSunPlanetFar;
        bool sunRayHitsPlanet = RaySphereIntersect(sunRayOrigin, sunRayDir, GetPlanetCenter(), _PlanetRadius, tSunPlanetNear, tSunPlanetFar);
        
        if (sunRayHitsPlanet && tSunPlanetNear > 0 && tSunPlanetNear < tSunAtmosphereFar)
        {
            return float3(0, 0, 0);
        }
        
        float sunRayLength = tSunAtmosphereFar - max(0, tSunAtmosphereNear);
        float stepSize = sunRayLength / float(steps);
        
        float opticalDepth = 0;
        for (int i = 0; i < steps; i++)
        {
            float3 samplePos = sunRayOrigin + sunRayDir * (max(0, tSunAtmosphereNear) + stepSize * (i + 0.5));
            float density = AtmosphereDensity(samplePos);
            opticalDepth += density * stepSize;
        }
        
        float3 transmittance = exp(-opticalDepth * _RayleighScatteringCoeff);
        return transmittance;
    }

    bool IsCameraOutsideAtmosphere()
    {
        float camDistanceFromCenter = length(_WorldSpaceCameraPos - GetPlanetCenter());
        return camDistanceFromCenter > (_PlanetRadius + _AtmosphereHeight);
    }

    // Improved step calculation with zoom-in handling
    int CalculateOptimalSteps(float3 rayOrigin, float atmosphereOuterRadius, float rayLength)
{
    float camDistanceFromCenter = length(rayOrigin - GetPlanetCenter());
    float heightRatio = (camDistanceFromCenter - _PlanetRadius) / _AtmosphereHeight;
    
    // Base on camera distance from surface
    float distanceFromSurface = camDistanceFromCenter - _PlanetRadius;
    float surfaceProximity = saturate(1.0 - distanceFromSurface / (_AtmosphereHeight * 0.5));
    
    // Calculate adaptive steps based on multiple factors
    int baseSteps;
    
    if (IsCameraOutsideAtmosphere())
    {
        // When outside, scale steps with distance but keep minimum near atmosphere
        float distScale = saturate(distanceFromSurface / (_AtmosphereHeight * 5.0));
        baseSteps = (int)lerp(_MaxSteps, _MinSteps, distScale);
    }
    else
    {
        // Inside atmosphere - prioritize quality when near surface
        baseSteps = (int)lerp(_MaxSteps * 1.5, _MaxSteps, saturate(heightRatio));
    }
    
    // Apply strong boost when very close to surface
    baseSteps = (int)lerp(baseSteps, _MaxSteps * 2.0, pow(surfaceProximity, 3.0));
    
    // Apply quality level scaling
    float qualityMultiplier = 1.0;
    switch (_QualityLevel)
    {
        case 0: qualityMultiplier = 0.5; break;
        case 1: qualityMultiplier = 0.75; break;
        case 2: qualityMultiplier = 1.0; break;
    }
    
    // Ensure we stay within reasonable bounds
    return clamp((int)(baseSteps * qualityMultiplier), (int)_MinSteps, (int)_MaxSteps * 2);
}

    float3 CalculateScattering(float3 startPos, float3 endPos, float3 sunDir, int steps)
    {
        float3 rayDir = normalize(endPos - startPos);
        float rayLength = length(endPos - startPos);
        float stepSize = rayLength / float(steps);
        
        float3 totalRayleigh = 0;
        float3 totalMie = 0;
        
        bool viewingFromSpace = IsCameraOutsideAtmosphere();
        float mieSpaceMultiplier = viewingFromSpace ? _MieScatteringFromSpace : 1.0;
        
        // Adaptive step optimization for sun ray marching
        int sunSteps = max(4, steps / 8);
        
        for (int i = 0; i < steps; i++)
        {
            float3 pos = startPos + rayDir * stepSize * (i + 0.5);
            
            float height = length(pos - GetPlanetCenter()) - _PlanetRadius;
            if (height < 0 || height > _AtmosphereHeight)
                continue;
            
            float density = AtmosphereDensity(pos);
            
            if (density > 0.0001)
            {
                float cosTheta = dot(rayDir, sunDir);
                float rayleighPhase = RayleighPhase(cosTheta);
                float miePhase = MiePhase(cosTheta, _MieAnisotropy);
                
                float3 sunTransmittance = CalculateSunPathTransmittance(pos, sunDir, sunSteps);
                float viewOpticalDepth = CalculateOpticalDepth(startPos, pos, max(4, steps/8));
                float3 viewTransmittance = exp(-viewOpticalDepth * _RayleighScatteringCoeff);
                float3 totalTransmittance = sunTransmittance * viewTransmittance;
                
                float3 rayleighScattering = _RayleighScatteringCoeff * density * rayleighPhase * totalTransmittance;
                float mieScattering = _MieScatteringCoeff * density * miePhase * totalTransmittance * mieSpaceMultiplier;
                
                float scatterAmount = stepSize * _ScatteringScale;
                totalRayleigh += rayleighScattering * scatterAmount;
                totalMie += mieScattering * scatterAmount;
            }
        }
        
        float sunDot = dot(rayDir, sunDir);
        float sunContribution = pow(max(0, sunDot), _SunFalloff) * _SunIntensity;
        
        return (totalRayleigh + totalMie) * _ScatteringPower * (1.0 + sunContribution);
    }
    // Additional helper function for better geometry detection
bool IsPixelOccludedByGeometry(float2 screenPos, float atmosphereDistance)
{
    float rawDepth = LoadCameraDepth(screenPos);
    float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
    
    // More sophisticated occlusion test
    return (rawDepth < 0.99f) && (linearDepth < atmosphereDistance - 0.00001f);
}

// Improved ray-sphere intersection with better numerical stability
bool RaySphereIntersectImproved(float3 rayOrigin, float3 rayDir, float3 sphereCenter, float sphereRadius, out float t0, out float t1)
{
    float3 L = sphereCenter - rayOrigin;
    float tca = dot(L, rayDir);
    float d2 = dot(L, L) - tca * tca;
    float radiusSquared = sphereRadius * sphereRadius;
    
    if (d2 > radiusSquared)
    {
        t0 = t1 = -1.0f;
        return false;
    }
    
    float thc = sqrt(radiusSquared - d2);
    t0 = tca - thc;
    t1 = tca + thc;
    
    // Handle case where ray starts inside sphere
    if (t0 < 0.0f && t1 > 0.0f)
    {
        t0 = 0.0f;
    }
    
    return t1 > 0.0f;
}

    FragmentOutput Frag(AtmosphereVaryings input)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    FragmentOutput output;
    
    // -- Step 1: Get accurate distance to scene geometry --
    float rawDepth = LoadCameraDepth(input.positionCS.xy);
    float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
    
    // Correct linear depth (distance from camera plane) to true distance along the view ray
    float rayDistToLinearDepthRatio = length(input.viewVector) / input.viewZ;
    float sceneDistance = linearDepth * rayDistToLinearDepthRatio;
    
    // Check if the pixel has any scene geometry rendered to it (skybox depth is 1.0)
    bool hasSceneGeometry = rawDepth < 1.0f;
    if (!hasSceneGeometry)
    {
        sceneDistance = 1e10; // Use a large number for 'infinity'
    }
    
    // -- Step 2: Calculate ray marching bounds for the atmosphere --
    float3 rayOrigin = _WorldSpaceCameraPos;
    float3 rayDir = normalize(input.viewVector);
    float3 sunDir = normalize(_SunPosition.xyz - GetPlanetCenter());
    
    // Calculate atmosphere intersections using the more robust function
    float tAtmosphereNear, tAtmosphereFar;
    float atmosphereOuterRadius = _PlanetRadius + _AtmosphereHeight;
    bool intersectsAtmosphere = RaySphereIntersectImproved(rayOrigin, rayDir, GetPlanetCenter(), 
                                                           atmosphereOuterRadius, tAtmosphereNear, tAtmosphereFar);
    
    if (!intersectsAtmosphere)
    {
        discard;
    }
    
    // Calculate planet intersections
    float tPlanetNear, tPlanetFar;
    bool intersectsPlanet = RaySphereIntersectImproved(rayOrigin, rayDir, GetPlanetCenter(), 
                                                       _PlanetRadius, tPlanetNear, tPlanetFar);
    
    // Clip atmosphere ray against the planet's surface
    if (intersectsPlanet)
    {
        tAtmosphereFar = min(tAtmosphereFar, tPlanetNear);
    }
    
    // -- Step 3: Clip atmosphere ray against scene geometry --
    float atmosphereStart = tAtmosphereNear;
    float atmosphereEnd = tAtmosphereFar;
    
    // Case 1: Scene geometry is entirely in front of the atmosphere
    if (hasSceneGeometry && sceneDistance < atmosphereStart - 0.001f)
    {
        discard;
    }
    
    // Case 2: Scene geometry intersects the atmosphere volume
    if (hasSceneGeometry && sceneDistance < atmosphereEnd)
    {
        // Shorten the ray to stop at the geometry
        atmosphereEnd = sceneDistance;
    }

    // Ensure we still have a valid segment to march
    if (atmosphereStart >= atmosphereEnd)
    {
        discard;
    }

    // -- Step 4: Perform ray marching --
    float3 startPos = rayOrigin + rayDir * atmosphereStart;
    float3 endPos = rayOrigin + rayDir * atmosphereEnd;
    float rayLength = atmosphereEnd - atmosphereStart;
    
    if (rayLength < 0.01f)
    {
        discard;
    }
    
    int steps = CalculateOptimalSteps(rayOrigin, atmosphereOuterRadius, rayLength);
    float3 atmosphereColor = CalculateScattering(startPos, endPos, sunDir, steps);
    
    // -- Step 5: Correctly calculate depth and alpha --
    
    // Determine the final depth value for the Z-buffer
    float finalDepthValue;
    bool clippedByGeometry = hasSceneGeometry && (sceneDistance <= atmosphereEnd + 0.001f);
    
    if (clippedByGeometry)
    {
        // If clipped by geometry, use its depth to prevent Z-fighting.
        finalDepthValue = rawDepth;
    }
    else
    {
        // Otherwise, calculate depth from the ray's end point.
        float3 depthWorldPos = rayOrigin + rayDir * atmosphereEnd;
        float4 clipPos = mul(UNITY_MATRIX_VP, float4(depthWorldPos, 1.0));
        finalDepthValue = clipPos.z / clipPos.w; // Correct perspective depth [0,1] for reversed-Z
    }

    // Calculate a physically-based alpha from optical depth
    float opticalDepth = CalculateOpticalDepth(startPos, endPos, 16); // Low sample count for alpha
    float3 transmittance = exp(-opticalDepth * _RayleighScatteringCoeff * 0.1f); // Use a fraction of scattering for alpha
    float avgTransmittance = (transmittance.r + transmittance.g + transmittance.b) / 3.0;
    float finalAlpha = 1.0 - avgTransmittance;

    // Fade out alpha for very thin atmosphere slices to reduce limb aliasing
    finalAlpha *= saturate(rayLength / (_AtmosphereHeight * 0.05f));
    
    // Smoothly fade out when clipped by geometry
    if (clippedByGeometry)
    {
        float totalPossibleLength = tAtmosphereFar - tAtmosphereNear;
        if (totalPossibleLength > 0.01f) {
            float clipFactor = saturate((sceneDistance - tAtmosphereNear) / totalPossibleLength);
            finalAlpha *= clipFactor * clipFactor; // quadratic falloff for smoothness
        } else {
            finalAlpha = 0;
        }
    }
    
    if (finalAlpha < 0.001)
    {
        discard;
    }
    
    output.color = float4(atmosphereColor, finalAlpha);
    output.depth = saturate(finalDepthValue);
    
    return output;
}
    ENDHLSL

    SubShader
    {
        Tags 
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "HDRenderPipeline"
            "Queue" = "Transparent-50"
        }
        
        Pass
        {
            Name "PlanetAtmosphere"
            
            Tags
            {
                "LightMode" = "CustomPass"
            }
            
            ZWrite On
            ZTest LEqual
            Blend One OneMinusSrcAlpha
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            ENDHLSL
        }
    }
    Fallback Off
}
