Shader "Frac/Frac Shader"
{
    SubShader
    {
        CGPROGRAM

        #pragma surface ConfigureSurface Standard fullforwardshadows addshadow
        #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
        #pragma editor_sync_compilation
        
        #pragma target 4.5

        #include "FracGPU.hlsl"

        struct Input {
            float3 worldPos;
        };

        float _Smothness;

        void ConfigureSurface (Input input, inout SurfaceOutputStandard surface)
        {
            surface.Albedo = GetFractalColor().rgb;
            surface.Smoothness = GetFractalColor().a;
        }
        ENDCG
    }

    Fallback "Diffuse"
}
